namespace DapperX.SqlServer.SampleApp.Infrastructure;

/// <summary>Demonstrates <see cref="DapperX.Core.Attributes.EntityListenersAttribute"/>.</summary>
public sealed class SampleAuditListener
{
    public static int PrePersistCount { get; private set; }

    public static void Reset() => PrePersistCount = 0;

    [DapperX.Core.Attributes.PrePersist]
    public void BeforeInsert(object entity) => PrePersistCount++;
}
