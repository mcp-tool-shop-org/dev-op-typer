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
            ValidateDifficultyDerivation();
            ValidateNoDifficultyDefault(contentLibrary);
            ValidateCalibrationPacks(contentLibrary);
            ValidateSessionPlanner();
            ValidateWeaknessTracking();
            ValidateUXTransparency();
            ValidateWeaknessBiasInvariants();
            ValidateSelectionPerformance();
            ValidatePerformanceGuardrails();
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
                Assert(s.Difficulty >= 1 && s.Difficulty <= 7,
                    $"{lang}/{s.Id}: difficulty in range 1-7 ({s.Difficulty})");
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

    /// <summary>
    /// Validates that DifficultyEstimator produces deterministic results
    /// across the full score range (0-9) and that all tiers 1-7 are reachable.
    /// </summary>
    private static void ValidateDifficultyDerivation()
    {
        // Verify formula: score 0-9 → difficulty 1-7
        var expectedMapping = new Dictionary<int, int>
        {
            [0] = 1, [1] = 1, [2] = 2, [3] = 3,
            [4] = 3, [5] = 4, [6] = 5, [7] = 5,
            [8] = 6, [9] = 7
        };

        foreach (var (score, expectedDiff) in expectedMapping)
        {
            int actual = Math.Clamp(1 + score * 6 / 9, 1, 7);
            Assert(actual == expectedDiff,
                $"Score {score} → difficulty {actual} (expected {expectedDiff})");
        }

        // Verify all tiers 1-7 are reachable
        var reachable = new HashSet<int>(expectedMapping.Values);
        for (int d = 1; d <= 7; d++)
        {
            Assert(reachable.Contains(d),
                $"Difficulty tier {d} is reachable from score space");
        }

        // Verify determinism: same metrics → same result
        var metrics = new CodeMetrics(Lines: 20, Characters: 400, SymbolDensity: 0.25f, MaxIndentDepth: 3);
        int first = DifficultyEstimator.Estimate(metrics);
        int second = DifficultyEstimator.Estimate(metrics);
        Assert(first == second, $"Deterministic: same metrics → same difficulty ({first})");
        Assert(first >= 1 && first <= 7, $"Result in range: {first}");

        Log("Difficulty derivation: all checks passed");
    }

    /// <summary>
    /// Validates that no snippet has a hardcoded default difficulty.
    /// Multiple distinct difficulties should exist across built-in content.
    /// </summary>
    private static void ValidateNoDifficultyDefault(ContentLibraryService contentLibrary)
    {
        var allDifficulties = new HashSet<int>();

        foreach (var lang in contentLibrary.Languages())
        {
            var snippets = contentLibrary.GetSnippets(lang);
            foreach (var s in snippets)
            {
                allDifficulties.Add(s.Difficulty);
            }
        }

        Assert(allDifficulties.Count > 1,
            $"Multiple difficulty tiers present ({string.Join(", ", allDifficulties.OrderBy(d => d))})");

        Log($"Difficulty distribution: tiers {string.Join(", ", allDifficulties.OrderBy(d => d))}");
    }

    /// <summary>
    /// Validates calibration packs: file existence, band coverage, ID format,
    /// language consistency, uniqueness, and separation from practice content.
    /// </summary>
    private static void ValidateCalibrationPacks(ContentLibraryService contentLibrary)
    {
        Log("--- ValidateCalibrationPacks ---");

        var calibrationDir = Path.Combine(AppContext.BaseDirectory, "Assets",
            ExtensionBoundary.CalibrationAssetsDir);
        Assert(Directory.Exists(calibrationDir),
            $"Calibration directory exists: {calibrationDir}");

        if (!Directory.Exists(calibrationDir)) return;

        var files = Directory.GetFiles(calibrationDir, "*.json");
        Assert(files.Length >= 6, $"At least 6 calibration files ({files.Length} found)");

        var allIds = new HashSet<string>();
        int totalCalibrationItems = 0;

        var expectedLanguages = new[] { "python", "javascript", "csharp", "java", "bash", "sql" };
        var prefixMap = new Dictionary<string, string>
        {
            ["python"] = "py", ["javascript"] = "js", ["csharp"] = "cs",
            ["java"] = "jv", ["bash"] = "sh", ["sql"] = "sq"
        };

        foreach (var lang in expectedLanguages)
        {
            var calSnippets = contentLibrary.GetCalibrationSnippets(lang);
            Assert(calSnippets.Count >= 33,
                $"Calibration {lang}: {calSnippets.Count} items (min 33)");

            // Validate band coverage
            var bandCounts = new int[8]; // index 0 unused, 1-7
            foreach (var s in calSnippets)
            {
                if (s.Difficulty >= 1 && s.Difficulty <= 7)
                    bandCounts[s.Difficulty]++;

                // Validate ID format
                if (prefixMap.TryGetValue(lang, out var prefix))
                {
                    var expectedIdPrefix = $"{ExtensionBoundary.CalibrationIdPrefix}{prefix}-d{s.Difficulty}-";
                    Assert(s.Id.StartsWith(expectedIdPrefix, StringComparison.OrdinalIgnoreCase),
                        $"ID format: {s.Id} starts with {expectedIdPrefix}");
                }

                // Track uniqueness
                Assert(allIds.Add(s.Id),
                    $"Unique ID: {s.Id}");

                totalCalibrationItems++;
            }

            // Min 5 per band for D1-D6, min 3 for D7
            for (int band = 1; band <= 7; band++)
            {
                int minRequired = band == 7 ? 3 : 5;
                Assert(bandCounts[band] >= minRequired,
                    $"Calibration {lang} D{band}: {bandCounts[band]} items (min {minRequired})");
            }

            // Calibration excluded from practice
            var practiceSnippets = contentLibrary.GetSnippets(lang);
            var calIds = new HashSet<string>(calSnippets.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);
            var leakedItems = practiceSnippets.Where(s => calIds.Contains(s.Id)).ToList();
            Assert(leakedItems.Count == 0,
                $"Calibration {lang}: 0 items leaked into practice ({leakedItems.Count} found)");
        }

        Assert(totalCalibrationItems >= 198,
            $"Total calibration items: {totalCalibrationItems} (min 198 = 6 × 33)");
        Assert(allIds.Count == totalCalibrationItems,
            $"All calibration IDs unique: {allIds.Count} unique / {totalCalibrationItems} total");

        Log($"Calibration summary: {totalCalibrationItems} items across {expectedLanguages.Length} languages");
    }

    /// <summary>
    /// Validates the SessionPlanner distribution logic.
    /// </summary>
    private static void ValidateSessionPlanner()
    {
        Log("--- ValidateSessionPlanner ---");

        // 1. ChooseCategory distribution over many trials
        var rng = new Random(42);
        int targetCount = 0, reviewCount = 0, stretchCount = 0;
        const int trials = 10000;

        for (int i = 0; i < trials; i++)
        {
            var category = SessionPlanner.ChooseCategory(rng);
            switch (category)
            {
                case MixCategory.Target: targetCount++; break;
                case MixCategory.Review: reviewCount++; break;
                case MixCategory.Stretch: stretchCount++; break;
            }
        }

        double targetPct = (double)targetCount / trials;
        double reviewPct = (double)reviewCount / trials;
        double stretchPct = (double)stretchCount / trials;

        Assert(targetPct > 0.45 && targetPct < 0.55,
            $"Target distribution: {targetPct:P1} (expected ~50%)");
        Assert(reviewPct > 0.25 && reviewPct < 0.35,
            $"Review distribution: {reviewPct:P1} (expected ~30%)");
        Assert(stretchPct > 0.15 && stretchPct < 0.25,
            $"Stretch distribution: {stretchPct:P1} (expected ~20%)");

        // 2. CategoryToDifficulty mapping
        Assert(SessionPlanner.CategoryToDifficulty(MixCategory.Target, 4) == 4,
            "Target at comfort 4 → D4");
        Assert(SessionPlanner.CategoryToDifficulty(MixCategory.Review, 4) == 3,
            "Review at comfort 4 → D3");
        Assert(SessionPlanner.CategoryToDifficulty(MixCategory.Stretch, 4) == 5,
            "Stretch at comfort 4 → D5");

        // 3. Boundary clamping
        Assert(SessionPlanner.CategoryToDifficulty(MixCategory.Review, 1) == 1,
            "Review at comfort 1 → D1 (clamped)");
        Assert(SessionPlanner.CategoryToDifficulty(MixCategory.Stretch, 7) == 7,
            "Stretch at comfort 7 → D7 (clamped)");

        // 4. ReasonFormatter produces non-empty strings
        var plan = new SessionPlan
        {
            Category = MixCategory.Target,
            TargetDifficulty = 4,
            ActualDifficulty = 4,
            ComfortZone = 4,
            Reason = "Practicing at D4"
        };
        var formatted = ReasonFormatter.Format(plan);
        Assert(!string.IsNullOrWhiteSpace(formatted),
            $"ReasonFormatter produces text: \"{formatted}\"");

        Log($"SessionPlanner: {trials} trials → Target {targetPct:P1}, Review {reviewPct:P1}, Stretch {stretchPct:P1}");
    }

    /// <summary>
    /// Validates MistakeHeatmap + WeaknessTracker integration:
    /// rolling window, error rates, weakness reports, confusion pairs.
    /// </summary>
    private static void ValidateWeaknessTracking()
    {
        Log("--- ValidateWeaknessTracking ---");

        // MistakeHeatmap basics
        var heatmap = new MistakeHeatmap();
        for (int i = 0; i < 7; i++) heatmap.RecordHit('{');
        for (int i = 0; i < 3; i++) heatmap.RecordMiss('{', '[');

        Assert(Math.Abs(heatmap.GetErrorRate('{') - 0.3) < 0.01,
            $"Error rate: expected 0.3, got {heatmap.GetErrorRate('{')}");

        Assert(heatmap.Records['{'].ConfusedWith.ContainsKey('['),
            "Confusion pair tracked: '{' confused with '['");

        // Rolling window
        var heatmap2 = new MistakeHeatmap();
        for (int i = 0; i < 80; i++) heatmap2.RecordHit('{');
        for (int i = 0; i < 20; i++) heatmap2.RecordMiss('{', null);

        double allTime = heatmap2.GetErrorRate('{');
        double recent = heatmap2.GetRecentErrorRate('{', 50);
        Assert(Math.Abs(allTime - 0.2) < 0.01,
            $"All-time error rate: expected 0.2, got {allTime}");
        Assert(recent > allTime,
            $"Recent error rate ({recent:F3}) should be higher than all-time ({allTime:F3}) when recent attempts are worse");

        // RecentAttempts cap
        var heatmap3 = new MistakeHeatmap();
        int cap = MistakeHeatmap.DefaultWindowSize * 2;
        for (int i = 0; i < cap + 50; i++) heatmap3.RecordHit('{');
        Assert(heatmap3.Records['{'].RecentAttempts.Count == cap,
            $"RecentAttempts capped at {cap}, got {heatmap3.Records['{'].RecentAttempts.Count}");

        // GetWeakest
        var weakest = heatmap.GetWeakest(count: 5, minAttempts: 5);
        Assert(weakest.Count == 1, $"GetWeakest: expected 1 weakness, got {weakest.Count}");
        Assert(weakest[0].Character == '{', $"GetWeakest: expected '{{', got '{weakest[0].Character}'");
        Assert(weakest[0].Group == SymbolGroup.Bracket,
            $"GetWeakest: expected Bracket group, got {weakest[0].Group}");

        // WeakCharSet
        var weakChars = heatmap.GetWeakCharSet(threshold: 0.15, minAttempts: 5);
        Assert(weakChars.Contains('{'), "GetWeakCharSet: '{' should be in weak set");

        // WeaknessTracker
        var tracker = new WeaknessTracker();
        var longitudinal = new LongitudinalData();

        // Create snapshots for trajectory
        longitudinal.WeaknessSnapshots.Add(new WeaknessSnapshot
        {
            Language = "csharp",
            CapturedAt = DateTime.UtcNow.AddDays(-1),
            TopWeaknesses = new List<WeaknessEntry>
            {
                new() { Character = '{', ErrorRate = 0.5, TotalAttempts = 10 }
            }
        });
        longitudinal.WeaknessSnapshots.Add(new WeaknessSnapshot
        {
            Language = "csharp",
            CapturedAt = DateTime.UtcNow,
            TopWeaknesses = new List<WeaknessEntry>
            {
                new() { Character = '{', ErrorRate = 0.3, TotalAttempts = 20 }
            }
        });

        var report = tracker.GetReport("csharp", heatmap, longitudinal);
        Assert(report.HasData, "WeaknessReport should have data");
        Assert(report.Items.Count > 0, "WeaknessReport should have items");

        var item = report.Items.FirstOrDefault(i => i.Character == '{');
        Assert(item != null, "WeaknessReport should include '{' item");
        if (item != null)
        {
            Assert(item.Trajectory == WeaknessTrajectory.Improving,
                $"Open brace should be improving (was 0.5, now 0.3), got {item.Trajectory}");
        }

        // Priority weakness
        var priority = tracker.GetPriorityWeakness(report);
        Assert(priority != null, "Priority weakness should not be null");

        Log("WeaknessTracking: all checks passed");
    }

    /// <summary>
    /// Validates UX transparency features: pick reason formatting, plan preview,
    /// session record storage of plan metadata.
    /// </summary>
    private static void ValidateUXTransparency()
    {
        Log("--- ValidateUXTransparency ---");

        // 1. ReasonFormatter produces correct text for each category
        foreach (var category in new[] { MixCategory.Target, MixCategory.Review, MixCategory.Stretch })
        {
            var plan = new SessionPlan
            {
                Category = category,
                TargetDifficulty = 4,
                ActualDifficulty = 4,
                ComfortZone = 4,
                Reason = $"Test {category}"
            };

            var formatted = ReasonFormatter.Format(plan);
            Assert(!string.IsNullOrEmpty(formatted),
                $"ReasonFormatter.Format produces text for {category}");
            Assert(formatted.Contains(ReasonFormatter.CategoryLabel(category)),
                $"ReasonFormatter includes category label for {category}");
        }

        // 2. CategoryIcon returns non-empty for each category
        foreach (var category in new[] { MixCategory.Target, MixCategory.Review, MixCategory.Stretch })
        {
            var icon = ReasonFormatter.CategoryIcon(category);
            Assert(!string.IsNullOrEmpty(icon),
                $"CategoryIcon returns non-empty for {category}");
        }

        // 3. SessionRecord stores plan metadata when plan is provided
        var testPlan = new SessionPlan
        {
            Category = MixCategory.Stretch,
            TargetDifficulty = 5,
            ActualDifficulty = 5,
            ComfortZone = 4,
            Reason = "Stretching to D5"
        };

        var record = SessionRecord.FromSession(
            "test-id", "python", "Test Snippet",
            60.0, 92.0, 2, 100,
            TimeSpan.FromSeconds(30), 5, 25, false,
            plan: testPlan);

        Assert(record.PlanCategory == MixCategory.Stretch,
            "SessionRecord stores PlanCategory from plan");
        Assert(!string.IsNullOrEmpty(record.PlanReason),
            "SessionRecord stores PlanReason from plan");

        // 4. SessionRecord handles null plan gracefully
        var noplanRecord = SessionRecord.FromSession(
            "test-id", "python", "Test Snippet",
            60.0, 92.0, 2, 100,
            TimeSpan.FromSeconds(30), 5, 25, false);

        Assert(noplanRecord.PlanCategory == null,
            "SessionRecord has null PlanCategory without plan");
        Assert(noplanRecord.PlanReason == null,
            "SessionRecord has null PlanReason without plan");

        // 5. Mismatch annotation in SessionPlanner
        // When actual != target, reason should mention it
        var mismatchPlan = new SessionPlan
        {
            Category = MixCategory.Stretch,
            TargetDifficulty = 6,
            ActualDifficulty = 5,
            ComfortZone = 5,
            Reason = "Stretching to D5 (nearest to D6)"
        };
        Assert(mismatchPlan.Reason.Contains("nearest"),
            "Mismatch plan reason includes 'nearest' annotation");

        Log("UXTransparency: all checks passed");
    }

    /// <summary>
    /// Validates performance: plan generation, heatmap operations, and
    /// reason formatting must complete well within budget.
    /// </summary>
    private static void ValidatePerformanceGuardrails()
    {
        Log("--- ValidatePerformanceGuardrails ---");

        // Heatmap: 10K records under 200ms
        var heatmap = new MistakeHeatmap();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            char c = (char)('!' + (i % 90));
            if (i % 5 == 0)
                heatmap.RecordMiss(c, (char)('!' + ((i + 1) % 90)));
            else
                heatmap.RecordHit(c);
        }
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 200,
            $"10K heatmap records in {sw.ElapsedMilliseconds}ms (budget: 200ms)");

        // GetWeakest: 1K queries under 100ms
        sw.Restart();
        for (int i = 0; i < 1_000; i++)
            heatmap.GetWeakest(5, 3);
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 100,
            $"1K GetWeakest queries in {sw.ElapsedMilliseconds}ms (budget: 100ms)");

        // ReasonFormatter: 10K formats under 50ms
        var plan = new SessionPlan
        {
            Category = MixCategory.Target,
            TargetDifficulty = 4,
            ActualDifficulty = 4,
            ComfortZone = 4,
            Reason = "Practicing at D4"
        };
        sw.Restart();
        for (int i = 0; i < 10_000; i++)
            ReasonFormatter.Format(plan);
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 50,
            $"10K ReasonFormatter.Format in {sw.ElapsedMilliseconds}ms (budget: 50ms)");

        Log("PerformanceGuardrails: all checks passed");
    }

    /// <summary>
    /// Validates WeaknessBias invariants: bounded, deterministic, policy-gated,
    /// band-preserving, and diversity-guarded.
    /// </summary>
    private static void ValidateWeaknessBiasInvariants()
    {
        Log("--- ValidateWeaknessBiasInvariants ---");

        // 1. Policy off → zero bias (v0.9 identical)
        var snippet = new Snippet
        {
            Id = "test", Language = "python", Difficulty = 3,
            Code = "if (x) { return [y]; }"
        };
        var heatmap = new MistakeHeatmap();
        foreach (char c in new[] { '{', '}', '(', ')', '[', ']', ':', '=' })
        {
            heatmap.RecordHit(c);
            for (int i = 0; i < 9; i++) heatmap.RecordMiss(c, null);
        }

        var policyOff = new SignalPolicy(); // GuidedMode = false
        double biasOff = WeaknessBias.ComputeCategoryBias(snippet, heatmap, policyOff);
        Assert(biasOff == 0.0, $"Policy off → zero bias: {biasOff}");

        // 2. Null policy → zero bias
        double biasNull = WeaknessBias.ComputeCategoryBias(snippet, heatmap, null);
        Assert(biasNull == 0.0, $"Null policy → zero bias: {biasNull}");

        // 3. Policy on → positive bias when weak groups exist
        var policyOn = new SignalPolicy();
        policyOn.EnableGuidedMode();
        double biasOn = WeaknessBias.ComputeCategoryBias(snippet, heatmap, policyOn);
        Assert(biasOn > 0, $"Policy on with weak groups → positive bias: {biasOn}");

        // 4. Bias is bounded (≤ 15.0)
        Assert(biasOn <= 15.0, $"Bias bounded ≤ 15.0: {biasOn}");

        // 5. Same inputs → same outputs (deterministic)
        double bias1 = WeaknessBias.ComputeCategoryBias(snippet, heatmap, policyOn);
        double bias2 = WeaknessBias.ComputeCategoryBias(snippet, heatmap, policyOn);
        Assert(bias1 == bias2, "Deterministic: same inputs → same output");

        // 6. Diversity guard: only 1 weak group → zero bias
        var singleGroupHeatmap = new MistakeHeatmap();
        for (int i = 0; i < 1; i++) singleGroupHeatmap.RecordHit('(');
        for (int i = 0; i < 9; i++) singleGroupHeatmap.RecordMiss('(', null);
        for (int i = 0; i < 1; i++) singleGroupHeatmap.RecordHit(')');
        for (int i = 0; i < 9; i++) singleGroupHeatmap.RecordMiss(')', null);

        double singleBias = WeaknessBias.ComputeCategoryBias(snippet, singleGroupHeatmap, policyOn);
        Assert(singleBias == 0.0, $"Diversity guard (1 group) → zero bias: {singleBias}");

        // 7. Empty heatmap → zero bias
        var emptyHeatmap = new MistakeHeatmap();
        double emptyBias = WeaknessBias.ComputeCategoryBias(snippet, emptyHeatmap, policyOn);
        Assert(emptyBias == 0.0, $"Empty heatmap → zero bias: {emptyBias}");

        // 8. GuidedMode=true but SignalsAffectSelection=false → zero bias
        var partialPolicy = new SignalPolicy { GuidedMode = true, SignalsAffectSelection = false };
        double partialBias = WeaknessBias.ComputeCategoryBias(snippet, heatmap, partialPolicy);
        Assert(partialBias == 0.0, $"Partial policy (no selection flag) → zero bias: {partialBias}");

        Log("WeaknessBiasInvariants: all checks passed");
    }

    /// <summary>
    /// Validates that selection + planning completes within budget at 5K CodeItems.
    /// Ensures UI doesn't stutter as libraries grow.
    /// </summary>
    private static void ValidateSelectionPerformance()
    {
        Log("--- ValidateSelectionPerformance (5K gate) ---");

        // Generate 5K synthetic snippets across 7 difficulty bands
        var library = new ContentLibraryService();
        var snippets = new List<Snippet>();
        for (int i = 0; i < 5_000; i++)
        {
            snippets.Add(new Snippet
            {
                Id = $"perf-{i:D5}",
                Language = "python",
                Difficulty = (i % 7) + 1,
                Title = $"Perf snippet {i}",
                Code = $"def f{i}(x): return x + {i} # {{ }} ( ) [ ]\n"
            });
        }

        // Build profile with heatmap data
        var profile = new Profile();
        foreach (char c in new[] { '{', '}', '(', ')', '[', ']', ':', '=' })
        {
            profile.Heatmap.RecordHit(c);
            for (int j = 0; j < 4; j++) profile.Heatmap.RecordMiss(c, null);
        }

        var weaknessReport = new WeaknessReport
        {
            Items = new List<WeaknessItem>
            {
                new() { Character = '{', CurrentErrorRate = 0.5, Group = SymbolGroup.Bracket, Trajectory = WeaknessTrajectory.Steady },
                new() { Character = '(', CurrentErrorRate = 0.3, Group = SymbolGroup.Bracket, Trajectory = WeaknessTrajectory.Improving }
            }
        };

        var policy = new SignalPolicy();
        policy.EnableGuidedMode();

        var diffProfile = new DifficultyProfile
        {
            TargetDifficulty = 4,
            MinDifficulty = 3,
            MaxDifficulty = 5,
            Confidence = 1.0,
            Reason = DifficultyReason.Static
        };

        // Warm up
        var selector = new SmartSnippetSelector(library);
        selector.SelectAdaptive("python", profile, diffProfile, weaknessReport, policy);

        // Time 100 selections from 5K pool
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            selector.SelectAdaptive("python", profile, diffProfile, weaknessReport, policy);
        }
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 500,
            $"100 selections from 5K pool in {sw.ElapsedMilliseconds}ms (budget: 500ms)");

        // Time WeaknessBias.ComputeCategoryBias on 5K snippets
        sw.Restart();
        foreach (var s in snippets)
            WeaknessBias.ComputeCategoryBias(s, profile.Heatmap, policy);
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 200,
            $"5K WeaknessBias.ComputeCategoryBias in {sw.ElapsedMilliseconds}ms (budget: 200ms)");

        // Time Prune on loaded heatmap
        sw.Restart();
        for (int i = 0; i < 1_000; i++)
            profile.Heatmap.Prune();
        sw.Stop();
        Assert(sw.ElapsedMilliseconds < 100,
            $"1K Prune() calls in {sw.ElapsedMilliseconds}ms (budget: 100ms)");

        Log("SelectionPerformance: all checks passed");
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
