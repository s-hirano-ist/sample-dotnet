# TodoTitle Value Object

## 今回の目的

Todoのタイトルを単なる`string`ではなく、Todoタイトルとして有効であることを保証した型にします。

DDDでは、値そのものに意味や制約がある場合、その値をValue Objectとして表現します。

## 1. stringのまま扱う問題

`string`はどんな文字列でも表現できます。Todoのタイトルとしては、空文字や長すぎる文字列は許可できません。各所で個別にチェックすると、チェック漏れや制約値の不一致が起こります。

## 2. TodoTitleの生成

`TodoTitle.Create`だけを生成入口にし、無効な値から`TodoTitle`を作れないようにしています。

```csharp
var result = TodoTitle.Create("Learn DDD");

if (result.IsSuccess)
{
    var title = result.Value;
}
```

失敗時は`DomainResult`にエラーコードとメッセージが入ります。Domain層はHTTPステータスコードを知らないため、API依存になりません。

## 3. recordによる値の比較

`TodoTitle`は`record`なので、同じ文字列を持つ値は同じ値として比較されます。

```csharp
var first = TodoTitle.Create("Same").Value;
var second = TodoTitle.Create("Same").Value;

first == second; // true
```

Entityの`TodoItem`は`Id`で識別しますが、Value Objectの`TodoTitle`は中身の値で比較します。この違いがEntityとValue Objectの重要な違いです。

## 4. APIとDBとの境界

APIのJSONでは、これまで通り次のような文字列を受け取ります。

```json
{ "title": "Learn DDD" }
```

DBの列も既存の文字列`Title`のままです。今回の段階では、Domain内部の生成・検証に`TodoTitle`を使い、外部契約とDBスキーマを変更しません。

このように内部モデルを先に改善すると、既存クライアントやマイグレーションへの影響を抑えながらDDDの設計を進められます。

## 学習ポイント

- Value Objectは値と制約を一つの型へまとめる
- `record`は値ベースの等価性を持つ
- Factoryを一つの生成入口にすると、不正な値を作りにくい
- DomainResultはHTTPに依存しないエラー表現
- Entityは識別子、Value Objectは値の内容で比較する
