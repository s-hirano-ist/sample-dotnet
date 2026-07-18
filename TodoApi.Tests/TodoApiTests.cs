// using は、別の名前空間にある型を短い名前で使うための宣言です。
// 例: System.Net.HttpStatusCode を HttpStatusCode と書けるようになります。
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// namespace は、このファイル内のクラスが属する名前空間です。
// API本体の TodoApi と区別するため、テスト側は TodoApi.Tests にしています。
namespace TodoApi.Tests;

// WebApplicationFactory は、テスト用にASP.NET Coreアプリをメモリ上で起動します。
// 実際のポートは開かず、HttpClientからAPIを呼び出せます。
public class TodoApiTests
{
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
    public async Task GetTodos_WhenNoTodoExists_ReturnsEmptyArray()
    {
        using var factory = new TodoApiTestFactory();
        using var client = factory.CreateClient();

        // まだTodoを作成していない新しいAPIインスタンスなので、一覧は空のはずです。
        var response = await client.GetAsync("/todos");

        response.EnsureSuccessStatusCode();

        // ReadFromJsonAsync<JsonArray>() は、レスポンス本文のJSON配列を読み取ります。
        // /todos は配列を返すAPIなので JsonArray を使います。
        var todos = await response.Content.ReadFromJsonAsync<JsonArray>();

        // Assert.NotNull は、値がnullではないことを確認します。
        Assert.NotNull(todos);

        // Assert.Empty は、配列やListの要素数が0であることを確認します。
        Assert.Empty(todos);
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
}
