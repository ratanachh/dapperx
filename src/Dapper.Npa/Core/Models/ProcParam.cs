using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Core.Models;

using Core.Enums;

public sealed record ProcParam(string Name, ParameterMode Mode = ParameterMode.In, Type? ClrType = null);
