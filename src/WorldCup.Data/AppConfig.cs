using System.Text.Json;

namespace WorldCup.Data;

/// <summary>
/// Reads optional local configuration (e.g. the football-data.org API key) from
/// <c>config.local.json</c>. This file is git-ignored and not copied into the build output, so the
/// secret stays local. Environment variables take precedence over the file.
/// </summary>
public static class AppConfig
{
    /// <summary>The API key from <c>config.local.json</c> (<c>footballDataApiKey</c>), or null.</summary>
    public static string? FootballDataApiKeyFromFile()
    {
        string? path = DataPaths.FindConfigFile();
        if (path is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("footballDataApiKey", out var key) &&
                key.ValueKind == JsonValueKind.String)
            {
                string? value = key.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }
        catch
        {
            // Malformed config is non-fatal — fall back to no key.
        }

        return null;
    }
}
