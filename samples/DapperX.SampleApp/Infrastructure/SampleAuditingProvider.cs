using DapperX.Abstractions.Auditing;

namespace DapperX.SampleApp.Infrastructure;

public sealed class SampleAuditingProvider : IAuditingProvider
{
    public string GetCurrentUser() => "sample-app";
}
