using DapperX.PostgreSql.SampleApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDapperX(builder.Configuration.GetConnectionString);

var app = builder.Build();

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
