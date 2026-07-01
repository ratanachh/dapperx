namespace DapperX.Abstractions.Tenancy;

public interface ITenantProvider
{
    object GetCurrentTenantId();
}
