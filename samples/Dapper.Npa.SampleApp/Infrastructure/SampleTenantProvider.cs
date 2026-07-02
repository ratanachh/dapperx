using Dapper.Npa.Abstractions.Tenancy;

namespace Dapper.Npa.SampleApp.Infrastructure;

public sealed class SampleTenantProvider : ITenantProvider
{
    public static readonly Guid DemoTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public object GetCurrentTenantId() => DemoTenantId;
}
