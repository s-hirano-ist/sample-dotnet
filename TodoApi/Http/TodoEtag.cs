using System.Security.Cryptography;
using System.Text;

// TodoEtagは、Todoの現在内容からHTTPキャッシュ用のETagを作ります。
public static class TodoEtag
{
    public static string Create(TodoItem todo)
    {
        var value = string.Join(
            "|",
            todo.Id,
            todo.Title,
            todo.IsDone,
            todo.CreatedAt.ToString("O"),
            todo.CompletedAt?.ToString("O") ?? string.Empty
        );
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"\"{Convert.ToHexString(digest)}\"";
    }
}
