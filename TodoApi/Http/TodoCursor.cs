using System.Text;
using System.Text.Json;

// TodoCursorは、次のページの位置をクライアントから隠すためのトークンです。
public static class TodoCursor
{
    public static string Create(int lastTodoId)
    {
        var payload = JsonSerializer.Serialize(new CursorPayload(1, lastTodoId));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static bool TryParse(string? value, out int lastTodoId)
    {
        lastTodoId = 0;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 200)
        {
            return false;
        }

        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json);

            if (payload is null || payload.Version != 1 || payload.LastTodoId < 1)
            {
                return false;
            }

            lastTodoId = payload.LastTodoId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record CursorPayload(int Version, int LastTodoId);
}
