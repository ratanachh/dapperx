namespace Dapper.Npa.Generator.Models;

internal sealed class EntityListenerModel
{
    public string TypeFqn { get; init; } = string.Empty;
    public IReadOnlyList<LifecycleMethodModel> Methods { get; init; } = [];
}
