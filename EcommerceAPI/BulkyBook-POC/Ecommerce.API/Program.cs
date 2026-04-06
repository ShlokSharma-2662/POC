using AspNetCoreRateLimit;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Ecommerce.API.Middleware;
using Ecommerce.Infrastructure.DependencyInjection;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault if enabled
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled", false);
var keyVaultName = builder.Configuration["KeyVault:VaultName"];

if (keyVaultEnabled && !string.IsNullOrEmpty(keyVaultName))
{
    try
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        
        // Use DefaultAzureCredential which supports:
        // - Managed Identity (for App Service/VM)
        // - Azure CLI (for local development)
        // - Visual Studio credentials
        // - Environment variables
        var credential = new DefaultAzureCredential();
        
        builder.Configuration.AddAzureKeyVault(keyVaultUri, credential, new Ecommerce.API.KeyVaultSecretManager());
        
        Console.WriteLine($"✅ Azure Key Vault configured: {keyVaultName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not connect to Azure Key Vault: {ex.Message}");
        Console.WriteLine("   Application will continue using other configuration sources.");
    }
}

builder.Services.AddEcommerceServices(builder.Configuration);
builder.Services.AddMediatR(Assembly.Load("Ecommerce.Application"));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
// ✅ Allow all CORS (for dev/testing)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   // Allows any frontend domain
            .AllowAnyHeader()   // Allows all headers (Auth, Content-Type, etc.)
            .AllowAnyMethod();  // Allows all HTTP methods (GET, POST, PUT, DELETE, etc.)
    });
});
builder.Services.AddScoped<Ecommerce.Application.Common.Services.IUserContextService, Ecommerce.Application.Common.Services.UserContextService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ecommerce API",
        Version = "v1",
        Description = "Ecommerce API with standardized response format"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.CustomSchemaIds(t => t.FullName?.Replace("+", ".") ?? t.Name);

    try
    {
        foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly))
            c.IncludeXmlComments(xml, includeControllerXmlComments: true);
    }
    catch { }
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database initialized successfully");

        var metricsCount = await context.SystemMetrics.CountAsync();
        Log.Information("SystemMetrics table contains {Count} records", metricsCount);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed. This may be due to connection issues, insufficient permissions, or database server unavailability. Please check your database configuration and try again.");
    }
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API V1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Ecommerce API Documentation";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
    c.ShowExtensions();
});

// Middleware order matters:
// 1. RateLimitingMiddleware - should run early to block excessive requests before processing
// 2. ErrorLoggingMiddleware - catches exceptions, logs them (fire-and-forget for DB), then rethrows
// 3. GlobalExceptionHandlerMiddleware - catches exceptions and converts to ApiResponse format
// 4. Other middleware

// Rate limiting should be early in the pipeline to block excessive requests
app.UseMiddleware<RateLimitingMiddleware>();

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Other middleware
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<RequestTimeoutMiddleware>();

app.MapControllers();

app.Run();
