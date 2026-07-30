using AspNetCoreRateLimit;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Messaging.Consumers;
using Ecommerce.Infrastructure.Payments;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using StackExchange.Redis;

namespace Ecommerce.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Infrastructure Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<ISecureCheckoutPaymentService, SecureCheckoutPaymentService>();
        
        // Resilience Policy Service
        services.AddSingleton<IResiliencePolicyService, ResiliencePolicyService>();
        
        // Email Failure Tracker
        services.AddSingleton<IEmailFailureTracker, EmailFailureTracker>();
        
        // Email Service with configuration
        services.AddScoped<IEmailService>(provider => 
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var context = provider.GetRequiredService<AppDbContext>();
            var logger = provider.GetRequiredService<ILogger<EmailService>>();
            var resiliencePolicyService = provider.GetRequiredService<IResiliencePolicyService>();
            var failureTracker = provider.GetRequiredService<IEmailFailureTracker>();
            return new EmailService(config, context, logger, resiliencePolicyService, failureTracker);
        });

        // OAuth Service with resilience policies
        services.AddHttpClient<IOAuthService, OAuthService>((provider, client) =>
        {
            // Timeout is set in OAuthService constructor, but we can also set it here
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler((provider, request) =>
        {
            var resilienceService = provider.GetRequiredService<IResiliencePolicyService>();
            var retryPolicy = resilienceService.GetHttpRetryPolicy();
            var circuitBreakerPolicy = resilienceService.GetHttpCircuitBreakerPolicy();
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        });

        // Google OAuth Service with resilience policies
        services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>((provider, client) =>
        {
            // Timeout is set in GoogleOAuthService constructor, but we can also set it here
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler((provider, request) =>
        {
            var resilienceService = provider.GetRequiredService<IResiliencePolicyService>();
            var retryPolicy = resilienceService.GetHttpRetryPolicy();
            var circuitBreakerPolicy = resilienceService.GetHttpCircuitBreakerPolicy();
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        });
        
        // Rate Limiting Service
        services.AddMemoryCache();
        services.AddSingleton<IRateLimitingService, RateLimitingService>();

        // Message Publisher Service - only register if RabbitMQ is enabled
        var rabbitMqEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        if (rabbitMqEnabled)
        {
            services.AddScoped<IMessagePublisherService, MessagePublisherService>();
        }
        else
        {
            // Register a no-op implementation when RabbitMQ is disabled
            services.AddScoped<IMessagePublisherService, NoOpMessagePublisherService>();
        }

        // Event Grid Publisher Service
        services.AddHttpClient<IEventGridPublisherService, EventGridPublisherService>();
        services.AddScoped<IProductStockUpdateNotifier, NoOpProductStockUpdateNotifier>();

        return services;
    }

    public static IServiceCollection AddRedisServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Redis connection
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? 
                                   configuration["Redis:ConnectionString"];
        var redisPassword = configuration["Redis:Password"];
        var isCacheEnabled = configuration["Redis:IsCacheEnabled"] ?? "false";
        
        if (!string.IsNullOrEmpty(redisConnectionString) && isCacheEnabled == "true")
        {
            // Build connection string with authentication if password is provided
            var connectionString = redisConnectionString;
            if (!string.IsNullOrEmpty(redisPassword))
            {
                connectionString = $"{redisConnectionString},password={redisPassword}";
            }

            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(connectionString);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the application startup
                    var logger = provider.GetService<ILogger<object>>();
                    logger?.LogError(ex, "Failed to connect to Redis server. This may be due to network issues, incorrect connection string, or Redis server unavailability. Caching will be disabled.");
                    return null;
                }
            });

            // Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
            });

            // Cache services
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        }
        else
        {
            // Register in-memory distributed cache when Redis is disabled
            services.AddDistributedMemoryCache();
            
            // Register null implementations when Redis is disabled or not configured
            services.AddScoped<ICacheService, NullCacheService>();
            services.AddScoped<ICacheInvalidationService, NullCacheInvalidationService>();
        }

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add HttpContextAccessor for UserContextService
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Controllers - JSON options configured in Program.cs
        services.AddControllers();

        // API Explorer and OpenAPI
        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "JWT";
            options.DefaultChallengeScheme = "JWT";
            options.DefaultSignInScheme = "Cookie";
        })
        .AddCookie("Cookie", options =>
        {
            options.LoginPath = "/api/oauth/login";
            options.LogoutPath = "/api/oauth/logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
        })
        .AddJwtBearer("JWT", options =>
        {
            var jwtSecretKey = configuration["Jwt:SecretKey"] 
                ?? throw new InvalidOperationException(
                    "JWT SecretKey is not configured. Please set it in User Secrets (development) or Azure Key Vault (production). " +
                    "See SECRETS_MANAGEMENT_GUIDE.md for instructions.");
            
            var jwtIssuer = configuration["Jwt:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            
            var jwtAudience = configuration["Jwt:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience is not configured.");

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                // Core validation settings
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                
                // Enhanced security settings
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(1), // Reduced from default 5 minutes for better security
                
                // Issuer and audience validation
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                
                // Signing key
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
                
                // Role claim mapping - ensure roles are properly extracted from tokens
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };
            
            // Event handlers for better debugging and security
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices
                        .GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("JwtBearer");
                    logger?.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Additional validation can be added here if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        
        // Add Authorization policies
        services.AddAuthorization(options =>
        {
            // Admin-only policy - requires Admin role
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // Admin or User policy - allows both Admin and User roles
            options.AddPolicy("AdminOrUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "User");
            });

            // User-only policy - requires User role (not Admin)
            options.AddPolicy("UserOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("User");
            });

            // Note: We don't set a FallbackPolicy to allow public endpoints
            // Endpoints must explicitly use [Authorize] or [AllowAnonymous] attributes
        });
        
        // Temporarily disable OAuth to fix Swagger issue
        // .AddOpenIdConnect("OpenIdConnect", options =>
        // {
        //     var authority = configuration["OAuth:Authority"];
        //     var clientId = configuration["OAuth:ClientId"];
        //     var clientSecret = configuration["OAuth:ClientSecret"];

        //     // Only configure OAuth if valid values are provided
        //     if (!string.IsNullOrEmpty(authority) && 
        //         !string.IsNullOrEmpty(clientId) && 
        //         !string.IsNullOrEmpty(clientSecret) &&
        //         !authority.Contains("your-tenant-id"))
        //     {
        //         options.Authority = authority;
        //         options.ClientId = clientId;
        //         options.ClientSecret = clientSecret;
        //         options.ResponseType = "code";
        //         options.Scope.Add("openid");
        //         options.Scope.Add("profile");
        //         options.Scope.Add("email");
                
        //         options.CallbackPath = configuration["OAuth:CallbackPath"] ?? "/signin-oidc";
        //         options.SignedOutCallbackPath = "/signout-callback-oidc";
                
        //         options.SaveTokens = true;
        //         options.GetClaimsFromUserInfoEndpoint = true;
        //     }
        //     else
        //     {
        //         // Disable OAuth if configuration is invalid by setting dummy values
        //         options.Authority = "https://login.microsoftonline.com/dummy-tenant/v2.0";
        //         options.ClientId = "dummy-client-id";
        //         options.ClientSecret = "dummy-secret";
        //         options.RequireHttpsMetadata = false;
        //         options.SkipUnrecognizedRequests = true;
        //     }
        // });

        return services;
    }

    public static IServiceCollection AddCorsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configuredOrigins = new List<string>();
        AddOrigins(configuredOrigins, configuration["Cors:AllowedOrigins"]);
        AddOrigins(configuredOrigins, configuration["Frontend:BaseUrl"]);

        var allowedOriginsSection = configuration.GetSection("Cors:AllowedOrigins");
        foreach (var child in allowedOriginsSection.GetChildren())
        {
            AddOrigins(configuredOrigins, child.Value);
        }

        // Keep both localhost variants available for the existing Angular app and
        // the parallel React migration unless a deployment explicitly adds more.
        configuredOrigins.Add("http://localhost:4200");
        configuredOrigins.Add("https://localhost:4200");

        var allowedOrigins = configuredOrigins
            .Select(NormalizeOrigin)
            .Where(origin => origin is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders(
                          "Retry-After",
                          "X-RateLimit-Limit",
                          "X-RateLimit-Remaining",
                          "X-RateLimit-Reset",
                          "X-RateLimit-Reset-At");
            });
        });

        return services;
    }

    private static void AddOrigins(ICollection<string> origins, string? configuredOrigins)
    {
        if (string.IsNullOrWhiteSpace(configuredOrigins))
        {
            return;
        }

        foreach (var origin in configuredOrigins.Split(
                     ',',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            origins.Add(origin);
        }
    }

    private static string? NormalizeOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }



    public static IServiceCollection AddLoggingServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Setup Serilog
        var logger = new Serilog.LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger, dispose: true));

        return services;
    }

    public static IServiceCollection AddThirdPartyServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Stripe configuration
        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
        }
        // Note: If Stripe key is missing, it will fail when Stripe is actually used
        // This is acceptable as Stripe might not be needed in all environments

        // Application Insights
        services.AddApplicationInsightsTelemetry();

        return services;
    }

    public static IServiceCollection AddRateLimitingServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var enableRateLimiting = configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true);
        
        if (enableRateLimiting)
        {
            // Configure AspNetCoreRateLimit
            services.Configure<IpRateLimitOptions>(configuration.GetSection("RateLimiting:IpRateLimiting"));
            services.Configure<ClientRateLimitOptions>(configuration.GetSection("RateLimiting:ClientRateLimiting"));
            
            // Add rate limiting services
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        }

        return services;
    }

    /// <summary>
    /// Registers MassTransit with RabbitMQ for message queuing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var rabbitMqEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        
        if (rabbitMqEnabled)
        {
            services.AddMassTransit(x =>
            {
                // Register all consumers
                x.AddConsumer<EmailConsumer>();
                x.AddConsumer<UserRegisteredConsumer>();
                x.AddConsumer<PasswordResetConsumer>();
                // CheckoutOrderHandler sends the detailed SendGrid template directly.
                // Registering OrderCreatedConsumer here would send a second confirmation.
                x.AddConsumer<OrderStatusChangedConsumer>();
                x.AddConsumer<PaymentProcessedConsumer>();
                x.AddConsumer<AdminNotificationConsumer>();
                x.AddConsumer<SystemMetricsAlertConsumer>();
                x.AddConsumer<UserActivityConsumer>();

                // Configure RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
                    var rabbitMqPort = configuration.GetValue<int>("RabbitMQ:Port", 5672);
                    var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "guest";
                    var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";
                    var rabbitMqVirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";

                    var connectionString = $"amqp://{rabbitMqUsername}:{rabbitMqPassword}@{rabbitMqHost}:{rabbitMqPort}{rabbitMqVirtualHost}";
                    cfg.Host(connectionString);

                    // Configure message routing
                    cfg.ConfigureEndpoints(context);
                });
            });

            // MassTransit hosted service is automatically registered in newer versions
        }

        return services;
    }

    /// <summary>
    /// Registers all infrastructure and common services for the Ecommerce API
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEcommerceServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        return services
            .AddInfrastructureServices(configuration)
            .AddRedisServices(configuration)
            .AddMessagingServices(configuration)
            .AddApplicationServices()
            .AddApiServices()
            .AddAuthenticationServices(configuration)
            .AddCorsServices(configuration)
            .AddRateLimitingServices(configuration)
            .AddLoggingServices(configuration)
            .AddThirdPartyServices(configuration);
    }
}
