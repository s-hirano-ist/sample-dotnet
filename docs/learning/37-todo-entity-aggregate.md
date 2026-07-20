# Todo EntityとAggregate Root

## 今回の目的

`TodoItem`を、DBから読み書きするだけのデータクラスから、Todoの状態変更ルールを自分で守るEntityへ変更しました。

DDDでは、識別子を持ち、時間が経っても同じものとして追跡されるオブジェクトをEntityと呼びます。Todoでは`Id`が識別子です。

## 1. なぜServiceから直接代入しないのか

変更前は、Serviceが次のように状態を直接書き換えていました。

```csharp
todo.IsDone = isDone;
todo.CompletedAt = isDone ? DateTimeOffset.UtcNow : null;
```

この書き方では、別の場所が`IsDone`だけを変更して`CompletedAt`を更新し忘れる可能性があります。

変更後は、Entityの操作を呼び出します。

```csharp
todo.Complete(now);
todo.Reopen();
```

完了状態と完了日時の関係を`TodoItem`自身が管理するため、呼び出し側がルールの詳細を知る必要がありません。

## 2. Aggregate Root

今回の範囲では、`TodoItem`をTodo AggregateのRootとして扱います。Rootとは、外部から変更を依頼する入口になるEntityです。

現在Todoに子Entityはありませんが、将来Todoにコメントやラベルなどを追加する場合も、外部はTodoItemを通して変更する設計にできます。

## 3. private setterとFactory

プロパティを`private set`にすると、TodoItemの外側から次のような直接変更ができません。

```csharp
todo.IsDone = true; // コンパイルエラー
```

新規作成は`TodoItem.Create`へ集めます。Entityが作成時のルールを確認し、成功または`DomainResult`を返します。

APIの`TodoValidation`も同じ`TodoRules.MaxTitleLength`を参照します。入力を早い段階で検証しつつ、Entityでも再確認する二重の防御です。

## 4. 時刻を注入する理由

Entityは現在時刻を直接取得せず、ServiceがDIされた`TimeProvider`から時刻を取得して渡します。

```csharp
var now = _timeProvider.GetUtcNow();
todo.Complete(now);
```

これにより、Entityのテストでは固定した日時を渡せます。`DateTimeOffset.UtcNow`をテスト中に呼ぶより、結果が安定します。

## 学習ポイント

- Entityはデータと、そのデータを守る振る舞いを持つ
- Aggregate Rootは状態変更の入口を一つにする
- private setterは不正な直接変更を防ぐ
- Factoryは生成時のルールを集約する
- DomainResultは通常のルール違反を戻り値で表現する
- 時刻や外部サービスはApplication/Service側から注入する
