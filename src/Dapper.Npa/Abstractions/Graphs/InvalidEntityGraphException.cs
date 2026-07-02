namespace Dapper.Npa.Abstractions.Graphs;

public sealed class InvalidEntityGraphException(string graphName)
    : Exception($"Named entity graph '{graphName}' is not defined on this entity.");
