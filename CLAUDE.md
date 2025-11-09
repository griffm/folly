# Build Instructions for Claude

## Prerequisites

### Install .NET SDK 8.0

```bash
# Install .NET SDK 8.0
apt-get install -y dotnet-sdk-8.0

# Verify installation
dotnet --version
# Should output: 8.0.121 (or similar)
```

## Notes

- Do not run `apt-get update` before installing .NET SDK
- The installation includes ASP.NET Core runtime, templates, and targeting packs
- After installation, the .NET SDK will be available system-wide
