namespace Dapper.Npa.Runtime.Converters;
using Dapper.Npa.Abstractions.Converters;
public sealed class UtcDateTimeConverter : IValueConverter<DateTime, DateTime>
{
    public DateTime ToColumn(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    public DateTime ToProperty(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
