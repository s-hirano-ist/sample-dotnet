// DomainErrorは、ドメインルールに違反した理由を表します。
// APIのApiErrorとは分け、Domain層がHTTPへ依存しないようにします。
public sealed record DomainError(string Code, string Message);
