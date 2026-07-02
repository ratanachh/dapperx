using System.Data;
using System.Data.Common;
using Dapper;
using Dapper.Npa.Abstractions.Auditing;
using Dapper.Npa.Abstractions.Configuration;
using Dapper.Npa.Abstractions.Tenancy;
using Dapper.Npa.IntegrationTests.Shared.Fixtures;
using Dapper.Npa.Runtime.Configuration;

namespace Dapper.Npa.IntegrationTests.Shared;

file sealed class IntegrationTenantProvider : ITenantProvider
{
    public object GetCurrentTenantId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
}

file sealed class IntegrationAuditingProvider : IAuditingProvider
{
    public string GetCurrentUser() => "integration-test";
}

public sealed class IntegrationEnvironment : IAsyncDisposable
{
    public IDbConnection Connection { get; private set; } = null!;
    public SqlExecutionCountFixture SqlCounter { get; } = new();
    public IDapperXOptions Options => SqlCounter.Options;
    public string Provider { get; private set; } = "";

    public IntegCatalogItemRepositoryImpl Catalog { get; private set; } = null!;
    public IntegArchivedItemRepositoryImpl Archived { get; private set; } = null!;
    public IntegTenantItemRepositoryImpl TenantItems { get; private set; } = null!;
    public IntegAuditedItemRepositoryImpl Audited { get; private set; } = null!;
    public IntegBulkRowRepositoryImpl Bulk { get; private set; } = null!;
    public IntegParentRepositoryImpl Parents { get; private set; } = null!;
    public IntegCompositeOrderItemRepositoryImpl CompositeItems { get; private set; } = null!;
    public IntegDocumentRepositoryImpl Documents { get; private set; } = null!;
    public IntegUserRepositoryImpl Users { get; private set; } = null!;
    public IntegGalleryProductRepositoryImpl Gallery { get; private set; } = null!;
    public IntegDepartmentRepositoryImpl Departments { get; private set; } = null!;
    public IntegGraphParentRepositoryImpl GraphParents { get; private set; } = null!;
    public IntegGraphOrderRepositoryImpl GraphOrders { get; private set; } = null!;
    public IntegFilteredCatalogItemRepositoryImpl FilteredCatalog { get; private set; } = null!;
    public IntegTransformProductRepositoryImpl TransformProducts { get; private set; } = null!;
    public IntegQueryProductRepositoryImpl QueryProducts { get; private set; } = null!;
    public IntegQueryCustomerRepositoryImpl QueryCustomers { get; private set; } = null!;
    public IntegTenantRegionUserRepositoryImpl TenantRegionUsers { get; private set; } = null!;
#if !DAPPERX_PROVIDER_SQLITE
    public IntegProcOrderRepositoryImpl ProcOrders { get; private set; } = null!;
#endif

#if DAPPERX_PROVIDER_SQLSERVER
    private Testcontainers.MsSql.MsSqlContainer? _container;
#endif
#if DAPPERX_PROVIDER_POSTGRESQL
    private Testcontainers.PostgreSql.PostgreSqlContainer? _container;
#endif
#if DAPPERX_PROVIDER_MYSQL
    private Testcontainers.MySql.MySqlContainer? _container;
#endif

    public static async Task<IntegrationEnvironment> CreateAsync()
    {
        var env = new IntegrationEnvironment();
        await env.InitializeAsync();
        return env;
    }

    private async Task InitializeAsync()
    {
#if DAPPERX_PROVIDER_SQLSERVER
        Provider = "SqlServer";
        _container = new Testcontainers.MsSql.MsSqlBuilder().Build();
        await _container.StartAsync();
        Connection = new Microsoft.Data.SqlClient.SqlConnection(_container.GetConnectionString());
#elif DAPPERX_PROVIDER_POSTGRESQL
        Provider = "PostgreSql";
        _container = new Testcontainers.PostgreSql.PostgreSqlBuilder().Build();
        await _container.StartAsync();
        Connection = new Npgsql.NpgsqlConnection(_container.GetConnectionString());
#elif DAPPERX_PROVIDER_MYSQL
        Provider = "MySql";
        _container = new Testcontainers.MySql.MySqlBuilder()
            .WithCommand("--local-infile=1")
            .Build();
        await _container.StartAsync();
        var mysqlCs = _container.GetConnectionString();
        if (!mysqlCs.Contains("AllowLoadLocalInfile", StringComparison.OrdinalIgnoreCase))
            mysqlCs += ";AllowLoadLocalInfile=true";
        Connection = new MySqlConnector.MySqlConnection(mysqlCs);
#elif DAPPERX_PROVIDER_SQLITE
        Provider = "Sqlite";
        Connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
#else
        throw new InvalidOperationException("Unknown integration test provider.");
#endif
        if (Connection is DbConnection dbConnection)
            await dbConnection.OpenAsync();
        else
            Connection.Open();

#if DAPPERX_PROVIDER_SQLITE
        SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
#endif

        await DatabaseBootstrap.CreateSchemaAsync(Connection, Provider);

        var tenantProvider = new IntegrationTenantProvider();
        var auditingProvider = new IntegrationAuditingProvider();

        Catalog = new IntegCatalogItemRepositoryImpl(Connection, Options);
        Archived = new IntegArchivedItemRepositoryImpl(Connection, Options);
        TenantItems = new IntegTenantItemRepositoryImpl(Connection, Options, tenantProvider);
        Audited = new IntegAuditedItemRepositoryImpl(Connection, Options, auditingProvider);
        Bulk = new IntegBulkRowRepositoryImpl(Connection, Options);
        Parents = new IntegParentRepositoryImpl(Connection, Options);
        CompositeItems = new IntegCompositeOrderItemRepositoryImpl(Connection, Options);
        Documents = new IntegDocumentRepositoryImpl(Connection, Options);
        Users = new IntegUserRepositoryImpl(Connection, Options);
        Gallery = new IntegGalleryProductRepositoryImpl(Connection, Options);
        Departments = new IntegDepartmentRepositoryImpl(Connection, Options);
        GraphParents = new IntegGraphParentRepositoryImpl(Connection, Options);
        GraphOrders = new IntegGraphOrderRepositoryImpl(Connection, Options);
        FilteredCatalog = new IntegFilteredCatalogItemRepositoryImpl(Connection, Options);
        TransformProducts = new IntegTransformProductRepositoryImpl(Connection, Options);
        QueryProducts = new IntegQueryProductRepositoryImpl(Connection, Options);
        QueryCustomers = new IntegQueryCustomerRepositoryImpl(Connection, Options);
        TenantRegionUsers = new IntegTenantRegionUserRepositoryImpl(Connection, Options, tenantProvider);
#if !DAPPERX_PROVIDER_SQLITE
        ProcOrders = new IntegProcOrderRepositoryImpl(Connection, Options);
#endif
    }

    public async ValueTask DisposeAsync()
    {
        if (Connection is DbConnection dbConn)
            await dbConn.DisposeAsync();
        else
            Connection.Dispose();
#if DAPPERX_PROVIDER_SQLSERVER
        if (_container is not null)
            await _container.DisposeAsync();
#elif DAPPERX_PROVIDER_POSTGRESQL
        if (_container is not null)
            await _container.DisposeAsync();
#elif DAPPERX_PROVIDER_MYSQL
        if (_container is not null)
            await _container.DisposeAsync();
#endif
    }
}
