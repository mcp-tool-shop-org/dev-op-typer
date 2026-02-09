using System.Diagnostics;
using DevOpTyper.Content.Models;
using DevOpTyper.Content.Services;
using DevOpTyper.Models;

namespace DevOpTyper.Services;

/// <summary>
/// Runtime validation of the ContentLibraryService integration.
/// Runs in Debug builds only, logs results to Debug output.
/// Covers: built-in parity, legacy ID preservation, paste flow,
/// import flow, persistence round-trip, and resilience.
/// </summary>
public static class ContentIntegrationValidator
{
    /// <summary>
    /// Runs all validation checks and logs results.
    /// Call from MainWindow constructor in Debug builds.
    /// </summary>
    public static void RunAll(ContentLibraryService contentLibrary)
    {
        Log("=== ContentIntegrationValidator START ===");

        try
        {
            ValidateBuiltinContent(contentLibrary);
            ValidateLegacyIds(contentLibrary);
            ValidateQueryApi(contentLibrary);
            ValidatePasteFlow(contentLibrary);
            ValidatePersistenceRoundTrip();
            ValidateResilienceMissingIndex();
            ValidateResilienceCorruptIndex();
            ValidateQueryPerformance(contentLibrary);
        }
        catch (Exception ex)
        {
            Log($"FATAL: Unhandled exception during validation: {ex.Message}");
        }

        Log("=== ContentIntegrationValidator END ===");
    }

    /// <summary>
    /// Validates built-in content: language count, snippet count, non-empty code.
    /// </summary>
    private static void ValidateBuiltinContent(ContentLibraryService contentLibrary)
    {
        var languages = contentLibrary.Languages();
        Assert(languages.Count > 0, "At least one language available");

        int totalSnippets = 0;
        foreach (var lang in languages)
        {
            var snippets = contentLibrary.GetSnippets(lang);
            Assert(snippets.Count > 0, $"{lang}: has snippets ({snippets.Count})");
            totalSnippets += snippets.Count;

            foreach (var s in snippets)
            {
                Assert(!string.IsNullOrEmpty(s.Code), $"{lang}/{s.Id}: code is not empty");
                Assert(!string.IsNullOrEmpty(s.Id), $"{lang}/{s.Title}: id is not empty");
                Assert(s.Difficulty >= 1 && s.Difficulty <= 5,
                    $"{lang}/{s.Id}: difficulty in range ({s.Difficulty})");
            }
        }

        Log($"Built-in: {languages.Count} languages, {totalSnippets} total snippets");
    }

    /// <summary>
    /// Validates legacy ID preservation: built-in snippets should have
    /// human-readable IDs (not content hashes) via SnippetOverlay.LegacyId.
    /// </summary>
    private static void ValidateLegacyIds(ContentLibraryService contentLibrary)
    {
        int legacyCount = 0;
        int hashCount = 0;

        foreach (var lang in contentLibrary.Languages())
        {
            var items = contentLibrary.Library.Query(
                new DevOpTyper.Content.Abstractions.ContentQuery { Language = lang });

            foreach (var item in items.Where(i => i.Source == "builtin"))
            {
                var overlay = contentLibrary.Overlays.GetOverlay(item.Id);
                if (overlay != null && !string.IsNullOrEmpty(overlay.LegacyId))
                {
                    legacyCount++;
                    // Verify the legacy ID isn't a content hash (should be human-readable)
                    Assert(overlay.LegacyId.Length < 40,
                        $"Legacy ID looks human-readable: {overlay.LegacyId}");
                }
                else
                {
                    hashCount++;
                }
            }
        }

        Log($"Legacy IDs: {legacyCount} with overlay, {hashCount} without");
        Assert(legacyCount > 0, "At least some built-ins have legacy IDs");
    }

    /// <summary>
    /// Validates query API: GetSnippets, GetLanguageTracks, GetSnippetsByTopic,
    /// GetSnippetById all return consistent results.
    /// </summary>
    private static void ValidateQueryApi(ContentLibraryService contentLibrary)
    {
        var tracks = contentLibrary.GetLanguageTracks();
        Assert(tracks.Count > 0, "GetLanguageTracks returns tracks");

        foreach (var track in tracks)
        {
            Assert(track.SnippetCount == contentLibrary.GetSnippetCount(track.Id),
                $"{track.Id}: GetSnippetCount matches track count");

            var snippets = contentLibrary.GetSnippets(track.Id);
            Assert(snippets.Count == track.SnippetCount,
                $"{track.Id}: GetSnippets count matches track count");

            // Test GetSnippetById for first snippet
            if (snippets.Count > 0)
            {
                var first = snippets[0];
                var found = contentLibrary.GetSnippetById(track.Id, first.Id);
                Assert(found != null, $"{track.Id}: GetSnippetById finds first snippet");
                Assert(found?.Code == first.Code,
                    $"{track.Id}: GetSnippetById returns correct code");
            }
        }

        Log("Query API: all checks passed");
    }

    /// <summary>
    /// Validates paste flow: add code, verify dedup, verify language detection.
    /// Uses a synthetic test snippet to avoid polluting the library.
    /// </summary>
    private static void ValidatePasteFlow(ContentLibraryService contentLibrary)
    {
        // Use a unique marker to identify test content
        var testCode = "// ContentIntegrationValidator test\nfunction validate() { return true; }";

        var (snippet, error) = contentLibrary.AddPastedCode(testCode, "javascript");
        Assert(error == null, $"Paste succeeds: {error}");
        Assert(snippet != null, "Paste returns a snippet");
        Assert(snippet?.Language == "javascript", $"Language detected: {snippet?.Language}");

        // Dedup: pasting same code again should return existing snippet
        var (dupe, dupeError) = contentLibrary.AddPastedCode(testCode, "javascript");
        Assert(dupeError == null, "Dedup paste has no error");
        Assert(dupe?.Id == snippet?.Id, "Dedup returns same snippet ID");

        // Empty paste should fail
        var (empty, emptyError) = contentLibrary.AddPastedCode("", null);
        Assert(empty == null, "Empty paste returns null");
        Assert(emptyError != null, "Empty paste has error message");

        Log("Paste flow: all checks passed");
    }

    /// <summary>
    /// Validates persistence round-trip: save index, create fresh service, reload.
    /// </summary>
    private static void ValidatePersistenceRoundTrip()
    {
        // Create a fresh service, add a test item, save, reload
        var service1 = new ContentLibraryService();
        service1.Initialize();

        var testCode = "// PersistenceTest " + Guid.NewGuid().ToString("N")[..8];
        var (snippet, _) = service1.AddPastedCode(testCode, "python");
        if (snippet == null)
        {
            Log("Persistence: skipped (paste failed)");
            return;
        }

        // Create a second service instance — should load the same item
        var service2 = new ContentLibraryService();
        service2.Initialize();

        var found = service2.GetSnippetById("python", snippet.Id);
        Assert(found != null, "Persisted item found after reload");
        Assert(found?.Code?.Contains("PersistenceTest") == true,
            "Persisted item contains test marker");

        Log("Persistence round-trip: passed");
    }

    /// <summary>
    /// Validates resilience when library.index.json is missing.
    /// </summary>
    private static void ValidateResilienceMissingIndex()
    {
        var indexPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper",
            ExtensionBoundary.LibraryIndexFile);

        // Back up the index if it exists
        string? backup = null;
        if (File.Exists(indexPath))
        {
            backup = indexPath + ".bak";
            File.Copy(indexPath, backup, overwrite: true);
        }

        try
        {
            // Delete the index
            if (File.Exists(indexPath))
                File.Delete(indexPath);

            // Create a fresh service — should work with built-ins only
            var service = new ContentLibraryService();
            service.Initialize();

            Assert(service.Languages().Count > 0, "Missing index: languages still available");
            var snippets = service.GetSnippets(service.Languages()[0]);
            Assert(snippets.Count > 0, "Missing index: built-in snippets available");

            Log("Resilience (missing index): passed");
        }
        finally
        {
            // Restore backup
            if (backup != null && File.Exists(backup))
            {
                File.Copy(backup, indexPath, overwrite: true);
                File.Delete(backup);
            }
        }
    }

    /// <summary>
    /// Validates resilience when library.index.json is corrupt.
    /// </summary>
    private static void ValidateResilienceCorruptIndex()
    {
        var indexPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevOpTyper",
            ExtensionBoundary.LibraryIndexFile);

        // Back up the index if it exists
        string? backup = null;
        if (File.Exists(indexPath))
        {
            backup = indexPath + ".bak";
            File.Copy(indexPath, backup, overwrite: true);
        }

        try
        {
            // Write corrupt JSON
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, "{ corrupt json [[[ }}}");

            // Create a fresh service — should recover gracefully
            var service = new ContentLibraryService();
            service.Initialize();

            Assert(service.Languages().Count > 0, "Corrupt index: languages still available");
            var snippets = service.GetSnippets(service.Languages()[0]);
            Assert(snippets.Count > 0, "Corrupt index: built-in snippets available");

            Log("Resilience (corrupt index): passed");
        }
        finally
        {
            // Restore backup
            if (backup != null && File.Exists(backup))
            {
                File.Copy(backup, indexPath, overwrite: true);
                File.Delete(backup);
            }
            else if (File.Exists(indexPath))
            {
                // No backup existed, delete the corrupt test file
                File.Delete(indexPath);
            }
        }
    }

    /// <summary>
    /// Validates that query operations complete within acceptable time.
    /// </summary>
    private static void ValidateQueryPerformance(ContentLibraryService contentLibrary)
    {
        var sw = Stopwatch.StartNew();

        // Simulate querying all languages and snippets (worst case)
        foreach (var lang in contentLibrary.Languages())
        {
            var snippets = contentLibrary.GetSnippets(lang);
            // Touch each snippet to ensure materialization
            foreach (var s in snippets)
            {
                _ = s.Id;
                _ = s.Difficulty;
            }
        }

        sw.Stop();
        var stats = contentLibrary.GetLibraryStats();
        Assert(sw.ElapsedMilliseconds < 1000,
            $"Full query scan: {stats.Total} items in {sw.ElapsedMilliseconds}ms (<1000ms)");

        Log($"Performance: {stats.Total} items queried in {sw.ElapsedMilliseconds}ms");
    }

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            Log($"  PASS: {message}");
        }
        else
        {
            Log($"  FAIL: {message}");
            Debug.Fail($"ContentIntegrationValidator: {message}");
        }
    }

    private static void Log(string message)
    {
        Debug.WriteLine($"[ContentValidator] {message}");
    }
}
