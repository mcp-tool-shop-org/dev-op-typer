using DevOpTyper.Content.Abstractions;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// IContentSource that enumerates code files from a local folder.
/// Filters by code file extensions, respects size limits, and
/// produces RawContent items with Source="corpus".
/// </summary>
public sealed class FolderContentSource : IContentSource
{
    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".py", ".js", ".ts", ".cs", ".java", ".rs", ".go", ".cpp", ".c", ".h",
        ".rb", ".php", ".swift", ".kt", ".scala", ".lua", ".sh", ".bash",
        ".sql", ".r", ".m", ".ps1", ".zig", ".nim", ".dart", ".ex", ".exs",
        ".hs", ".ml", ".fs", ".fsx", ".clj", ".lisp", ".el", ".vim",
        ".jsx", ".tsx", ".vue", ".svelte", ".html", ".css", ".scss",
        ".yaml", ".yml", ".toml", ".json", ".xml", ".md", ".txt"
    };

    private readonly string _rootPath;
    private readonly int _maxFileSize;

    public FolderContentSource(string rootPath, int maxFileSize = 2 * 1024 * 1024)
    {
        _rootPath = rootPath;
        _maxFileSize = maxFileSize;
    }

    public async IAsyncEnumerable<RawContent> EnumerateAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(_rootPath))
            yield break;

        var files = Directory.EnumerateFiles(_rootPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(file);
            if (!CodeExtensions.Contains(ext))
                continue;

            // Skip files exceeding size limit
            try
            {
                var info = new FileInfo(file);
                if (info.Length > _maxFileSize || info.Length == 0)
                    continue;
            }
            catch
            {
                continue;
            }

            // Skip binary files and common non-code directories
            var relativePath = Path.GetRelativePath(_rootPath, file);
            if (ShouldSkipPath(relativePath))
                continue;

            string text;
            try
            {
                text = await File.ReadAllTextAsync(file, ct);
            }
            catch
            {
                continue;
            }

            // Skip files that look binary (high ratio of non-printable chars)
            if (LooksBinary(text))
                continue;

            yield return new RawContent(
                Path: file,
                LanguageHint: null,
                Title: Path.GetFileName(file),
                Text: text,
                Source: "corpus",
                Origin: _rootPath
            );
        }
    }

    private static bool ShouldSkipPath(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
        {
            if (part.StartsWith('.'))
                return true;
            if (part is "node_modules" or "bin" or "obj" or "target" or "dist" or "build"
                or "__pycache__" or ".git" or ".vs" or ".vscode" or "packages"
                or "vendor" or "venv" or ".env")
                return true;
        }
        return false;
    }

    private static bool LooksBinary(string text)
    {
        if (text.Length == 0) return false;
        int nonPrintable = 0;
        int check = Math.Min(text.Length, 512);
        for (int i = 0; i < check; i++)
        {
            char c = text[i];
            if (c != '\t' && c != '\r' && c != '\n' && c < ' ')
                nonPrintable++;
        }
        return (double)nonPrintable / check > 0.1;
    }
}
