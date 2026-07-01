namespace DapperX.Abstractions.Exceptions;

public sealed class UnmappedPropertyException(Type entityType, string propertyName)
    : Exception($"Property '{propertyName}' on '{entityType.Name}' has no column mapping.");
