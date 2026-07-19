using Microsoft.Extensions.Options;

namespace TodoApi.Tests;

public class ApiKeyOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidLegacyConfiguration_Succeeds()
    {
        var validator = new ApiKeyOptionsValidator();
        var options = new ApiKeyOptions
        {
            ApiKey = "primary-key",
            Permissions = [ApiKeyClaimDefaults.TodoWritePermission]
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithDuplicateKeys_Fails()
    {
        var validator = new ApiKeyOptionsValidator();
        var options = new ApiKeyOptions
        {
            ApiKey = "same-key",
            Permissions = [ApiKeyClaimDefaults.TodoWritePermission],
            AdditionalApiKeys = ["same-key"]
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WithIncompleteClient_Fails()
    {
        var validator = new ApiKeyOptionsValidator();
        var options = new ApiKeyOptions
        {
            Clients =
            [
                new ApiKeyClientOptions
                {
                    Name = "read-only-client",
                    Key = "client-key"
                }
            ]
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
    }
}
