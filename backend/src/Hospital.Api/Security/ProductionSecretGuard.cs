namespace Hospital.Api.Security;

/// <summary>
/// Production startup check. Failure text names configuration keys only.
/// </summary>
public static class ProductionSecretGuard
{
    public static void Validate(IConfiguration configuration, bool tlsTerminatedByHost)
    {
        var problems = new List<string>();

        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
        {
            problems.Add("ConnectionStrings:DefaultConnection is missing.");
        }
        else
        {
            var password = ReadConnectionValue(connection, "Password") ?? ReadConnectionValue(connection, "Pwd");
            if (string.IsNullOrWhiteSpace(password))
            {
                problems.Add("ConnectionStrings:DefaultConnection password is missing.");
            }
            else if (IsKnownDevSecret(password))
            {
                problems.Add("ConnectionStrings:DefaultConnection password is a development placeholder.");
            }
        }

        var jwt = FirstNonEmpty(configuration["Jwt:SigningKey"], configuration["Jwt:Secret"]);
        if (string.IsNullOrWhiteSpace(jwt) || jwt.Length < 32)
        {
            problems.Add("Jwt:SigningKey is missing or shorter than 32 characters.");
        }
        else if (IsKnownDevSecret(jwt))
        {
            problems.Add("Jwt:SigningKey is a development placeholder.");
        }

        var agentSecret = FirstNonEmpty(configuration["AgentService:SharedSecret"], configuration["AGENT_SHARED_SECRET"]);
        if (string.IsNullOrWhiteSpace(agentSecret))
        {
            problems.Add("AgentService:SharedSecret is missing.");
        }
        else if (IsKnownDevSecret(agentSecret))
        {
            problems.Add("AgentService:SharedSecret is a development placeholder.");
        }

        var apiKey = FirstNonEmpty(
            configuration["InternalService:ApiKey"],
            configuration["INTERNAL_SERVICE_KEY"],
            configuration["InternalServiceKey"],
            configuration["InternalService:Key"]);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            problems.Add("InternalService:ApiKey is missing.");
        }
        else if (IsKnownDevSecret(apiKey))
        {
            problems.Add("InternalService:ApiKey is a development placeholder.");
        }

        if (!tlsTerminatedByHost)
        {
            var certificatePath = configuration["Kestrel:Certificates:Default:Path"];
            var certificatePassword = configuration["Kestrel:Certificates:Default:Password"];
            if (string.IsNullOrWhiteSpace(certificatePath))
            {
                problems.Add("Kestrel:Certificates:Default:Path is missing. Set PORT when the host terminates TLS.");
            }
            else if (!File.Exists(certificatePath))
            {
                problems.Add("Kestrel:Certificates:Default:Path does not point at a certificate file.");
            }

            if (string.IsNullOrWhiteSpace(certificatePassword))
            {
                problems.Add("Kestrel:Certificates:Default:Password is missing.");
            }
            else if (IsKnownDevSecret(certificatePassword))
            {
                problems.Add("Kestrel:Certificates:Default:Password is a development placeholder.");
            }
        }

        var origins = ReadProductionOrigins(configuration);
        if (origins.Length == 0)
        {
            problems.Add("CORS origins are missing. Set AllowedOrigins, or Cors:StaffOrigins and Cors:FlutterWebOrigins.");
        }
        else if (origins.Any(origin => !IsDeployedHttpsOrigin(origin)))
        {
            problems.Add("CORS origins must be the https staff web and Flutter web origins.");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("Production startup refused. " + string.Join(" ", problems));
        }
    }

    public static string[] ReadProductionOrigins(IConfiguration configuration)
    {
        var allowed = configuration["AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(allowed))
        {
            return allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var staff = configuration.GetSection("Cors:StaffOrigins").Get<string[]>() ?? Array.Empty<string>();
        var flutter = configuration.GetSection("Cors:FlutterWebOrigins").Get<string[]>() ?? Array.Empty<string>();
        return staff.Concat(flutter)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsDeployedHttpsOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return uri.Host is not ("localhost" or "127.0.0.1" or "0.0.0.0" or "::1");
    }

    private static bool IsKnownDevSecret(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Equals("postgres", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("change-me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return trimmed.Contains("change-me", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("dev-only", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("dev-internal", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("replace-me", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("replace-with-a-random", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadConnectionValue(string connectionString, string key)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (part[..separator].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return part[(separator + 1)..].Trim();
            }
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
