// TodoTitleは、Todoのタイトルを表すValue Objectです。
// 文字列そのものではなく、タイトルとして有効であることを保証した値として扱います。
public sealed record TodoTitle
{
    public string Value { get; }

    private TodoTitle(string value)
    {
        Value = value;
    }

    // Create以外から無効なTodoTitleを作れないようにします。
    public static DomainResult<TodoTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainResult<TodoTitle>.Failure(
                "title_required",
                "Title is required."
            );
        }

        if (value.Length > TodoRules.MaxTitleLength)
        {
            return DomainResult<TodoTitle>.Failure(
                "title_too_long",
                $"Title must be {TodoRules.MaxTitleLength} characters or fewer."
            );
        }

        return DomainResult<TodoTitle>.Success(new TodoTitle(value));
    }

    public override string ToString() => Value;
}
