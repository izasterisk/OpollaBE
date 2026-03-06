using BLL.Interfaces;
using BLL.Services;
using BLL.Helper;
using Infrastructure;
using DotNetEnv;
using DAL.Data;
using Microsoft.EntityFrameworkCore;

// Load .env file if exists (for local development)
try
{
    var envPath = File.Exists("OpollaBE/.env") ? "OpollaBE/.env" :
        File.Exists(".env") ? ".env" : null;
    
    if (envPath != null)
    {
        Env.Load(envPath);
        Console.WriteLine($".env file loaded from: {envPath}");
    }
    else
    {
        Console.WriteLine("No .env file found. Using environment variables from host.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not load .env file: {ex.Message}. Using environment variables from host.");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Infrastructure services
builder.Services.AddInfrastructure();

// Add DAL - DbContext with MySQL
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")!;
builder.Services.AddDbContext<OpollaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<TokenHelper>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGoogleSheetsSyncService, GoogleSheetsSyncService>();

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
// Enable Swagger in all environments (including Production for Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpollaBE API V1");
    c.RoutePrefix = "swagger"; // Access via /swagger
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Redirect root to Swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
