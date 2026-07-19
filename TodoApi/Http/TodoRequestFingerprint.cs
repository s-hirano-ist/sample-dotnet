using System.Security.Cryptography;
using System.Text;

// TodoRequestFingerprintは、同じIdempotency-Keyで異なる内容が送られたことを検出します。
public static class TodoRequestFingerprint
{
    public static string Create(CreateTodoRequest request)
    {
        var bytes = Encoding.UTF8.GetBytes(request.Title);
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest);
    }
}
