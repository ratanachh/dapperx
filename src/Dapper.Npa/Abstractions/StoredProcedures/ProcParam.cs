namespace Dapper.Npa.Abstractions.StoredProcedures;
public sealed record ProcParam(string Name, ParameterMode Mode = ParameterMode.In, Type? ClrType = null);
