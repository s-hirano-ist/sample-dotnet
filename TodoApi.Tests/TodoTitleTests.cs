namespace TodoApi.Tests;

public class TodoTitleTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsValueObject()
    {
        var result = TodoTitle.Create("Learn Value Objects");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Learn Value Objects", result.Value.Value);
        Assert.Equal("Learn Value Objects", result.Value.ToString());
    }

    [Theory]
    [InlineData(null, "title_required")]
    [InlineData("", "title_required")]
    [InlineData("   ", "title_required")]
    public void Create_WithBlankValue_ReturnsRequiredError(string? value, string expectedCode)
    {
        var result = TodoTitle.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error?.Code);
    }

    [Fact]
    public void Create_WithTooLongValue_ReturnsLengthError()
    {
        var result = TodoTitle.Create(new string('a', TodoRules.MaxTitleLength + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("title_too_long", result.Error?.Code);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        var first = TodoTitle.Create("Same title").Value!;
        var second = TodoTitle.Create("Same title").Value!;

        Assert.Equal(first, second);
    }
}
