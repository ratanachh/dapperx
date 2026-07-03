using DapperX.Core.Enums;

namespace DapperX.Core.Models;

using Core.Enums;

public sealed record ProcParam(string Name, ParameterMode Mode = ParameterMode.In, Type? ClrType = null);
