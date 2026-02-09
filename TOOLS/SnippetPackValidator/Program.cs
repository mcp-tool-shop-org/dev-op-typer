// OPTIONAL TOOL (not wired into solution by default)
// Create a console project and paste this file to validate snippet packs.
using System.Text.Json;

var dir = args.Length > 0 ? args[0] : "DevOpTyper/Assets/Snippets";
if (!Directory.Exists(dir))
{
    Console.WriteLine($"Directory not found: {dir}");
    return;
}

int errors = 0;

foreach (var file in Directory.GetFiles(dir, "*.json"))
{
    Console.WriteLine($"Validating {file}...");
    try
    {
        var json = File.ReadAllText(file);
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("  ERROR: root is not an array");
            errors++;
            continue;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            string id = el.TryGetProperty("id", out var v) ? v.GetString() ?? "" : "";
            string lang = el.TryGetProperty("language", out var l) ? l.GetString() ?? "" : "";
            string title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            string code = el.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(lang) ||
                string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("  ERROR: missing required fields (id/language/title/code)");
                errors++;
                continue;
            }

            if (!seen.Add(id))
            {
                Console.WriteLine($"  ERROR: duplicate id '{id}'");
                errors++;
            }

            if (!code.EndsWith("\n"))
            {
                Console.WriteLine($"  WARN: code for '{id}' does not end with newline");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        errors++;
    }
}

Console.WriteLine(errors == 0 ? "All good." : $"Done with {errors} error(s).");
