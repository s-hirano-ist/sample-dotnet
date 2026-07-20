// IDomainEventDispatcherは、保存成功後にDomain Eventを処理する契約です。
// Use Caseはイベントの具体的な処理方法を知りません。
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken
    );
}
