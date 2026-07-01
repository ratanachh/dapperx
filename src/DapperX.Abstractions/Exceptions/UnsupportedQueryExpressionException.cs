namespace DapperX.Abstractions.Exceptions;

/// <summary>Thrown when a runtime query expression cannot be translated to SQL.</summary>
public sealed class UnsupportedQueryExpressionException(string message) : Exception(message);
