namespace Dapper.Npa.Abstractions.Paging;

public sealed record Pageable(int PageNumber, int PageSize)
{
    public int Offset => PageNumber * PageSize;
}
