namespace Dapper.Npa.Runtime.Converters;
using Dapper.Npa.Abstractions.Converters;
public sealed class EnumToStringConverter<TEnum> : IValueConverter<TEnum, string> where TEnum : struct, Enum
{
    public string ToColumn(TEnum value) => value.ToString();
    public TEnum ToProperty(string value) => Enum.Parse<TEnum>(value);
}
