using DapperX.MySql.SampleApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDapperX(builder.Configuration.GetConnectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
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

