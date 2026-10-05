using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Hospital.Api.Authentication;
using Hospital.Api.Hosting;
using Microsoft.AspNetCore.Authentication;
using Hospital.Api.Middleware;
using Hospital.Api.Security;
using Hospital.Application;
using Hospital.Application.Auth;
using Hospital.Application.Abstractions;
using Hospital.Infrastructure;
using Hospital.Infrastructure.Doctors;
using Hospital.Infrastructure.Documents;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

static string? FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

var builder = WebApplication.CreateBuilder(args);
var serilogEnabled = !builder.Environment.IsEnvironment("Testing");

if (serilogEnabled)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
}

try
{
    if (serilogEnabled)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console());
    }

    // Secrets come from environment variables or Development user-secrets, not appsettings.json.
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (!string.IsNullOrWhiteSpace(jwtSecret))
    {
        builder.Configuration["Jwt:SigningKey"] = jwtSecret;
    }

    var agentSharedSecret = builder.Configuration["AGENT_SHARED_SECRET"];
    if (!string.IsNullOrWhiteSpace(agentSharedSecret))
    {
        builder.Configuration["AgentService:SharedSecret"] = agentSharedSecret;
    }

    // One name for the agent and the API. INTERNAL_SERVICE_KEY wins; InternalServiceKey
    // remains so an existing host keeps working. Both readers then see the same value.
    // An empty value is left empty so the filter and the handler fail closed.
    var internalServiceKey = FirstNonEmpty(
        builder.Configuration["INTERNAL_SERVICE_KEY"],
        builder.Configuration["InternalServiceKey"]);
    if (!string.IsNullOrWhiteSpace(internalServiceKey))
    {
        builder.Configuration["InternalService:ApiKey"] = internalServiceKey;
        builder.Configuration["InternalService:Key"] = internalServiceKey;
    }

    var tlsTerminatedByHost = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
    if (builder.Environment.IsEnvironment("Testing")
        && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SigningKey"]))
    {
        builder.Configuration["Jwt:SigningKey"] = "integration-test-signing-key-32chars!!";
    }

    if (builder.Environment.IsProduction())
    {
        ProductionSecretGuard.Validate(builder.Configuration, tlsTerminatedByHost);
    }
    else if (string.IsNullOrWhiteSpace(builder.Configuration["AgentService:SharedSecret"]))
    {
        Log.Warning("AgentService:SharedSecret is empty. Calls to the agent will return 401 until it is set in user-secrets or AGENT_SHARED_SECRET.");
    }

    builder.Services.AddApplication();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.Configure<RequestLimitsOptions>(builder.Configuration.GetSection(RequestLimitsOptions.SectionName));
    builder.WebHost.ConfigureKestrel(options =>
    {
        var jsonLimit = builder.Configuration.GetValue<long?>("RequestLimits:MaxJsonBodyBytes") ?? 1_048_576;
        if (jsonLimit < 256)
        {
            jsonLimit = 256;
        }

        var documentMax = MedicalDocumentOptions.NormalizeMaxBytes(
            builder.Configuration.GetValue<int?>("MedicalDocuments:MaxBytes") ?? MedicalDocumentOptions.DefaultMaxBytes);
        var uploadLimit = (long)documentMax + MedicalDocumentOptions.MultipartOverheadBytes;
        options.Limits.MaxRequestBodySize = Math.Max(jsonLimit, uploadLimit);
    });
    builder.Services.AddSingleton(BindOptions<PasswordPolicyOptions>(builder.Configuration, PasswordPolicyOptions.SectionName));
    builder.Services.AddSingleton(BindOptions<AccountLockoutOptions>(builder.Configuration, AccountLockoutOptions.SectionName));
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.PostConfigure<DoctorPhotoOptions>(options =>
    {
        var webRoot = string.IsNullOrWhiteSpace(builder.Environment.WebRootPath)
            ? Path.Combine(builder.Environment.ContentRootPath, "wwwroot")
            : builder.Environment.WebRootPath;
        DoctorPhotoPaths.Apply(options, builder.Environment.ContentRootPath, webRoot);
    });
    builder.Services.PostConfigure<MedicalDocumentOptions>(options =>
    {
        var webRoot = string.IsNullOrWhiteSpace(builder.Environment.WebRootPath)
            ? Path.Combine(builder.Environment.ContentRootPath, "wwwroot")
            : builder.Environment.WebRootPath;
        MedicalDocumentPaths.Apply(options, builder.Environment.ContentRootPath, webRoot);
    });
    builder.Services.Configure<InternalServiceOptions>(builder.Configuration.GetSection(InternalServiceOptions.SectionName));
    builder.Services.AddSingleton<InternalServiceKeyFilter>();
    builder.Services.Configure<ComplaintEscalationOptions>(
        builder.Configuration.GetSection(ComplaintEscalationOptions.SectionName));
    builder.Services.AddSingleton<ComplaintEscalationHostedService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ComplaintEscalationHostedService>());
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IClientAddress, HttpClientAddress>();
    builder.Services.AddScoped<HttpCurrentUser>();
    builder.Services.AddScoped<CurrentUserOverride>();
    builder.Services.AddScoped<ICurrentUser, CompositeCurrentUser>();
    builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var entry in context.ModelState)
            {
                var messages = entry.Value?.Errors
                    .Select(error => ProblemDetailSanitizer.PublicMessage(
                        string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage,
                        "The value is invalid."))
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray() ?? Array.Empty<string>();
                if (messages.Length == 0)
                {
                    continue;
                }

                var key = string.IsNullOrEmpty(entry.Key) ? "_error" : entry.Key;
                errors[key] = messages;
            }

            var problem = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The request is invalid."
            };
            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    }).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<HospitalDbContext>();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is missing.");
    if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be at least 32 characters. Set Jwt__SigningKey or dotnet user-secrets \"Jwt:SigningKey\".");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<HospitalDbContext>();
                    var sub = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? context.Principal?.FindFirst("UserId")?.Value;

                    if (!Guid.TryParse(sub, out var userId))
                    {
                        return;
                    }

                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    if (user is not null)
                    {
                        if (!user.IsActive)
                        {
                            context.Fail("User account has been deactivated.");
                            return;
                        }

                        var tokenVersionClaim = context.Principal?.FindFirst("token_version")?.Value;
                        if (!string.IsNullOrEmpty(tokenVersionClaim) && int.TryParse(tokenVersionClaim, out var tokenVersion))
                        {
                            if (tokenVersion != user.TokenVersion)
                            {
                                context.Fail("Token has been revoked.");
                                return;
                            }
                        }
                    }
                }
            };
        })
        .AddScheme<AuthenticationSchemeOptions, InternalServiceAuthenticationHandler>(
            InternalServiceAuthenticationHandler.SchemeName, _ => { });
    builder.Services.AddAuthorization(options =>
        options.AddPolicy(InternalServiceAuthenticationHandler.PolicyName, policy =>
            policy.AddAuthenticationSchemes(InternalServiceAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()));

    var authRateLimit = BindOptions<AuthRateLimitOptions>(builder.Configuration, AuthRateLimitOptions.SectionName);
    if (builder.Environment.IsEnvironment("Testing"))
    {
        authRateLimit.PermitLimit = Math.Max(authRateLimit.PermitLimit, 10_000);
    }

    var authPermitLimit = Math.Clamp(authRateLimit.PermitLimit, 1, 100_000);
    var authWindow = TimeSpan.FromSeconds(Math.Clamp(authRateLimit.WindowSeconds, 1, 3_600));
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            if (context.HttpContext.Response.HasStarted)
            {
                return;
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = StatusCodes.Status429TooManyRequests,
                detail = AuthMessages.TooManyAttempts
            }, cancellationToken);
        };
        options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = httpContext.Request.Path.Value ?? string.Empty;
            return RateLimitPartition.GetFixedWindowLimiter(
                $"{ip}|{path}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermitLimit,
                    Window = authWindow,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    });

    if (builder.Environment.IsProduction())
    {
        builder.Services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("StaffPortal", policy =>
        {
            if (builder.Environment.IsProduction())
            {
                policy.WithOrigins(ProductionSecretGuard.ReadProductionOrigins(builder.Configuration));
            }
            else if (builder.Environment.IsDevelopment())
            {
                // Flutter web, mobile browsers, and local dev pick localhost ports or private LAN IPs.
                policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    (uri.Host is "localhost" or "127.0.0.1" or "10.0.2.2" ||
                     uri.Host.StartsWith("192.168.") ||
                     uri.Host.StartsWith("10.") ||
                     uri.Host.StartsWith("172.")));
            }
            else
            {
                policy.AllowAnyOrigin();
            }

            policy.AllowAnyHeader().AllowAnyMethod();
        });
    });

    var app = builder.Build();
    _ = app.Services.GetRequiredService<IOptions<DoctorPhotoOptions>>().Value;
    _ = app.Services.GetRequiredService<IOptions<MedicalDocumentOptions>>().Value;

    if (serilogEnabled)
    {
        app.UseSerilogRequestLogging();
    }
    if (app.Environment.IsProduction())
    {
        app.UseForwardedHeaders();
        app.UseHsts();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestSizeLimitMiddleware>();
    app.UseMiddleware<EmptyErrorBodyMiddleware>();

    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        if (app.Environment.IsProduction())
        {
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }
        }

        await next();
    });

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (app.Environment.IsProduction() && !tlsTerminatedByHost)
    {
        app.UseHttpsRedirection();
    }
    app.UseRouting();
    app.UseCors("StaffPortal");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        await DbSeeder.SeedAsync(db, logger, app.Environment.IsDevelopment());
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    if (serilogEnabled)
    {
        Log.Fatal(ex, "Hospital API terminated unexpectedly");
    }

    throw;
}
finally
{
    if (serilogEnabled)
    {
        await Log.CloseAndFlushAsync();
    }
}

static T BindOptions<T>(IConfiguration configuration, string section) where T : class, new() =>
    configuration.GetSection(section).Get<T>() ?? new T();

public partial class Program;