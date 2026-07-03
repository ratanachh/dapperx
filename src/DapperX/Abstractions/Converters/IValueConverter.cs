namespace DapperX.Abstractions.Converters;

public interface IValueConverter<TProperty, TColumn>
{
    TColumn ToColumn(TProperty value);
    TProperty ToProperty(TColumn value);
}
