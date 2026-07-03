using DapperX.Core.Configuration;
using DapperX.Core.Enums;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace DapperX.Tests;

public class CompileTimeDatabaseProviderTests
{
    private static MetadataReference[] CoreReferences =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(DatabaseProvider).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(DapperXDatabaseProviderAttribute).Assembly.Location),
    ];

    [Fact]
    public void Resolve_defaults_to_SqlServer_when_unconfigured()
    {
        var compilation = CSharpCompilation.Create("Empty", references: CoreReferences);
        Assert.Equal("SqlServer", CompileTimeDatabaseProvider.Resolve(compilation));
    }

    [Fact]
    public void Resolve_honors_msbuild_property()
    {
        var compilation = CSharpCompilation.Create("Empty", references: CoreReferences);
        Assert.Equal("PostgreSql", CompileTimeDatabaseProvider.Resolve(compilation, "PostgreSql"));
        Assert.Equal("Sqlite", CompileTimeDatabaseProvider.Resolve(compilation, "SQLite"));
    }

    [Fact]
    public void MsBuild_property_overrides_default()
    {
        var compilation = CSharpCompilation.Create("Empty", references: CoreReferences);
        Assert.Equal("SqlServer", CompileTimeDatabaseProvider.Resolve(compilation, "SqlServer"));
        Assert.Equal("Sqlite", CompileTimeDatabaseProvider.Resolve(compilation, "Sqlite"));
    }
}
