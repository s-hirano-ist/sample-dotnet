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
}
