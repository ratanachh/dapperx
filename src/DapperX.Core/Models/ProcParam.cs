namespace DapperX.Core.Models;

using DapperX.Core.Enums;

public sealed record ProcParam(string Name, ParameterMode Mode = ParameterMode.In, Type? ClrType = null);
