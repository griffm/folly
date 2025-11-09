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

## Documentation Maintenance

**IMPORTANT**: When implementing new features or completing milestones, always update the following documentation files:

1. **README.md** - Update the "Current Status" section and feature lists to reflect:
   - Completed milestones (mark with ✅ or 🚧 for in-progress)
   - New features in the "Layout Engine" and "PDF Rendering" sections
   - Any changes to the "Quality Assurance" metrics

2. **PLAN.md** - Update milestone deliverables to reflect:
   - Mark completed deliverables with [x]
   - Update milestone status (✅ for completed, 🚧 for in-progress)
   - Add details/notes to completed items for clarity

3. **Examples** - When adding new features, consider adding examples to demonstrate the capabilities

**Workflow**: After implementing a feature:
1. Test the feature thoroughly
2. Update README.md with the new capability
3. Update PLAN.md to mark the deliverable as complete
4. Commit all changes together with a clear commit message
5. Keep documentation in sync with the codebase at all times
