using AspNetCoreRateLimit;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Ecommerce.Application.Common.Behaviors;
using Ecommerce.ProductService.GraphQL;
using Ecommerce.Infrastructure.DependencyInjection;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Docker/runtime secret files use double underscores as configuration delimiters.
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// Ensure KeyVault and other shared Config
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled", false);
var keyVaultName = builder.Configuration["KeyVault:VaultName"];
if (keyVaultEnabled && !string.IsNullOrEmpty(keyVaultName))
{
    try
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        Console.WriteLine($"✅ Azure Key Vault configured: {keyVaultName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not connect to Azure Key Vault: {ex.Message}");
    }
}

builder.Services.AddEcommerceServices(builder.Configuration);
builder.Services.AddMediatR(typeof(Ecommerce.Application.Features.Products.Commands.CreateProductCommand).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Ecommerce.Application.Features.Products.Commands.CreateProductCommand).Assembly);

builder.Services.AddControllers()
    .AddOData(options => options.Select().Filter().OrderBy().Count().SetMaxTop(100));

builder.Services.AddAuthorization();
// Add GraphQL for Products
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQuery>()
    .AddMutationType<ProductMutation>()
    .AddSubscriptionType<RealtimeSubscription>()
    .AddInMemorySubscriptions();

var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "Ecommerce.ProductService";
var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation();
    });

builder.Services.AddScoped<Ecommerce.Application.Common.Services.IUserContextService, Ecommerce.Application.Common.Services.UserContextService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce Product API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce Product API V1");
    c.RoutePrefix = "swagger";
});

// Since rate limiting, exception handling middlewares might be in Ecommerce.API
// we should ideally move them to Ecommerce.Infrastructure or just use standard ones if they are missing.
// For now, mapping endpoints:

app.MapGraphQL("/graphql").RequireAuthorization();
app.MapControllers();

app.Run();
