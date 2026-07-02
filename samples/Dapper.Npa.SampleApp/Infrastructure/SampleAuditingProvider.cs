using Dapper.Npa.Abstractions.Auditing;

namespace Dapper.Npa.SampleApp.Infrastructure;

public sealed class SampleAuditingProvider : IAuditingProvider
{
    public string GetCurrentUser() => "sample-app";
}
