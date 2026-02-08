using System.Text.Json;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Discovers and loads user-authored practice configurations.
///
/// Configs live in a dedicated directory (UserConfigs/) alongside
/// user snippets. Each JSON file defines one PracticeConfig.
/// The filename becomes the config's display name.
///
/// Loading follows the same pattern as UserContentService:
/// - If the directory doesn't exist, this service does nothing.
/// - Malformed files are logged and skipped — never crash.
/// - Configs are validated against ExtensionBoundary limits.
/// - A built-in "Default" config is always available.
/// </summary>
public sealed class PracticeConfigService
{
    private readonly List<PracticeConfig> _configs = new();
    private readonly List<string> _loadErrors = new();
    private bool _initialized;

    /// <summary>
    /// The directory where user-authored configs live.
    /// Null if no config directory exists.
    /// </summary>
    public string? UserConfigsPath { get; private set; }

    /// <summary>
    /// All available configs (built-in Default + user-authored).
    /// The Default config is always first.
    /// </summary>
    public IReadOnlyList<PracticeConfig> Configs => _configs;

    /// <summary>
    /// Whether any user configs were found and loaded.
    /// </summary>
    public bool HasUserConfigs => _configs.Any(c => c.IsUserAuthored);

    /// <summary>
    /// Number of user config files successfully loaded.
    /// </summary>
    public int LoadedConfigCount => _configs.Count(c => c.IsUserAuthored);

    /// <summary>
    /// Errors encountered during loading.
    /// </summary>
    public IReadOnlyList<string> LoadErrors => _loadErrors;

    /// <summary>
    /// Discovers and loads configs. Always includes the built-in Default.
    /// Safe to call multiple times — only initializes once.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Always start with the built-in default
        _configs.Add(PracticeConfig.Default);

        // User configs live in %LOCALAPPDATA%, never in the app's install directory.
        // This isolation ensures user configs can never corrupt built-in assets.
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var configDir = Path.Combine(appDataDir, ExtensionBoundary.UserConfigsDir);

        // Safety: reject if the path somehow resolves inside the app directory
        var appBase = AppContext.BaseDirectory;
        if (configDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase))
        {
            _loadErrors.Add("User configs directory must not be inside the app directory");
            return;
        }

        if (!Directory.Exists(configDir))
        {
            UserConfigsPath = null;
            return;
        }

        UserConfigsPath = configDir;

        var files = Directory.GetFiles(configDir, $"*{ExtensionBoundary.ConfigFileExtension}");
        foreach (var file in files.Take(ExtensionBoundary.MaxUserConfigs))
        {
            LoadConfigFile(file);
        }
    }

    /// <summary>
    /// Gets a config by name (case-insensitive).
    /// Returns the Default config if not found.
    /// </summary>
    public PracticeConfig GetConfig(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return PracticeConfig.Default;

        return _configs.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? PracticeConfig.Default;
    }

    /// <summary>
    /// Gets all config names for UI display.
    /// </summary>
    public IReadOnlyList<string> GetConfigNames()
    {
        return _configs.Select(c => c.Name).ToList();
    }

    /// <summary>
    /// Returns the path where users should place config files.
    /// Creates the directory if it doesn't exist yet.
    /// </summary>
    public string EnsureUserConfigsDirectory()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper");
        var configDir = Path.Combine(appDataDir, ExtensionBoundary.UserConfigsDir);

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        UserConfigsPath = configDir;
        return configDir;
    }

    private void LoadConfigFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<PracticeConfig>(json, options);
            if (config == null)
            {
                _loadErrors.Add($"{fileName}: empty or invalid JSON");
                return;
            }

            // Validate
            var error = ValidateConfig(config, fileName);
            if (error != null)
            {
                _loadErrors.Add(error);
                return;
            }

            // Name from filename (not JSON content)
            config.Name = Path.GetFileNameWithoutExtension(filePath);
            config.IsUserAuthored = true;
            config.SourcePath = filePath;

            // Don't allow overriding the built-in "Default" name
            if (config.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                config.Name = $"{config.Name} (user)";
            }

            _configs.Add(config);
        }
        catch (JsonException ex)
        {
            _loadErrors.Add($"{fileName}: JSON parse error — {ex.Message}");
        }
        catch (Exception ex)
        {
            _loadErrors.Add($"{fileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a config against sensible boundaries.
    /// Returns null if valid, or an error message if not.
    /// </summary>
    private static string? ValidateConfig(PracticeConfig config, string fileName)
    {
        // Validate difficulty bias if present
        if (!string.IsNullOrEmpty(config.DifficultyBias))
        {
            var validBiases = new[] { "easier", "harder", "match" };
            if (!validBiases.Contains(config.DifficultyBias.ToLowerInvariant()))
            {
                return $"{fileName}: difficultyBias must be 'easier', 'harder', or 'match' (got '{config.DifficultyBias}')";
            }
        }

        // Validate whitespace override if present
        if (!string.IsNullOrEmpty(config.Whitespace))
        {
            var validModes = new[] { "strict", "lenient", "normalize" };
            if (!validModes.Contains(config.Whitespace.ToLowerInvariant()))
            {
                return $"{fileName}: whitespace must be 'strict', 'lenient', or 'normalize' (got '{config.Whitespace}')";
            }
        }

        // Validate backspace override if present
        if (!string.IsNullOrEmpty(config.Backspace))
        {
            var validModes = new[] { "always", "limited", "never" };
            if (!validModes.Contains(config.Backspace.ToLowerInvariant()))
            {
                return $"{fileName}: backspace must be 'always', 'limited', or 'never' (got '{config.Backspace}')";
            }
        }

        // Validate accuracy floor if present
        if (config.AccuracyFloor.HasValue)
        {
            if (config.AccuracyFloor.Value < 0 || config.AccuracyFloor.Value > 100)
            {
                return $"{fileName}: accuracyFloor must be 0-100 (got {config.AccuracyFloor.Value})";
            }
        }

        // Validate description length
        if (config.Description != null && config.Description.Length > 500)
        {
            return $"{fileName}: description exceeds 500 character limit";
        }

        return null; // Valid
    }
}
