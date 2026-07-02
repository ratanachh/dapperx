namespace Dapper.Npa.Core.Enums;
[Flags]
public enum CascadeType { None = 0, Persist = 1, Merge = 2, Remove = 4, All = Persist | Merge | Remove }
