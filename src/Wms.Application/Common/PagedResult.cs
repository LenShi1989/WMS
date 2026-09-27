namespace Wms.Application.Common;

/// <summary>分頁查詢結果。</summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public PagedResult() { }

    public PagedResult(List<T> items, int page, int pageSize, int total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}

/// <summary>分頁查詢共用參數。</summary>
public class PagedQuery
{
    private const int MaxPageSize = 200;
    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>關鍵字模糊查詢。</summary>
    public string? Keyword { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
