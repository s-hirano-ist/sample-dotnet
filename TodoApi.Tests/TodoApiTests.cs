// using は、別の名前空間にある型を短い名前で使うための宣言です。
// 例: System.Net.HttpStatusCode を HttpStatusCode と書けるようになります。
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// namespace は、このファイル内のクラスが属する名前空間です。
// API本体の TodoApi と区別するため、テスト側は TodoApi.Tests にしています。
namespace TodoApi.Tests;

// WebApplicationFactory は、テスト用にASP.NET Coreアプリをメモリ上で起動します。
// 実際のポートは開かず、HttpClientからAPIを呼び出せます。
public class TodoApiTests
{
    [Fact]
    public async Task GetOpenApiDocument_ReturnsApiSpecification()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // MapOpenApiが追加した仕様書のURLへGETリクエストを送ります。
        var response = await client.GetAsync("/openapi/v1.json");

        // OpenAPI仕様書が正常に取得できることを確認します。
        response.EnsureSuccessStatusCode();

        // JSONをJsonObjectとして読み込み、仕様書の主要な項目を確認します。
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(document);
        // OpenAPIの3.1系であることを確認します。
        // パッチバージョンは使用するパッケージ更新で変わるため、3.1.で始まることだけを検証します。
        Assert.StartsWith("3.1.", document["openapi"]?.GetValue<string>());
        Assert.NotNull(document["paths"]?["/todos"]);
        Assert.Equal("List todos", document["paths"]?["/todos"]?["get"]?["summary"]?.GetValue<string>());
        Assert.Equal(
            "X-API-Key",
            document["components"]?["securitySchemes"]?["ApiKey"]?["name"]?.GetValue<string>()
        );
        Assert.NotNull(document["paths"]?["/todos"]?["post"]?["security"]);
        Assert.Null(document["paths"]?["/todos"]?["get"]?["security"]);
        Assert.NotNull(document["paths"]?["/todos"]?["post"]?["responses"]?["500"]);
    }

    [Fact]
    public async Task GetSwaggerUi_ReturnsHtmlPage()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // Swagger UIの画面は、開発環境で /swagger/index.html に公開されます。
        var response = await client.GetAsync("/swagger/index.html");

        response.EnsureSuccessStatusCode();

        // Swagger UIはブラウザで表示するHTMLを返します。
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", body);
        Assert.DoesNotContain("Content-Security-Policy", response.Headers.Select(header => header.Key));
    }

    [Fact]
    public async Task GetRoot_WithAllowedOrigin_ReturnsCorsHeader()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // ブラウザから送られるOriginヘッダーをテストで再現します。
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        // 許可したOriginだけがレスポンスヘッダーに返ることを確認します。
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single()
        );
    }

    [Fact]
    public async Task OptionsTodo_WithAllowedOrigin_ReturnsCorsPreflightHeaders()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // ブラウザがPOSTの前に送るプリフライトリクエストを再現します。
        using var request = new HttpRequestMessage(HttpMethod.Options, "/todos");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-api-key");

        var response = await client.SendAsync(request);

        // CORSミドルウェアが、許可されたプリフライトとして応答します。
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single()
        );
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains(
            "Content-Type",
            response.Headers.GetValues("Access-Control-Allow-Headers").Single()
        );
    }

    [Fact]
    public async Task GetRoot_WithDisallowedOrigin_DoesNotReturnCorsHeader()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // 設定にないOriginからのブラウザリクエストを再現します。
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "https://untrusted.example");

        var response = await client.SendAsync(request);

        // API自体は応答しても、ブラウザから本文を読めるCORSヘッダーは返しません。
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain("Access-Control-Allow-Origin", response.Headers.Select(header => header.Key));
    }

    [Fact]
    public async Task PostTodo_WithoutApiKey_ReturnsUnauthorized()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // テストファクトリは通常リクエストへAPIキーを付けます。
        // ここではヘッダーを削除して、未認証のリクエストを再現します。
        client.DefaultRequestHeaders.Remove("X-API-Key");

        var response = await client.PostAsJsonAsync("/todos", new { title = "No key" });

        // 認証情報がないため、HTTP 401 Unauthorizedを期待します。
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Authentication is required.", problemDetails["title"]?.GetValue<string>());
        Assert.Equal(401, problemDetails["status"]?.GetValue<int>());
    }

    [Fact]
    public async Task PostTodo_WithInvalidApiKey_ReturnsUnauthorized()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // 正しいキーではなく、別のキーを送る異常系を再現します。
        client.DefaultRequestHeaders.Remove("X-API-Key");
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong-api-key");

        var response = await client.PostAsJsonAsync("/todos", new { title = "Should fail" });

        // キーが存在していても、値が一致しなければ401になります。
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostTodo_WithReadOnlyPermission_ReturnsForbidden()
    {
        using var factory = new TodoApiReadOnlyTestFactory();
        using var client = factory.CreateClient();

        // APIキーは正しいものの、todo:write権限を持たないクライアントを再現します。
        var response = await client.PostAsJsonAsync("/todos", new { title = "Read only" });

        // 認証は成功しているため401ではなく、認可失敗の403になります。
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(problemDetails);
        Assert.Equal(403, problemDetails["status"]?.GetValue<int>());
    }

    [Fact]
    public async Task PostTodo_WithAdditionalApiKey_IsAcceptedDuringRotation()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Remove(ApiKeyAuthenticationDefaults.HeaderName);
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "rotating-api-key");

        var response = await client.PostAsJsonAsync("/todos", new { title = "Rotated key" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // [Fact] はxUnitの属性です。
    // このメソッドが「1つのテストケース」であることを表します。
    [Fact]
    // async Task は、非同期処理を含むテストメソッドを書くための戻り値です。
    // HTTPリクエストは待ち時間が発生する処理なので、awaitと組み合わせます。
    public async Task GetRoot_ReturnsRunningMessage()
    {
        // using var は、変数の使用が終わったら自動でDisposeする書き方です。
        // WebApplicationFactoryはテスト用APIを起動するため、テスト終了時に片付けます。
        using var factory = new TodoApiTestFactory();

        // CreateClient は、テスト用APIにリクエストを送るHttpClientを作ります。
        // 実際のlocalhostポートを使わず、メモリ上のAPIへアクセスします。
        using var client = factory.CreateClient();

        // await は、非同期処理が終わるまで待つためのキーワードです。
        // GetAsync("/") は GET / にHTTPリクエストを送ります。
        var response = await client.GetAsync("/");

        // EnsureSuccessStatusCode は、HTTPステータスが2xxでなければ例外にします。
        // このテストでは、GET / が成功することをまず確認しています。
        response.EnsureSuccessStatusCode();

        // レスポンス本文を文字列として読み取ります。
        var body = await response.Content.ReadAsStringAsync();

        // Assert.Equal は「期待値」と「実際の値」が同じか確認します。
        // 第1引数が期待値、第2引数が実際の値です。
        Assert.Equal("Todo API is running.", body);
    }

    [Fact]
    public async Task GetRoot_ReturnsSecurityHeaders()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        // ブラウザの解釈や埋め込みに関するセキュリティヘッダーを確認します。
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal(
            "camera=(), microphone=(), geolocation=()",
            response.Headers.GetValues("Permissions-Policy").Single()
        );
        Assert.Equal(
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'",
            response.Headers.GetValues("Content-Security-Policy").Single()
        );
    }

    [Fact]
    public async Task GetRoot_OverHttps_ReturnsHstsHeader()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // テスト用リクエストのスキームをHTTPSにして、HTTPS時の動作を確認します。
        var response = await client.GetAsync("https://localhost/");

        response.EnsureSuccessStatusCode();
        Assert.Equal(
            "max-age=31536000; includeSubDomains",
            response.Headers.GetValues("Strict-Transport-Security").Single()
        );
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseIsAvailable_ReturnsOk()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // /healthはアプリとテスト用SQLiteの接続状態を確認します。
        var response = await client.GetAsync("/health");

        // DBへ接続できているので、HTTP 200 OKを期待します。
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetLiveness_WhenApiProcessIsRunning_ReturnsOk()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // /liveはDBなどの外部依存先を確認せず、APIプロセスが動いているかだけを確認します。
        var response = await client.GetAsync("/live");

        // テスト用APIプロセスが動いているので、HTTP 200 OKを期待します。
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReadiness_WhenDatabaseIsAvailable_ReturnsOk()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // /readyは、リクエストを受け付けるために必要な依存サービスを確認します。
        var response = await client.GetAsync("/ready");

        // テスト用SQLiteへ接続できるため、HTTP 200 OKを期待します。
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRoot_WithRequestId_ReturnsSameRequestId()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Request-Id", "11111111-1111-1111-1111-111111111111");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(
            "11111111111111111111111111111111",
            response.Headers.GetValues("X-Request-Id").Single()
        );
    }

    [Fact]
    public async Task GetTodos_WhenRequestLimitIsExceeded_ReturnsTooManyRequests()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // 10回までは許可されるため、11回目のリクエストを確認します。
        for (var requestNumber = 1; requestNumber <= 10; requestNumber++)
        {
            var allowedResponse = await client.GetAsync("/todos");
            Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        }

        var rejectedResponse = await client.GetAsync("/todos");

        // 制限を超えた場合はHTTP 429 Too Many Requestsを返します。
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);

        // Retry-Afterは、再試行までの待ち時間を秒数で伝えます。
        Assert.Equal("10", rejectedResponse.Headers.RetryAfter?.ToString());
    }

    [Fact]
    public async Task GetTodos_WhenNoTodoExists_ReturnsEmptyArray()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // まだTodoを作成していない新しいAPIインスタンスなので、一覧は空のはずです。
        var response = await client.GetAsync("/todos");

        response.EnsureSuccessStatusCode();

        // ReadFromJsonAsync<JsonArray>() は、レスポンス本文のJSON配列を読み取ります。
        // /todos はページ情報を含むJSONオブジェクトを返します。
        var todos = await response.Content.ReadFromJsonAsync<JsonObject>();

        // Assert.NotNull は、値がnullではないことを確認します。
        Assert.NotNull(todos);

        Assert.NotNull(todos["items"]?.AsArray());
        Assert.Empty(todos["items"]!.AsArray());
        Assert.Equal(1, todos["page"]?.GetValue<int>());
        Assert.Equal(20, todos["pageSize"]?.GetValue<int>());
        Assert.Equal(0, todos["totalCount"]?.GetValue<int>());
        Assert.Equal(0, todos["totalPages"]?.GetValue<int>());
    }

    [Fact]
    public async Task GetTodos_WithPageSize_ReturnsRequestedPageAndTotalCount()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // ページングの確認用に3件作成します。
        for (var todoNumber = 1; todoNumber <= 3; todoNumber++)
        {
            var createResponse = await client.PostAsJsonAsync(
                "/todos",
                new { title = $"Todo {todoNumber}" }
            );
            createResponse.EnsureSuccessStatusCode();
        }

        var response = await client.GetAsync("/todos?page=2&pageSize=2");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(page);
        Assert.Equal(2, page["page"]?.GetValue<int>());
        Assert.Equal(2, page["pageSize"]?.GetValue<int>());
        Assert.Equal(3, page["totalCount"]?.GetValue<int>());
        Assert.Equal(2, page["totalPages"]?.GetValue<int>());
        Assert.Equal(1, page["items"]?.AsArray().Count);
        Assert.Equal("Todo 3", page["items"]?[0]?["title"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetTodos_WithInvalidPageSize_ReturnsBadRequest()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/todos?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(error);
        Assert.Equal("page_size_invalid", error["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetTodos_WithIsDoneFilter_ReturnsOnlyMatchingTodos()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var firstCreateResponse = await client.PostAsJsonAsync(
            "/todos",
            new { title = "Incomplete todo" }
        );
        firstCreateResponse.EnsureSuccessStatusCode();

        var secondCreateResponse = await client.PostAsJsonAsync(
            "/todos",
            new { title = "Completed todo" }
        );
        secondCreateResponse.EnsureSuccessStatusCode();

        // 2件目だけを完了状態へ変更します。
        var updateResponse = await client.PutAsJsonAsync(
            "/todos/2",
            new { isDone = true }
        );
        updateResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/todos?isDone=true");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(page);
        Assert.Equal(1, page["totalCount"]?.GetValue<int>());
        Assert.Equal(1, page["items"]?.AsArray().Count);
        Assert.Equal("Completed todo", page["items"]?[0]?["title"]?.GetValue<string>());
        Assert.True(page["items"]?[0]?["isDone"]?.GetValue<bool>());
    }

    [Fact]
    public async Task GetTodos_WithSearch_ReturnsTitlesContainingSearchTerm()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        foreach (var title in new[] { "Learn C#", "Learn ASP.NET Core", "Read a book" })
        {
            var createResponse = await client.PostAsJsonAsync("/todos", new { title });
            createResponse.EnsureSuccessStatusCode();
        }

        // search=learnに一致する2件だけが返ることを確認します。
        var response = await client.GetAsync("/todos?search=learn");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(page);
        Assert.Equal(2, page["totalCount"]?.GetValue<int>());
        var items = page["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
        var titles = items.Select(item => item?["title"]?.GetValue<string>()).ToArray();
        Assert.Contains("Learn C#", titles);
        Assert.Contains("Learn ASP.NET Core", titles);
    }

    [Fact]
    public async Task GetTodos_WithSortByTitleDescending_ReturnsDescendingTitles()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        foreach (var title in new[] { "Alpha", "Charlie", "Bravo" })
        {
            var createResponse = await client.PostAsJsonAsync("/todos", new { title });
            createResponse.EnsureSuccessStatusCode();
        }

        var response = await client.GetAsync("/todos?sortBy=title&sortOrder=desc");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(page);
        var items = page["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal("Charlie", items[0]?["title"]?.GetValue<string>());
        Assert.Equal("Bravo", items[1]?["title"]?.GetValue<string>());
        Assert.Equal("Alpha", items[2]?["title"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetTodos_WithUnsupportedSortBy_ReturnsBadRequest()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/todos?sortBy=unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(error);
        Assert.Equal("sort_by_invalid", error["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task PostTodos_WithValidTitle_CreatesTodo()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // new { ... } は匿名型です。
        // テスト用の小さなJSONリクエストを作るときによく使います。
        var request = new
        {
            title = "Learn automated testing"
        };

        // PostAsJsonAsync は、C#のオブジェクトをJSONに変換してPOSTします。
        // Content-Type: application/json も自動で設定されます。
        var response = await client.PostAsJsonAsync("/todos", request);

        // HttpStatusCode.Created はHTTP 201 Createdを表す列挙値です。
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Locationヘッダーには、作成されたリソースのURLが入ります。
        // ?. はnull条件演算子で、LocationがnullならOriginalStringを読まずにnullを返します。
        Assert.Equal("/todos/1", response.Headers.Location?.OriginalString);

        // レスポンス本文のJSONオブジェクトを読み取ります。
        // 今回はTodoItem型をテスト側に公開せず、JsonObjectとして確認します。
        var todo = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(todo);

        // todo["id"] はJSONオブジェクトのidプロパティを取り出します。
        // GetValue<int>() は、そのJSON値をintとして読み取ります。
        Assert.Equal(1, todo["id"]?.GetValue<int>());
        Assert.Equal("Learn automated testing", todo["title"]?.GetValue<string>());

        // Assert.False は、値がfalseであることを確認します。
        Assert.False(todo["isDone"]?.GetValue<bool>());
        Assert.NotNull(todo["createdAt"]);

        // 新規作成直後は未完了なので、completedAt は null のはずです。
        Assert.Null(todo["completedAt"]);
    }

    [Fact]
    public async Task PostTodos_WithBlankTitle_ReturnsBadRequest()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // 空白だけのタイトルは不正な入力として扱います。
        var request = new
        {
            title = "   "
        };

        var response = await client.PostAsJsonAsync("/todos", request);

        // 不正な入力なので、HTTP 400 Bad Requestを期待します。
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // エラーレスポンスもJSONオブジェクトとして返します。
        // codeはプログラム向け、messageは人間向けの説明です。
        var error = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(error);
        Assert.Equal("title_required", error["code"]?.GetValue<string>());
        Assert.Equal("Title is required.", error["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task PostTodos_WithTooLongTitle_ReturnsBadRequest()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // new string('a', 101) は、'a' を101文字並べた文字列を作ります。
        var request = new
        {
            title = new string('a', 101)
        };

        var response = await client.PostAsJsonAsync("/todos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(error);
        Assert.Equal("title_too_long", error["code"]?.GetValue<string>());
        Assert.Equal("Title must be 100 characters or fewer.", error["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetTodo_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // 999というIDのTodoは作っていないので、見つからないはずです。
        var response = await client.GetAsync("/todos/999");

        // 見つからないリソースにはHTTP 404 Not Foundを返します。
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTodo_WithValidRequest_UpdatesTodo()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var createRequest = new
        {
            title = "Write tests"
        };

        // 更新テストの準備として、まずPOSTでTodoを1件作ります。
        var createResponse = await client.PostAsJsonAsync("/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateRequest = new
        {
            title = "Write API tests",
            isDone = true
        };

        // PutAsJsonAsync は、C#のオブジェクトをJSONに変換してPUTします。
        var updateResponse = await client.PutAsJsonAsync("/todos/1", updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(updatedTodo);
        Assert.Equal(1, updatedTodo["id"]?.GetValue<int>());
        Assert.Equal("Write API tests", updatedTodo["title"]?.GetValue<string>());
        Assert.True(updatedTodo["isDone"]?.GetValue<bool>());
        Assert.NotNull(updatedTodo["completedAt"]);
    }

    [Fact]
    public async Task PutTodo_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var updateRequest = new
        {
            title = "This todo does not exist"
        };

        // 存在しないTodoを更新しようとすると、404になることを確認します。
        var response = await client.PutAsJsonAsync("/todos/999", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTodo_WithBlankTitle_ReturnsBadRequest()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var createRequest = new
        {
            title = "Write tests"
        };

        var createResponse = await client.PostAsJsonAsync("/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateRequest = new
        {
            title = "   "
        };

        var response = await client.PutAsJsonAsync("/todos/1", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(error);
        Assert.Equal("title_required", error["code"]?.GetValue<string>());
        Assert.Equal("Title is required.", error["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task DeleteTodo_WithExistingId_RemovesTodo()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var createRequest = new
        {
            title = "Delete this todo"
        };

        var createResponse = await client.PostAsJsonAsync("/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // DeleteAsync は DELETE /todos/1 にHTTPリクエストを送ります。
        var deleteResponse = await client.DeleteAsync("/todos/1");

        // 削除成功時は、本文なしのHTTP 204 No Contentを期待します。
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 削除後に同じIDを取得すると、もう存在しないので404になるはずです。
        var getResponse = await client.GetAsync("/todos/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/todos/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

// TodoApiTestFactoryは、テスト用のアプリ起動設定をまとめたクラスです。
// 本番用のSQLiteファイルではなく、テストごとにメモリ上のSQLiteを使います。
public class TodoApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // 既存の作成・更新・削除テストは、認証済みクライアントとして実行します。
        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.csの開発環境用設定をテストでも有効にします。
        builder.UseEnvironment("Development");

        // テストでは本物のUser Secretsや環境変数を使わず、テスト専用の設定を追加します。
        // Dictionaryは「設定キー」と「設定値」の組み合わせを表します。
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:ApiKey"] = "test-api-key",
                ["Authentication:AdditionalApiKeys:0"] = "rotating-api-key"
            });

            // 派生テストファクトリが、共通設定より後に設定を追加できるようにします。
            ConfigureAdditionalTestConfiguration(configuration);
        });

        builder.ConfigureServices(services =>
        {
            // Program.csで登録した本番用DbContext設定を探します。
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<TodoDbContext>)
            );

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // SQLiteのインメモリDBは、接続が開いている間だけデータが残ります。
            // そのため、SqliteConnectionをSingletonとして登録し、テスト中は開いたままにします。
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddSingleton(connection);

            services.AddDbContext<TodoDbContext>(options =>
            {
                options.UseSqlite(connection);
            });
        });
    }

    protected virtual void ConfigureAdditionalTestConfiguration(IConfigurationBuilder configuration)
    {
    }
}

// TodoApiReadOnlyTestFactoryは、認証は成功するが書き込み権限がない設定を作ります。
public class TodoApiReadOnlyTestFactory : TodoApiTestFactory
{
    protected override void ConfigureAdditionalTestConfiguration(IConfigurationBuilder configuration)
    {
        base.ConfigureAdditionalTestConfiguration(configuration);

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // 配列のPermissionsを読み取り権限だけで上書きします。
            ["Authentication:Permissions:0"] = "todo:read"
        });
    }
}
