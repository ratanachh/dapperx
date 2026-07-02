namespace Dapper.Npa.Runtime.Utilities;
public static class TypeHelper
{
    public static bool IsNullable(Type t) => !t.IsValueType || Nullable.GetUnderlyingType(t) is not null;
    public static Type UnwrapNullable(Type t) => Nullable.GetUnderlyingType(t) ?? t;
}
