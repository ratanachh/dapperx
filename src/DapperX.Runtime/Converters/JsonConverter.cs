namespace DapperX.Runtime.Converters;
using System.Text.Json;
using DapperX.Abstractions.Converters;
public sealed class JsonConverter<T> : IValueConverter<T, string>
{
    public string ToColumn(T value) => JsonSerializer.Serialize(value);
    public T ToProperty(string value) => JsonSerializer.Deserialize<T>(value)!;
}
