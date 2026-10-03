using OptixMovies.Core;

namespace OptixMovies.Api.Contracts;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public static class PageResponse
{
    public static PageResponse<TResponse> From<TItem, TResponse>(Page<TItem> page, Func<TItem, TResponse> toResponse) =>
        new([.. page.Items.Select(toResponse)], page.Number, page.Size, page.TotalCount);
}
