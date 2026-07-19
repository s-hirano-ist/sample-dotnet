// ApiKeyClaimDefaultsは、APIキー認証が作るClaimの名前と値をまとめます。
public static class ApiKeyClaimDefaults
{
    public const string PermissionClaimType = "permission";
    public const string TodoWritePermission = "todo:write";
    public const string ClientName = "api-client";
}
