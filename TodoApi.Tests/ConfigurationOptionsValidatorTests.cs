namespace TodoApi.Tests;

public class ConfigurationOptionsValidatorTests
{
    [Fact]
    public void CorsValidator_WithInvalidOrigin_Fails()
    {
        var result = new CorsOptionsValidator().Validate(null, new CorsOptions
        {
            AllowedOrigins = ["localhost:3000"],
            AllowedMethods = ["GET"],
            AllowedHeaders = ["Content-Type"]
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void RateLimitValidator_WithZeroPermitLimit_Fails()
    {
        var result = new RateLimitOptionsValidator().Validate(null, new RateLimitOptions
        {
            Store = "Memory",
            PermitLimit = 0,
            WindowSeconds = 10
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void RateLimitValidator_WithValidRedisConfiguration_Succeeds()
    {
        var result = new RateLimitOptionsValidator().Validate(null, new RateLimitOptions
        {
            Store = "Redis",
            PermitLimit = 10,
            WindowSeconds = 10
        });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("Postgres")]
    [InlineData("PostgreSQL")]
    public void DatabaseValidator_WithSupportedProvider_Succeeds(string provider)
    {
        var result = new DatabaseOptionsValidator().Validate(null, new DatabaseOptions
        {
            Provider = provider
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DatabaseValidator_WithUnsupportedProvider_Fails()
    {
        var result = new DatabaseOptionsValidator().Validate(null, new DatabaseOptions
        {
            Provider = "MySql"
        });

        Assert.True(result.Failed);
    }
}
