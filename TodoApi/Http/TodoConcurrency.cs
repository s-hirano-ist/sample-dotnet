// TodoConcurrencyは、Todo更新・削除で共有する条件付きリクエスト判定です。
public static class TodoConcurrency
{
    public static bool MatchesIfMatch(HttpRequest request, string currentEtag)
    {
        if (!request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            // If-Matchがない場合は、既存クライアントとの互換性のため許可します。
            return true;
        }

        return ifMatch.Any(value =>
            value?.Split(',').Any(candidate => candidate.Trim() == currentEtag) == true
        );
    }
}
