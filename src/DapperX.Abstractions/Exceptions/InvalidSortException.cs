namespace DapperX.Abstractions.Exceptions;

public sealed class InvalidSortException(string column)
    : Exception($"'{column}' is not a sortable column. Mark the property with [Sortable].");
