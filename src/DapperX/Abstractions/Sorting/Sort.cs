namespace DapperX.Abstractions.Sorting;

/// <summary>A single ORDER BY clause: the mapped column name to sort by and its direction, passed to sorted overloads on <see cref="DapperX.Abstractions.Repositories.IRepository{T, TId}"/>.</summary>
/// <param name="Column">The entity property name (not the raw SQL column name) to sort by.</param>
/// <param name="Ascending">Whether to sort ascending (<c>true</c>, the default) or descending.</param>
public sealed record Sort(string Column, bool Ascending = true);
