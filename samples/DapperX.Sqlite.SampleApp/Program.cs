using System.Data;
using Dapper;
using DapperX.Sqlite.SampleApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDapperX(builder.Configuration.GetConnectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await EnsureSchemaAsync(connection);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/students", async (IStudentRepository studentRepository) =>
{
    return await studentRepository.GetAllAsync();
})
.WithName("GetStudents");

app.Run();

static async Task EnsureSchemaAsync(IDbConnection connection)
{
    await connection.ExecuteAsync(
        """
        CREATE TABLE IF NOT EXISTS students (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          name TEXT NOT NULL
        );
        """);

    var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM students;");
    if (count == 0)
    {
        await connection.ExecuteAsync(
            """
            INSERT INTO students (name) VALUES
              ('Alice'),
              ('Bob'),
              ('Carol');
            """);
    }
}
