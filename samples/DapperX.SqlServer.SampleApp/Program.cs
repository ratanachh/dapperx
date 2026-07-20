using System.Data;
using DapperX.Abstractions.Auditing;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Tenancy;
using DapperX.Generated;
using DapperX.Runtime.Configuration;
using DapperX.SqlServer.SampleApp;
using DapperX.SqlServer.SampleApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAuditingProvider, SampleAuditingProvider>();
builder.Services.AddSingleton<ITenantProvider, SampleTenantProvider>();
builder.Services.AddScoped<IDapperXOptions>(_ =>
{
    var options = new DapperXOptions { LogSql = true };
    var region = builder.Configuration["DapperX:DefaultRegion"] ?? "US";
    options.EnableFilter("active_region", new { region });
    return options;
});

builder.Services.AddDapperX(builder.Configuration.GetConnectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await AppDb.EnsureSchemaAsync(connection, DapperXConnectionFactory.ProviderName);
}

app.MapDemoEndpoints();

app.Run();
