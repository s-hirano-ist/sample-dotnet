# Domain Event

## 今回の目的

Todoの状態変化を「発生した事実」として記録し、ログや将来の通知処理をEntityやUse Caseから分離します。

## 1. Domain Eventとは

Domain Eventは、ドメイン上で起きた重要な事実を表します。

```csharp
new TodoCompletedDomainEvent(todo.Id, completedAt)
```

これは「Todoを完了させる処理」ではなく、「Todoが完了した」という事実です。イベント自身はログ出力やメール送信を行いません。

## 2. Entityがイベントを保持する

`TodoItem.Complete`や`TodoItem.Reopen`が状態を変更すると、Entity内部のイベント一覧へイベントを追加します。

```csharp
todo.Complete(now);
var events = todo.DequeueDomainEvents();
```

同じTodoをすでに完了済みの状態で再度完了しても、完了イベントは二重に発行しません。状態が変わったときだけイベントを作ります。

## 3. 保存とイベント発行の順序

Use Caseでは次の順番にしています。

1. Entityの状態を変更する
2. RepositoryでDBへ保存する
3. 保存成功後にDomain EventをDispatcherへ渡す

先に通知してDB保存に失敗すると、「完了した」という通知だけが届く不整合が起きるためです。

厳密な本番運用で外部メッセージキューへ確実に届ける場合は、Outboxパターンを追加で検討します。今回はインプロセスDispatcherで基本構造を学びます。

## 4. Dispatcherの責務

`IDomainEventDispatcher`はイベントを処理する契約です。現在の実装は`LoggingDomainEventDispatcher`で構造化ログへ記録します。

将来、イベントごとに次のような処理へ差し替えられます。

- 監査ログを保存する
- 通知を送信する
- メッセージキューへ発行する
- 検索インデックスを更新する

Use Caseは具体的な副作用を知らず、Dispatcherの契約だけを知ります。

## 学習ポイント

- Domain Eventは処理ではなく発生した事実
- Entityは状態変化とイベント発生を近くに保つ
- DB保存成功後にイベントを発行する
- Dispatcherで副作用を分離する
- 外部システムへの確実な配信には将来Outboxが必要になる

## テスト実行について

このプロジェクトのAPIテストは、`WebApplicationFactory`とテストごとのインメモリSQLiteを使います。複数のホストを同時に起動すると、テストホストや生成物の競合が起こりやすいため、テストアセンブリでは並列実行を無効にしています。
