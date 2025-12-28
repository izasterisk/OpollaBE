using BLL.Interfaces;
using BLL.Services.Authentication;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Infrastructure services
builder.Services.AddInfrastructure();

builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
