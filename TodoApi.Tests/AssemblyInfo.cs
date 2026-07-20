using Xunit;

// WebApplicationFactoryとテストごとのインメモリSQLiteを使う統合テストは、
// 同時起動によるテストホスト・生成物の競合を避けるため順番に実行します。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
