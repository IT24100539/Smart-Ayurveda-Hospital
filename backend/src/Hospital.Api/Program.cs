using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Hospital.Api.Authentication;
using Hospital.Api.Hosting;
using Microsoft.AspNetCore.Authentication;
using Hospital.Api.Middleware;
using Hospital.Api.Security;
using Hospital.Application;
using Hospital.Application.Abstractions;
using Hospital.Infrastructure;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.Configure<InternalServiceOptions>(builder.Configuration.GetSection(InternalServiceOptions.SectionName));
    builder.Services.AddSingleton<InternalServiceKeyFilter>();
    builder.Services.Configure<ComplaintEscalationOptions>(
        builder.Configuration.GetSection(ComplaintEscalationOptions.SectionName));
    builder.Services.AddSingleton<ComplaintEscalationHostedService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ComplaintEscalationHostedService>());
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<HttpCurrentUser>();
    builder.Services.AddScoped<CurrentUserOverride>();
    builder.Services.AddScoped<ICurrentUser, CompositeCurrentUser>();
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<HospitalDbContext>();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is missing.");
    if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
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
        })
        .AddScheme<AuthenticationSchemeOptions, InternalServiceAuthenticationHandler>(
            InternalServiceAuthenticationHandler.SchemeName, _ => { });
    builder.Services.AddAuthorization(options =>
        options.AddPolicy(InternalServiceAuthenticationHandler.PolicyName, policy =>
            policy.AddAuthenticationSchemes(InternalServiceAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("StaffPortal", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Flutter web picks a new localhost port on every `flutter run`.
                policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    uri.Host is "localhost" or "127.0.0.1");
            }
            else
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? builder.Configuration.GetSection("Cors:StaffOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5173" };
                policy.WithOrigins(origins);
            }

            policy.AllowAnyHeader().AllowAnyMethod();
        });
    });

    var app = builder.Build();

    if (serilogEnabled)
    {
        app.UseSerilogRequestLogging();
    }
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // The local staff portal calls the HTTP development endpoint. Redirecting that
    // request to HTTPS makes browser requests fail when the ASP.NET development
    // certificate has not been trusted. Keep HTTPS enforcement for non-development
    // environments, where a trusted certificate is expected.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("StaffPortal");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        await DbSeeder.SeedAsync(db, logger);
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

public partial class Program;