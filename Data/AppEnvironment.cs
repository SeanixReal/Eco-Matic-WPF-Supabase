using System.IO;

namespace Eco_Matic.Data;

public sealed class AppConfigurationException : InvalidOperationException
{
    public AppConfigurationException(string message) : base(message)
    {
    }
}

public static class AppEnvironment
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public static string? LoadedDotEnvPath { get; private set; }

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            string? dotEnvPath = FindDotEnvPath(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(dotEnvPath))
            {
                throw new AppConfigurationException(
                    "No .env file was found.\n\nCopy .env.example to .env in the project root, then fill in the required Supabase and local MySQL values.");
            }

            LoadDotEnvFile(dotEnvPath);
            ValidateRequiredSettings();
            LoadedDotEnvPath = dotEnvPath;
            _initialized = true;
        }
    }

    public static string GetRequired(string key)
    {
        Initialize();
        return GetRequiredCore(key);
    }

    public static string? GetOptional(string key)
    {
        Initialize();
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static string GetRequiredSupabaseApiKey()
    {
        Initialize();
        return GetRequiredSupabaseApiKeyCore();
    }

    private static string GetRequiredSupabaseApiKeyCore()
    {
        string? publishableKey = Environment.GetEnvironmentVariable("ECOMATIC_SUPABASE_PUBLISHABLE_KEY")?.Trim();
        if (!string.IsNullOrWhiteSpace(publishableKey))
        {
            if (IsPlaceholderValue("ECOMATIC_SUPABASE_PUBLISHABLE_KEY", publishableKey))
            {
                throw new AppConfigurationException(
                    "Required setting 'ECOMATIC_SUPABASE_PUBLISHABLE_KEY' still contains the placeholder example value. Update your .env file with a real value.");
            }

            return publishableKey;
        }

        return GetRequiredCore("ECOMATIC_SUPABASE_ANON_KEY");
    }

    private static string GetRequiredCore(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AppConfigurationException($"Required setting '{key}' is missing.");
        }

        if (IsPlaceholderValue(key, value))
        {
            throw new AppConfigurationException(
                $"Required setting '{key}' still contains the placeholder example value. Update your .env file with a real value.");
        }

        return value;
    }

    public static uint GetRequiredUInt(string key)
    {
        string value = GetRequired(key);
        if (!uint.TryParse(value, out uint parsedValue) || parsedValue == 0)
        {
            throw new AppConfigurationException(
                $"Required setting '{key}' must be a valid positive integer.");
        }

        return parsedValue;
    }

    private static void ValidateRequiredSettings()
    {
        string supabaseUrl = GetRequiredCore("ECOMATIC_SUPABASE_URL");
        if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out Uri? parsedSupabaseUrl) ||
            parsedSupabaseUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new AppConfigurationException(
                "ECOMATIC_SUPABASE_URL must be a valid absolute HTTPS URL.");
        }

        string? publishableKey = Environment.GetEnvironmentVariable("ECOMATIC_SUPABASE_PUBLISHABLE_KEY")?.Trim();
        string? anonKey = Environment.GetEnvironmentVariable("ECOMATIC_SUPABASE_ANON_KEY")?.Trim();

        if (string.IsNullOrWhiteSpace(publishableKey) && string.IsNullOrWhiteSpace(anonKey))
        {
            throw new AppConfigurationException(
                "You must set either ECOMATIC_SUPABASE_PUBLISHABLE_KEY or ECOMATIC_SUPABASE_ANON_KEY in .env.");
        }

        _ = GetRequiredSupabaseApiKeyCore();

        string? mysqlHost = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_HOST")?.Trim();
        string? mysqlPort = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_PORT")?.Trim();
        string? mysqlUser = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_USER")?.Trim();
        string? mysqlPassword = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_PASSWORD")?.Trim();
        string? mysqlSchema = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_SCHEMA")?.Trim();

        bool anyMySqlSettingProvided =
            !string.IsNullOrWhiteSpace(mysqlHost) ||
            !string.IsNullOrWhiteSpace(mysqlPort) ||
            !string.IsNullOrWhiteSpace(mysqlUser) ||
            !string.IsNullOrWhiteSpace(mysqlPassword) ||
            !string.IsNullOrWhiteSpace(mysqlSchema);

        if (!anyMySqlSettingProvided)
        {
            return;
        }

        _ = GetRequiredCore("ECOMATIC_LOCAL_MYSQL_HOST");
        _ = GetRequiredCore("ECOMATIC_LOCAL_MYSQL_USER");
        _ = GetRequiredCore("ECOMATIC_LOCAL_MYSQL_PASSWORD");
        _ = GetRequiredCore("ECOMATIC_LOCAL_MYSQL_SCHEMA");

        string mysqlPortValue = GetRequiredCore("ECOMATIC_LOCAL_MYSQL_PORT");
        if (!uint.TryParse(mysqlPortValue, out uint parsedPort) || parsedPort == 0)
        {
            throw new AppConfigurationException(
                "ECOMATIC_LOCAL_MYSQL_PORT must be a valid positive integer.");
        }
    }

    private static string? FindDotEnvPath(string startDirectory)
    {
        DirectoryInfo? current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static void LoadDotEnvFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        for (int index = 0; index < lines.Length; index++)
        {
            string rawLine = lines[index];
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new AppConfigurationException(
                    $".env contains an invalid line {index + 1}: '{rawLine}'. Expected KEY=value format.");
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new AppConfigurationException(
                    $".env contains an invalid key on line {index + 1}.");
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }
        }
    }

    private static bool IsPlaceholderValue(string key, string value)
    {
        return (key, value) switch
        {
            ("ECOMATIC_SUPABASE_URL", "https://your-project-ref.supabase.co") => true,
            ("ECOMATIC_SUPABASE_PUBLISHABLE_KEY", "your_supabase_publishable_key_here") => true,
            ("ECOMATIC_SUPABASE_ANON_KEY", "your_supabase_anon_key_here") => true,
            ("ECOMATIC_LOCAL_MYSQL_PASSWORD", "your_mysql_password_here") => true,
            _ => false
        };
    }
}
