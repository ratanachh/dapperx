namespace DapperX.Runtime.Converters;
using DapperX.Abstractions.Converters;
public sealed class EnumToStringConverter<TEnum> : IValueConverter<TEnum, string> where TEnum : struct, Enum
{
    public string ToColumn(TEnum value) => value.ToString();
    public TEnum ToProperty(string value) => Enum.Parse<TEnum>(value);
}
