using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

// WebApplicationFactory は、テスト用にASP.NET Coreアプリをメモリ上で起動します。
// 実際のポートは開かず、HttpClientからAPIを呼び出せます。
public class TodoApiTests
{
    [Fact]
    public async Task GetRoot_ReturnsRunningMessage()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Todo API is running.", body);
    }

    [Fact]
    public async Task GetTodos_WhenNoTodoExists_ReturnsEmptyArray()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/todos");

        response.EnsureSuccessStatusCode();

        var todos = await response.Content.ReadFromJsonAsync<JsonArray>();
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task PostTodos_WithValidTitle_CreatesTodo()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var request = new
        {
            title = "Learn automated testing"
        };

        var response = await client.PostAsJsonAsync("/todos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/todos/1", response.Headers.Location?.OriginalString);

        var todo = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(todo);
        Assert.Equal(1, todo["id"]?.GetValue<int>());
        Assert.Equal("Learn automated testing", todo["title"]?.GetValue<string>());
        Assert.False(todo["isDone"]?.GetValue<bool>());
        Assert.NotNull(todo["createdAt"]);
        Assert.Null(todo["completedAt"]);
    }

    [Fact]
    public async Task PostTodos_WithBlankTitle_ReturnsBadRequest()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var request = new
        {
            title = "   "
        };

        var response = await client.PostAsJsonAsync("/todos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = await response.Content.ReadFromJsonAsync<string>();
        Assert.Equal("Title is required.", message);
    }

    [Fact]
    public async Task GetTodo_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/todos/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
