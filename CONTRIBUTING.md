# Contributing to Dev-Op-Typer

Thank you for your interest in contributing to Dev-Op-Typer!

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Open `DevOpTyper.sln` in Visual Studio 2022
4. Create a feature branch: `git checkout -b feature/your-feature-name`

## Development Setup

### Requirements
- Windows 10 version 1809+ or Windows 11
- Visual Studio 2022 with Windows App SDK workload
- .NET 10.0 SDK

### Building
```powershell
dotnet build DevOpTyper.sln -c Debug -p:Platform=x64
```

## Contribution Guidelines

### Code Style
- Follow existing code patterns and naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and reasonably sized

### Commits
- Use conventional commit messages:
  - `feat:` for new features
  - `fix:` for bug fixes
  - `docs:` for documentation
  - `refactor:` for code refactoring
  - `test:` for tests
  - `chore:` for maintenance tasks

### Pull Requests
1. Ensure your code builds without warnings
2. Test your changes manually
3. Update documentation if needed
4. Keep PRs focused on a single change
5. Reference any related issues

## Adding Snippets

Snippet packs live in `DevOpTyper/Assets/Snippets/`. Each snippet should:
- Have a unique `id`
- Include a descriptive `title`
- Use valid, real code (not pseudocode)
- End with a newline (`\n`)
- Include `topics` for categorization

See [DOCS/SNIPPET_SCHEMA.md](DOCS/SNIPPET_SCHEMA.md) for the full schema.

## Reporting Issues

When reporting bugs, please include:
- Windows version
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable

## Questions?

Open a discussion or issue if you have questions about contributing.
