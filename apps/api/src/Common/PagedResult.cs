namespace API.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalPages { get; set; }
    public int TotalItemsCount { get; set; }
    public int ItemsFrom { get; set; }
    public int ItemsTo { get; set; }
    public PagedResult(IEnumerable<T> items, int totalCount, int pageSize, int pageNumber)
    {
        Items = items;
        TotalItemsCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        // long arithmetic: same int32 overflow guard as the repository Skip.
        long from = (long)pageSize * (pageNumber - 1) + 1;
        ItemsFrom = totalCount == 0 ? 0 : (from > int.MaxValue ? int.MaxValue : (int)from);
        long to = (long)ItemsFrom + pageSize - 1;
        ItemsTo = (int)Math.Min(to, (long)totalCount);
    }
}