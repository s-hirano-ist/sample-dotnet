using System.Linq.Expressions;

// TodoFilterSpecificationは、Todo一覧で使う絞り込み条件を表します。
// 条件を一つのオブジェクトにまとめ、Repositoryの検索処理から分離します。
public sealed class TodoFilterSpecification
{
    public Expression<Func<TodoItem, bool>> Criteria { get; }

    public TodoFilterSpecification(bool? isDone, string? search)
    {
        var normalizedSearch = search?.Trim().ToLowerInvariant() ?? string.Empty;

        Criteria = todo =>
            (!isDone.HasValue || todo.IsDone == isDone.Value)
            && (
                normalizedSearch.Length == 0
                || todo.Title.ToLower().Contains(normalizedSearch)
            );
    }
}
