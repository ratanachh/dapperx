namespace DapperX.Generator.Models;

internal sealed class GlobalFilterModel
{
    public string Name { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    /// <summary>Sanitised constant name: FILTER_ActiveRegion etc.</summary>
    public string ConstantName => $"FILTER_{string.Concat(Name.Select((c, i) => i == 0 ? char.ToUpper(c) : c))}";
}
