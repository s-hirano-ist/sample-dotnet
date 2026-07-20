// これらは「何が起きたか」を表すイベントです。
// ここでは副作用を実行せず、発生した事実だけを保持します。
public sealed record TodoCreatedDomainEvent(int TodoId) : IDomainEvent;

public sealed record TodoTitleChangedDomainEvent(int TodoId) : IDomainEvent;

public sealed record TodoCompletedDomainEvent(int TodoId, DateTimeOffset CompletedAt) : IDomainEvent;

public sealed record TodoReopenedDomainEvent(int TodoId) : IDomainEvent;

public sealed record TodoDeletedDomainEvent(int TodoId) : IDomainEvent;
