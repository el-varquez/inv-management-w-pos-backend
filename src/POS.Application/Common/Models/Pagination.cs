namespace POS.Application.Common.Models;

public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page is null || page < 1 ? 1 : page.Value;
        var normalizedSize = pageSize is null
            ? DefaultPageSize
            : Math.Clamp(pageSize.Value, MinPageSize, MaxPageSize);
        return (normalizedPage, normalizedSize);
    }
}
