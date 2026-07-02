namespace Dapper.Npa.Abstractions.Auditing;

public interface IAuditingProvider
{
    string GetCurrentUser();
}
