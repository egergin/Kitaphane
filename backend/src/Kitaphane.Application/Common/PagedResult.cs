namespace Kitaphane.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasNext => Page * PageSize < TotalCount;
}
