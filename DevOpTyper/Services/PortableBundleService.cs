using System.IO.Compression;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Exports and imports user content as portable ZIP bundles.
///
/// Bundle format:
///   bundle.zip/
///     snippets/          — user snippet files (.json)
///     configs/           — user config files (.json)
///     manifest.json      — metadata (version, timestamp, counts)
///
/// The format is deliberately simple:
/// - Standard ZIP, no encryption, no proprietary headers.
/// - JSON files inside, same schema as the app uses.
/// - Can be unpacked and read with any text editor.
/// - No dependency on the app for reading or editing.
/// </summary>
public sealed class PortableBundleService
{
    /// <summary>
    /// Exports user snippets and configs to a ZIP file.
    /// Returns the path to the created ZIP, or null if nothing to export.
    /// </summary>
    public string? Export(string outputPath, UserContentService userContent, PracticeConfigService configService)
    {
        // Nothing to export?
        if (!userContent.HasUserContent && !configService.HasUserConfigs)
            return null;

        try
        {
            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            int snippetCount = 0;
            int configCount = 0;

            // Export user snippet files
            if (userContent.UserSnippetsPath != null && Directory.Exists(userContent.UserSnippetsPath))
            {
                snippetCount = ExportDirectory(zip, userContent.UserSnippetsPath, "snippets");
            }

            // Export user config files
            if (configService.UserConfigsPath != null && Directory.Exists(configService.UserConfigsPath))
            {
                configCount = ExportDirectory(zip, configService.UserConfigsPath, "configs");
            }

            // Write manifest
            var manifest = new BundleManifest
            {
                AppVersion = "0.7.0-dev",
                ExportedAt = DateTime.UtcNow.ToString("o"),
                SnippetFileCount = snippetCount,
                ConfigFileCount = configCount
            };

            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var manifestEntry = zip.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write(manifestJson);
            }

            return outputPath;
        }
        catch
        {
            // Clean up partial file
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
            return null;
        }
    }

    /// <summary>
    /// Imports a ZIP bundle into the user content directories.
    /// Returns an BundleImportResult describing what was imported.
    /// </summary>
    public BundleImportResult Import(string zipPath, string userSnippetsDir, string userConfigsDir)
    {
        var result = new BundleImportResult();

        try
        {
            if (!File.Exists(zipPath))
            {
                result.Error = "File not found";
                return result;
            }

            // Safety: never import into the app's install directory
            var appBase = AppContext.BaseDirectory;
            if (userSnippetsDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase) ||
                userConfigsDir.StartsWith(appBase, StringComparison.OrdinalIgnoreCase))
            {
                result.Error = "Cannot import into the app directory";
                return result;
            }

            using var zip = ZipFile.OpenRead(zipPath);

            // Validate: must have manifest or at least some JSON files
            var hasManifest = zip.Entries.Any(e =>
                e.FullName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));

            // Import snippet files
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.StartsWith("snippets/", StringComparison.OrdinalIgnoreCase) &&
                    entry.FullName.EndsWith(ExtensionBoundary.SnippetFileExtension, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(entry.Name))
                {
                    // Preserve subdirectory structure (one level)
                    var relativePath = entry.FullName["snippets/".Length..];
                    var targetPath = Path.Combine(userSnippetsDir, SanitizeRelativePath(relativePath));

                    // Ensure parent directory exists
                    var parentDir = Path.GetDirectoryName(targetPath);
                    if (parentDir != null && !Directory.Exists(parentDir))
                        Directory.CreateDirectory(parentDir);

                    // Don't overwrite existing files
                    if (!File.Exists(targetPath))
                    {
                        entry.ExtractToFile(targetPath);
                        result.SnippetFilesImported++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
                else if (entry.FullName.StartsWith("configs/", StringComparison.OrdinalIgnoreCase) &&
                         entry.FullName.EndsWith(ExtensionBoundary.ConfigFileExtension, StringComparison.OrdinalIgnoreCase) &&
                         !string.IsNullOrEmpty(entry.Name))
                {
                    var targetPath = Path.Combine(userConfigsDir, entry.Name);

                    if (!File.Exists(targetPath))
                    {
                        entry.ExtractToFile(targetPath);
                        result.ConfigFilesImported++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
            }

            result.Success = true;
        }
        catch (InvalidDataException)
        {
            result.Error = "Not a valid ZIP file";
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Exports all files from a directory into a ZIP prefix.
    /// Returns the number of files exported.
    /// </summary>
    private static int ExportDirectory(ZipArchive zip, string sourceDir, string zipPrefix)
    {
        int count = 0;

        // Top-level files
        foreach (var file in Directory.GetFiles(sourceDir, "*.json"))
        {
            var entryName = $"{zipPrefix}/{Path.GetFileName(file)}";
            zip.CreateEntryFromFile(file, entryName);
            count++;
        }

        // One level of subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var subDirName = Path.GetFileName(subDir);
            foreach (var file in Directory.GetFiles(subDir, "*.json"))
            {
                var entryName = $"{zipPrefix}/{subDirName}/{Path.GetFileName(file)}";
                zip.CreateEntryFromFile(file, entryName);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Sanitizes a relative path to prevent directory traversal.
    /// </summary>
    private static string SanitizeRelativePath(string relativePath)
    {
        // Replace any dangerous characters
        var sanitized = relativePath
            .Replace("..", "")
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Remove any leading separator
        return sanitized.TrimStart(Path.DirectorySeparatorChar);
    }
}

/// <summary>
/// Metadata for a portable bundle.
/// </summary>
public sealed class BundleManifest
{
    public string AppVersion { get; set; } = "";
    public string ExportedAt { get; set; } = "";
    public int SnippetFileCount { get; set; }
    public int ConfigFileCount { get; set; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public sealed class BundleImportResult
{
    public bool Success { get; set; }
    public int SnippetFilesImported { get; set; }
    public int ConfigFilesImported { get; set; }
    public int Skipped { get; set; }
    public string? Error { get; set; }

    public string Summary
    {
        get
        {
            if (!Success)
                return $"Import failed: {Error}";

            var parts = new List<string>();
            if (SnippetFilesImported > 0)
                parts.Add($"{SnippetFilesImported} snippet file(s)");
            if (ConfigFilesImported > 0)
                parts.Add($"{ConfigFilesImported} config file(s)");
            if (Skipped > 0)
                parts.Add($"{Skipped} skipped (already exist)");

            return parts.Count > 0
                ? $"Imported: {string.Join(", ", parts)}"
                : "Nothing to import";
        }
    }
}
