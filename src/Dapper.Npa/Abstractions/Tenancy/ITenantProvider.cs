namespace Dapper.Npa.Abstractions.Tenancy;

public interface ITenantProvider
{
    object GetCurrentTenantId();
}
