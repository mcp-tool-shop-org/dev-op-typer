# Snippet Pack Schema (JSON)

Each file in `DevOpTyper/Assets/Snippets/*.json` is a JSON array of snippets.

## Fields
- `id` (string) unique
- `language` (string) e.g. `python`, `java`, `csharp`, `javascript`
- `difficulty` (int) 1..10
- `title` (string)
- `code` (string) the exact code users must type (include \n)
- `topics` (string[]) tags (loops, generics, async, etc.)
- `explain` (string[]) 1–3 short teaching bullets

## Optional fields (recommended)
- `symbols` (string[]) clusters e.g. ["()","{}","=>","!="]
- `strictWhitespace` (bool) if a snippet requires strict trailing spaces rules
- `source` (string) origin attribution (your own, book, etc.)
