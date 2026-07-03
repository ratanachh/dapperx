using DapperX.Abstractions.Converters;

namespace DapperX.Runtime.Converters;
using DapperX.Abstractions.Converters;
public sealed class UtcDateTimeConverter : IValueConverter<DateTime, DateTime>
{
    public DateTime ToColumn(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    public DateTime ToProperty(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
