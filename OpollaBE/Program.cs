using BLL.Interfaces;
using BLL.Services.Authentication;
using BLL.Services.Classes;
using BLL.Helper;
using Infrastructure;
using DotNetEnv;

// Load .env file
var envPath = File.Exists(".env") ? ".env" : throw new FileNotFoundException(".env file not found");
Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Infrastructure services
builder.Services.AddInfrastructure();

builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<TokenHelper>();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add CORS - Allow all origins for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpollaBE API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at root (http://localhost:5000/)
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
