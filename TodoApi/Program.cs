using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using StackExchange.Redis;

// WebApplication.CreateBuilder は、ASP.NET Coreアプリを作るための準備をします。
// args には、コマンドライン引数が入ります。今は特別な引数を使っていません。
var builder = WebApplication.CreateBuilder(args);

// CORSは、ブラウザから別のオリジンにあるAPIを呼び出すときの許可ルールです。
// 許可するオリジンはコードに直接書かず、appsettings.jsonから読み込みます。
builder.Services
    .AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection("Cors"))
    .Validate(
        options =>
            options.AllowedOrigins.Length > 0
            && options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ),
        "Cors:AllowedOrigins must contain at least one absolute HTTP or HTTPS origin."
    )
    .Validate(
        options => options.AllowedMethods.Length > 0 && options.AllowedHeaders.Length > 0,
        "Cors:AllowedMethods and Cors:AllowedHeaders must not be empty."
    )
    .ValidateOnStart();

// CORSの設定をOptionsと同じ型へ読み込み、ポリシー作成に使います。
var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();
var allowedOrigins = corsOptions.AllowedOrigins;

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods(corsOptions.AllowedMethods)
            .WithHeaders(corsOptions.AllowedHeaders);
    });
});

// AddAuthenticationは、リクエストの認証方法を登録します。
// 今回はX-API-Keyを確認する独自の認証ハンドラーを使います。
builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection("Authentication"))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "Authentication:ApiKey must be configured."
    )
    .ValidateOnStart();

builder.Services
    .AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

// AddAuthorizationは、認証済みかどうかによってエンドポイントへのアクセスを制御します。
builder.Services.AddAuthorization();

// レート制限の設定をOptionsへ束ね、起動時に値を検証します。
builder.Services
    .AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection("RateLimit"))
    .Validate(
        options =>
            (string.Equals(options.Store, "Memory", StringComparison.OrdinalIgnoreCase)
                || string.Equals(options.Store, "Redis", StringComparison.OrdinalIgnoreCase))
            && options.PermitLimit > 0
            && options.WindowSeconds > 0,
        "RateLimit:Store must be Memory or Redis, and limits must be greater than zero."
    )
    .ValidateOnStart();

// AddRateLimiterは、一定時間内のリクエスト数を制限する機能を登録します。
// 過剰なアクセスや、意図しない大量リクエストからAPIを守るために使います。
var rateLimitStore = builder.Configuration.GetValue<string>("RateLimit:Store") ?? "Memory";
var useRedisRateLimit = string.Equals(rateLimitStore, "Redis", StringComparison.OrdinalIgnoreCase);
var rateLimitPermitLimit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 10);
var rateLimitWindowSeconds = builder.Configuration.GetValue<int>("RateLimit:WindowSeconds", 10);

if (useRedisRateLimit)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        throw new InvalidOperationException(
            "RateLimit:Store is Redis, but ConnectionStrings:Redis is not configured."
        );
    }

    // Redis接続はアプリケーション全体で共有するため、Singletonとして登録します。
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConnectionString)
    );
}
else
{
    builder.Services.AddRateLimiter(options =>
    {
        // クライアントごとに別の固定ウィンドウを作ります。
        options.AddPolicy("api", httpContext =>
        {
            // 認証済みならユーザー名、未認証なら接続元IPを制限キーにします。
            var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                ? $"user:{httpContext.User.Identity.Name}"
                : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitPermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                    QueueLimit = 0
                }
            );
        });

        // 制限を超えたリクエストにはHTTP 429を返します。
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Retry-Afterは、クライアントへ「何秒後に再試行してよいか」を伝えるHTTPヘッダーです。
        options.OnRejected = (context, _) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = rateLimitWindowSeconds.ToString();
            return ValueTask.CompletedTask;
        };
    });
}

// AddHealthChecksは、アプリが正常に動作できるか確認する機能を登録します。
// AddDbContextCheckは、TodoDbContextを使ってSQLiteへ接続できるかも確認対象にします。
var healthChecks = builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<TodoDbContext>();

if (useRedisRateLimit)
{
    // Redisモードでは、DBに加えてRedisも正常性チェックの対象にします。
    healthChecks.AddCheck<RedisHealthCheck>("redis");
}

// AddOpenApi は、アプリのエンドポイントからOpenAPI形式の仕様書を作る機能を登録します。
// OpenAPIは、APIのURL、HTTPメソッド、リクエスト、レスポンスなどを機械可読な形で表す標準です。
builder.Services.AddOpenApi(options =>
{
    // 実際のX-API-Key認証をOpenAPI仕様へ登録します。
    options.AddDocumentTransformer<ApiKeyDocumentTransformer>();
    // 認証が必要な操作へ、APIキーを使うことを明示します。
    options.AddOperationTransformer<ApiKeyOperationTransformer>();
});

// AddDbContext は、Entity Framework Coreで使うDbContextをDIコンテナに登録します。
// ConnectionStrings:TodoDatabase は appsettings.json に書いたSQLiteの接続先です。
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDatabase")));

// TodoServiceは、Todoの作成・取得・更新・削除の処理をまとめたサービスです。
// AddScoped は、HTTPリクエストごとに1つのインスタンスを作る登録方法です。
builder.Services.AddScoped<TodoService>();

// Build を呼ぶと、実際に起動できるWebアプリケーションの本体が作られます。
var app = builder.Build();

// すべてのリクエストにRequest IDを付けるため、早い段階でミドルウェアを実行します。
app.UseMiddleware<RequestIdMiddleware>();

// Request IDのスコープ内で例外を記録するため、RequestIdの後に実行します。
// 後続のミドルウェアやエンドポイントの例外を共通のJSONレスポンスへ変換します。
app.UseMiddleware<ExceptionHandlingMiddleware>();

// APIレスポンスへ、ブラウザ向けの基本的なセキュリティヘッダーを追加します。
app.UseMiddleware<SecurityHeadersMiddleware>();

// HTTPの結果と処理時間を記録するため、Request IDの後に実行します。
app.UseMiddleware<RequestLoggingMiddleware>();

// 登録したCORSポリシーをHTTPリクエストへ適用します。
app.UseCors("Frontend");

// 認証・認可ミドルウェアをエンドポイントより前に配置します。
app.UseAuthentication();
app.UseAuthorization();

if (useRedisRateLimit)
{
    // Redisモードでは、全コンテナで共有できるミドルウェアを使います。
    app.UseMiddleware<DistributedRateLimitMiddleware>();
}
else
{
    // Memoryモードでは、ASP.NET Core標準のプロセス内レートリミッターを使います。
    app.UseRateLimiter();
}

// MapOpenApi は、OpenAPI仕様書をJSONで公開するエンドポイントを追加します。
// 開発中は /openapi/v1.json にアクセスすると、API仕様を確認できます。
app.MapOpenApi();

// Swagger UIは、OpenAPI仕様書をブラウザで見たり、APIを試したりするための画面です。
// API仕様書そのものは公開できますが、操作画面は開発環境だけで有効にします。
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        // Swagger UIに、前で公開したOpenAPI JSONの場所を教えます。
        options.SwaggerEndpoint("/openapi/v1.json", "Todo API v1");
    });
}

// Migrate は、未適用のマイグレーションをデータベースへ反映します。
// 今回はハンズオンを簡単にするため、起動時にSQLiteのテーブルを自動作成します。
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    dbContext.Database.Migrate();
}

// GET / にアクセスされたときの処理です。
// () => ... はラムダ式で、「引数なしで、この値を返す処理」を短く書いています。
app.MapGet("/", () => "Todo API is running.")
    .WithName("GetApiStatus")
    .WithSummary("Check API status")
    .WithDescription("Returns a simple message when the Todo API is running.")
    .Produces<string>(StatusCodes.Status200OK);

// /healthは、監視システムがアプリとデータベースの状態を確認するためのURLです。
// 正常ならHTTP 200、異常ならHTTP 503を返します。
app.MapHealthChecks(
        "/health",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }
    )
    .WithName("GetHealth")
    .WithSummary("Check application health")
    .WithDescription("Checks the database and configured external dependencies.");

// /liveは、プロセス自体が動いているかだけを確認します。
// 外部サービスを確認しないため、コンテナ再起動の判断に使えます。
app.MapHealthChecks(
        "/live",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }
    )
    .WithName("GetLiveness")
    .WithSummary("Check process liveness")
    .WithDescription("Returns healthy when the API process is running.");

// /readyは、DBやRedisなど登録済みの依存サービスを確認します。
// 依存サービスが使えないとき、ロードバランサーから外す判断に使えます。
app.MapHealthChecks(
        "/ready",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }
    )
    .WithName("GetReadiness")
    .WithSummary("Check dependency readiness")
    .WithDescription("Checks whether the API dependencies are available.");

// GET /todos は、指定されたページのTodo一覧を返します。
// クエリ文字列がない場合は、page=1、pageSize=20、全状態として扱います。
app.MapGet("/todos", async (
    int? page,
    int? pageSize,
    bool? isDone,
    string? search,
    string? sortBy,
    string? sortOrder,
    CancellationToken cancellationToken,
    TodoService todoService
) =>
{
    var currentPage = page ?? PaginationValidation.DefaultPage;
    var currentPageSize = pageSize ?? PaginationValidation.DefaultPageSize;

    var validation = PaginationValidation.Validate(currentPage, currentPageSize);

    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var sortValidation = TodoSortValidation.Validate(sortBy, sortOrder);

    if (!sortValidation.IsValid)
    {
        return Results.BadRequest(sortValidation.Error);
    }

    var todos = await todoService.GetPageAsync(
        currentPage,
        currentPageSize,
        isDone,
        search,
        sortBy,
        sortOrder,
        cancellationToken
    );

    return Results.Ok(todos);
})
    .WithName("GetTodos")
    .WithSummary("List todos")
    .WithDescription("Returns a paged list of todos with optional filtering and sorting.")
    .Produces<TodoListResponse>(StatusCodes.Status200OK)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .RequireRateLimiting("api");

// GET /todos/1 のように、URLの一部からidを受け取ります。
// {id:int} と書くことで、idは整数だけ受け付けます。
app.MapGet("/todos/{id:int}", async (int id, TodoService todoService, CancellationToken cancellationToken) =>
{
    var todo = await todoService.GetByIdAsync(id, cancellationToken);

    // 三項演算子です。
    // 条件 ? trueの場合の値 : falseの場合の値 という形で書きます。
    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
})
    .WithName("GetTodo")
    .WithSummary("Get a todo")
    .WithDescription("Returns one todo by its numeric ID.")
    .Produces<TodoItem>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .RequireRateLimiting("api");

// POST /todos は、新しいTodoを作成します。
// リクエストボディのJSONは、CreateTodoRequest型として受け取れます。
app.MapPost("/todos", async (
    CreateTodoRequest request,
    TodoService todoService,
    CancellationToken cancellationToken
) =>
{
    var validation = TodoValidation.ValidateTitle(request.Title);

    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var todo = await todoService.CreateAsync(request, cancellationToken);

    // Created はHTTP 201 Createdを返します。
    // 第1引数には、作成されたリソースのURLを入れます。
    return Results.Created($"/todos/{todo.Id}", todo);
})
    .WithName("CreateTodo")
    .WithSummary("Create a todo")
    .WithDescription("Creates a new incomplete todo.")
    .Produces<TodoItem>(StatusCodes.Status201Created)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization()
    .RequireRateLimiting("api");

// PUT /todos/1 は、指定したTodoを更新します。
// UpdateTodoRequestでは Title と IsDone を nullable にしているので、片方だけ更新できます。
app.MapPut("/todos/{id:int}", async (
    int id,
    UpdateTodoRequest request,
    TodoService todoService,
    CancellationToken cancellationToken
) =>
{
    var validation = TodoValidation.ValidateOptionalTitle(request.Title);

    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var updatedTodo = await todoService.UpdateAsync(id, request, cancellationToken);

    if (updatedTodo is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(updatedTodo);
})
    .WithName("UpdateTodo")
    .WithSummary("Update a todo")
    .WithDescription("Updates the supplied fields of an existing todo.")
    .Produces<TodoItem>(StatusCodes.Status200OK)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization()
    .RequireRateLimiting("api");

// DELETE /todos/1 は、指定したTodoを削除します。
app.MapDelete("/todos/{id:int}", async (
    int id,
    TodoService todoService,
    CancellationToken cancellationToken
) =>
{
    var deleted = await todoService.DeleteAsync(id, cancellationToken);

    if (!deleted)
    {
        return Results.NotFound();
    }

    // NoContent はHTTP 204 No Contentを返します。
    // 削除成功時は、返すデータがないので204を使います。
    return Results.NoContent();
})
    .WithName("DeleteTodo")
    .WithSummary("Delete a todo")
    .WithDescription("Deletes an existing todo.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization()
    .RequireRateLimiting("api");

// アプリを起動して、HTTPリクエストを待ち受けます。
app.Run();

// テストプロジェクトから、このMinimal APIアプリを起動できるようにするための型です。
// partial は「同じクラスの定義を別の場所にも分けて書ける」という意味です。
public partial class Program;
