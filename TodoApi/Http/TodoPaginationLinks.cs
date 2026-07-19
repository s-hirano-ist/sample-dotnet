using Microsoft.AspNetCore.WebUtilities;

// TodoPaginationLinksは、ページング結果をたどるためのLinkヘッダーを作ります。
// ページ情報をJSONへ追加するだけでなく、HTTPヘッダーでも次のURLを返す設計を学びます。
public static class TodoPaginationLinks
{
    public static string Create(HttpRequest request, TodoListResponse response)
    {
        var links = new List<string>
        {
            $"<{BuildUri(request, response, 1)}>; rel=\"first\"",
            $"<{BuildUri(request, response, response.Page)}>; rel=\"self\"",
        };

        if (response.Page > 1)
        {
            links.Add($"<{BuildUri(request, response, response.Page - 1)}>; rel=\"prev\"");
        }

        if (response.Page < response.TotalPages)
        {
            links.Add($"<{BuildUri(request, response, response.Page + 1)}>; rel=\"next\"");
        }

        if (response.TotalPages > 0)
        {
            links.Add($"<{BuildUri(request, response, response.TotalPages)}>; rel=\"last\"");
        }

        return string.Join(", ", links);
    }

    private static string BuildUri(
        HttpRequest request,
        TodoListResponse response,
        int page
    )
    {
        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("page", page.ToString()),
            new("pageSize", response.PageSize.ToString()),
        };

        if (request.Query.TryGetValue("isDone", out var isDone))
        {
            queryParameters.Add(new("isDone", isDone.ToString()));
        }

        if (request.Query.TryGetValue("search", out var search) && !string.IsNullOrWhiteSpace(search))
        {
            queryParameters.Add(new("search", search.ToString().Trim()));
        }

        if (request.Query.TryGetValue("sortBy", out var sortBy))
        {
            queryParameters.Add(new("sortBy", sortBy.ToString()));
        }

        if (request.Query.TryGetValue("sortOrder", out var sortOrder))
        {
            queryParameters.Add(new("sortOrder", sortOrder.ToString()));
        }

        return QueryHelpers.AddQueryString(request.Path, queryParameters);
    }
}
