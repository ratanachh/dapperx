namespace Dapper.Npa.Runtime.Converters;
using Dapper.Npa.Abstractions.Converters;
public sealed class EnumToIntConverter<TEnum> : IValueConverter<TEnum, int> where TEnum : struct, Enum
{
    public int ToColumn(TEnum value) => Convert.ToInt32(value);
    public TEnum ToProperty(int value) => (TEnum)Enum.ToObject(typeof(TEnum), value);
}
