# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in Dev-Op-Typer, please report it by:

1. **Do NOT** open a public issue
2. Email the maintainers directly or use GitHub's private vulnerability reporting
3. Include a detailed description of the vulnerability
4. Provide steps to reproduce if applicable

We will acknowledge receipt within 48 hours and provide a detailed response within 7 days.

## Security Considerations

Dev-Op-Typer is a desktop application that:
- Stores user data locally using Windows ApplicationData
- Does not transmit data over the network
- Does not execute user-provided code

### Data Storage
- Profile and settings are stored in `%LOCALAPPDATA%`
- No sensitive credentials are stored
- Data can be exported/imported via JSON files
