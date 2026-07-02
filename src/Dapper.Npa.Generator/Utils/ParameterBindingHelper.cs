using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Utils;

using Generator.Models;

/// <summary>Maps C# parameter variables to Dapper names matching SQL placeholders (e.g. @Id requires Id = id, not id).</summary>
internal static class ParameterBindingHelper
{
    public static string IdAssignment(EntityModel entity, string variableName = "id")
        => $"{GetIdPropertyName(entity)} = {variableName}";

    public static string GetIdPropertyName(EntityModel entity)
        => entity.Properties.First(p => p.IsId).PropertyName;
}
