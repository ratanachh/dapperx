using DapperX.Abstractions.Tenancy;

namespace DapperX.SqlServer.SampleApp.Infrastructure;

public sealed class SampleTenantProvider : ITenantProvider
{
    public static readonly Guid DemoTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public object GetCurrentTenantId() => DemoTenantId;
}
