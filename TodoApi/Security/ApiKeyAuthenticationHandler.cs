using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

// ApiKeyAuthenticationHandlerは、X-API-Keyヘッダーを確認する認証処理です。
// 学習用の簡易実装であり、本番環境では専用の認証基盤やシークレット管理を使います。
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiKeyOptions _apiKeyOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeyOptions> apiKeyOptions
    ) : base(options, logger, encoder)
    {
        _apiKeyOptions = apiKeyOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // HTTPヘッダーからクライアントが送ったAPIキーを取得します。
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var providedApiKey))
        {
            // キーがない場合は「認証情報がない」として扱います。
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // 固定時間比較は、キーの比較時間から値を推測されにくくするために使います。
        // ローテーション中は、現在のキーと追加キーをすべて確認します。
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey.ToString());
        var credentials = new List<ApiKeyCredential>();

        if (!string.IsNullOrWhiteSpace(_apiKeyOptions.ApiKey))
        {
            credentials.Add(
                new ApiKeyCredential(
                    _apiKeyOptions.ApiKey,
                    ApiKeyClaimDefaults.ClientName,
                    _apiKeyOptions.Permissions
                )
            );
        }

        credentials.AddRange(
            _apiKeyOptions.AdditionalApiKeys.Select(
                key => new ApiKeyCredential(
                    key,
                    ApiKeyClaimDefaults.ClientName,
                    _apiKeyOptions.Permissions
                )
            )
        );
        credentials.AddRange(
            _apiKeyOptions.Clients.Select(
                client => new ApiKeyCredential(client.Key, client.Name, client.Permissions)
            )
        );

        ApiKeyCredential? matchedCredential = null;
        foreach (var credential in credentials)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(credential.Key);
            var isMatch = KeysMatch(providedBytes, expectedBytes);

            if (isMatch)
            {
                matchedCredential = credential;
            }
        }

        if (matchedCredential is null)
        {
            // APIキーの値は記録せず、失敗したパスだけを監査ログへ残します。
            Logger.LogWarning(
                "API key authentication failed for path {Path}.",
                Request.Path
            );
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        // 認証に成功したユーザーを表すClaimsPrincipalを作ります。
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, matchedCredential.Name)
        };

        // 設定された権限をClaimへ変換します。
        claims.AddRange(
            matchedCredential.Permissions.Select(
                permission => new Claim(ApiKeyClaimDefaults.PermissionClaimType, permission)
            )
        );
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private sealed record ApiKeyCredential(string Key, string Name, string[] Permissions);

    private static bool KeysMatch(byte[] providedBytes, byte[] expectedBytes)
    {
        // 入力を固定長のダイジェストへ変換してから比較します。
        // これにより、元のキーの長さに依存せず比較できます。
        var providedDigest = SHA256.HashData(providedBytes);
        var expectedDigest = SHA256.HashData(expectedBytes);
        return CryptographicOperations.FixedTimeEquals(providedDigest, expectedDigest);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // 認証情報の詳細は返さず、クライアントが扱いやすいProblemDetails形式にします。
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://httpstatuses.com/401",
            title = "Authentication is required.",
            status = StatusCodes.Status401Unauthorized
        };

        await JsonSerializer.SerializeAsync(Response.Body, problemDetails, JsonSerializerOptions.Web);
    }
}
