using System.Data;
using Dapper;

namespace DapperX.IntegrationTests.Shared;

/// <summary>Maps SQLite TEXT tenant_id columns to <see cref="Guid"/> for Dapper materialization.</summary>
internal sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => value switch
    {
        Guid g => g,
        string s => Guid.Parse(s),
        byte[] bytes when bytes.Length == 16 => new Guid(bytes),
        _ => throw new DataException($"Cannot convert {value?.GetType().Name ?? "null"} to Guid.")
    };

    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
}
