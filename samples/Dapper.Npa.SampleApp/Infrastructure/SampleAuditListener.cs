namespace Dapper.Npa.SampleApp.Infrastructure;

/// <summary>Demonstrates <see cref="Dapper.Npa.Core.Attributes.EntityListenersAttribute"/>.</summary>
public sealed class SampleAuditListener
{
    public static int PrePersistCount { get; private set; }

    public static void Reset() => PrePersistCount = 0;

    [Dapper.Npa.Core.Attributes.PrePersist]
    public void BeforeInsert(object entity) => PrePersistCount++;
}
