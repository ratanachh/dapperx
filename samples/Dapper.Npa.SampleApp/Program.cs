using System.Data;
using System.Data.Common;
using Dapper.Npa.Abstractions.Auditing;
using Dapper.Npa.Abstractions.Configuration;
using Dapper.Npa.Abstractions.Tenancy;
using Dapper.Npa.Runtime.Configuration;
using Dapper.Npa.SampleApp;
using Dapper.Npa.SampleApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var dbHost = SampleDatabaseHost.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(dbHost);

builder.Services.AddSingleton<IAuditingProvider, SampleAuditingProvider>();
builder.Services.AddSingleton<ITenantProvider, SampleTenantProvider>();
builder.Services.AddScoped<IDapperXOptions>(_ =>
{
    var options = new DapperXOptions { LogSql = true };
    var region = builder.Configuration["DapperX:DefaultRegion"] ?? "US";
    options.EnableFilter("active_region", new { region });
    return options;
});

builder.Services.AddDapperXRepositories(_ => CreateOpenConnection(dbHost));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await AppDb.EnsureSchemaAsync(connection, dbHost.Provider);
}

app.MapDemoEndpoints(dbHost);

app.Run();

static IDbConnection CreateOpenConnection(SampleDatabaseHost dbHost)
{
    var connection = dbHost.CreateConnection();
    if (connection is DbConnection db)
        db.Open();
    else
        connection.Open();
    return connection;
}
