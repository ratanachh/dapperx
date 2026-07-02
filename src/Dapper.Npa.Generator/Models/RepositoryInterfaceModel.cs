namespace Dapper.Npa.Generator.Models;

using Microsoft.CodeAnalysis;

/// <summary>
/// Developer-defined <c>[Repository]</c> interface to implement on the generated Impl class.
/// </summary>
internal sealed class RepositoryInterfaceModel
{
    public string InterfaceFqn { get; init; } = string.Empty;
    public string ImplClassName { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string IdTypeName { get; init; } = string.Empty;
    public IReadOnlyList<IMethodSymbol> DeclaredMethods { get; init; } = [];
}
