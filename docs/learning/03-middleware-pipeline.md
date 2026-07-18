# Middlewareとリクエストパイプラインを理解する

Middlewareは、HTTPリクエストとレスポンスの途中に処理を差し込む仕組みです。

## 1. リクエストの流れ

ASP.NET Coreでは、リクエストは複数のMiddlewareを順番に通ります。

```text
クライアント
  ↓
ExceptionHandlingMiddleware
  ↓
RequestIdMiddleware
  ↓
RequestLoggingMiddleware
  ↓
CORS
  ↓
認証・認可
  ↓
レート制限
  ↓
エンドポイント
  ↓
レスポンス
```

`Program.cs`の次のコードがMiddlewareを登録しています。

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
```

## 2. Middlewareの基本形

Middlewareは、次の処理を表す`RequestDelegate`を受け取ります。

```csharp
public class SimpleMiddleware
{
    private readonly RequestDelegate _next;

    public SimpleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // エンドポイントへ進む前の処理
        Console.WriteLine("Before");

        await _next(context);

        // エンドポイントから戻った後の処理
        Console.WriteLine("After");
    }
}
```

`_next`は「次のMiddlewareまたはエンドポイントを実行する処理」です。

## 3. 実行順序

Middleware A、B、エンドポイントをこの順番で登録します。

```csharp
app.UseMiddleware<MiddlewareA>();
app.UseMiddleware<MiddlewareB>();
app.MapGet("/", () => "OK");
```

実行順序は次のようになります。

```text
Aの前半
  ↓
Bの前半
  ↓
エンドポイント
  ↓
Bの後半
  ↓
Aの後半
```

Middlewareは、リクエスト処理の周囲を囲むように動きます。

## 4. 現在のMiddleware

### ExceptionHandlingMiddleware

最も外側で例外を捕捉します。

```csharp
try
{
    await _next(context);
}
catch (Exception exception)
{
    _logger.LogError(exception, "Unhandled exception while processing the request.");
    // 500のProblemDetailsを返す
}
```

後続の処理で例外が発生しても、このMiddlewareまで戻ってくるため、共通のエラーレスポンスに変換できます。

### RequestIdMiddleware

`X-Request-Id`を用意してから、次の処理へ進みます。

```csharp
context.Response.Headers["X-Request-Id"] = requestId;
await _next(context);
```

後続のログとレスポンスへ、同じIDを使えるようにしています。

### RequestLoggingMiddleware

`try`と`finally`を使い、成功・失敗に関係なく処理時間を記録します。

```csharp
try
{
    await _next(context);
}
finally
{
    _logger.LogInformation(
        "HTTP {HttpMethod} {Path} returned {StatusCode}",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode
    );
}
```

## 5. 順番が重要な理由

例えば、例外処理Middlewareを後ろに置くと、前にあるMiddlewareの例外を捕捉できません。

```csharp
// 例外を全体で捕捉したいので、最初に置く
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

Request IDも、ログやエラーレスポンスで使いたいため、早い段階で設定します。

Middlewareの順番は、次の2つに影響します。

- どの処理がどの処理を囲むか
- どのMiddlewareが前後の処理を観測できるか

## 練習問題

次の登録順で、ログの出力順を考えてみてください。

```csharp
app.UseMiddleware<FirstMiddleware>();
app.UseMiddleware<SecondMiddleware>();
app.MapGet("/", () => "OK");
```

各Middlewareが次の実装を持つとします。

```csharp
Console.WriteLine("First before");
await _next(context);
Console.WriteLine("First after");
```

```csharp
Console.WriteLine("Second before");
await _next(context);
Console.WriteLine("Second after");
```

答え:

```text
First before
Second before
エンドポイント
Second after
First after
```
