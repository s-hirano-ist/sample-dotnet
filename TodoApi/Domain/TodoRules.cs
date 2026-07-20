// TodoRulesは、Todo自身が守るドメインルールをまとめます。
// APIの入力検証からも参照しますが、ルールの所有者はDomainです。
public static class TodoRules
{
    public const int MaxTitleLength = 100;
}
