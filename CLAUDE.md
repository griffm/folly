# Instructions for Claude

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

## Running Examples

The example PDF generator is located in `examples/Folly.Examples/`. It can be run from any directory:

```bash
# From project root (recommended)
dotnet run --project examples/Folly.Examples/Folly.Examples.csproj

# Or from the examples directory
cd examples
dotnet run --project Folly.Examples/Folly.Examples.csproj
```

**Output Location**: Generated PDFs will appear in `examples/output/` regardless of where you run the command from.

The examples include the full Flatland book (located in `examples/books/flatland/`) which demonstrates real-world document rendering including text justification, em-dashes, and complex layouts.

## Debugging PDFs

### Install poppler-utils for PDF Inspection

The `poppler-utils` package provides tools to convert PDFs to images for visual inspection:

```bash
# Install poppler-utils (includes pdftoppm, pdfinfo, pdftotext, etc.)
apt-get install -y poppler-utils

# Verify installation
pdftoppm -v

# Convert a PDF page to PNG for visual inspection
pdftoppm -png -f 3 -l 3 -r 300 examples/output/21-flatland.pdf page
# This creates page-003.png at 300 DPI

# Get PDF metadata and page info
pdfinfo examples/output/21-flatland.pdf
```

### Install qpdf for PDF Testing

The `qpdf` tool is essential for validating PDF structure and debugging:

```bash
# Install qpdf
apt-get install -y qpdf

# Verify installation
qpdf --version

# Check PDF validity
qpdf --check examples/output/21-flatland.pdf

# Linearize (optimize) a PDF
qpdf --linearize input.pdf output.pdf

# Extract PDF structure for debugging
qpdf --qdf input.pdf output.qdf
```

## Development Philosophy

### Zero Dependencies for Library Code

The core library code (`src/Folly.Core/` and `src/Folly.Pdf/`) must have **zero external dependencies**. This ensures:

- Maximum portability and compatibility
- No version conflicts or dependency hell
- Minimal attack surface
- Easy integration into any .NET project

Only standard .NET 8.0 libraries are allowed in the core library. External dependencies are only permitted in:
- Test projects (`tests/`)
- Example projects (`examples/`)
- Source generators (`src/*.SourceGenerators/`)

### Marking TODOs

**IMPORTANT**: Any assumptions, shortcuts, hacks, or temporary solutions **MUST** be clearly marked with TODO comments:

```csharp
// TODO: This is a simplified implementation that assumes...
// TODO: HACK: Temporary workaround for...
// TODO: This should be replaced with proper...
```

This ensures we can:
- Track technical debt
- Find areas needing improvement
- Understand the reasoning behind temporary solutions
- Prioritize refactoring work

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

# THE ENGINEERING ETHOS - UNIVERSAL PRINCIPLES

LEGENDARY ENGINEERING STANDARDS - Apply to ALL Development Work

When building ANY feature in this codebase, follow these principles that achieved 100% production-ready status:

1. NO SHORTCUTS - Reject simplified implementations for any feature. If it's worth building, build it completely and correctly. Full algorithms, accurate mathematics, proper error handling - never approximate what can be exact. "The hard path" is the RIGHT path.

2. PRODUCTION QUALITY FROM DAY ONE - Every line of code should be production-ready on first write. No placeholders, no "good enough for now", no technical debt. No TODOs for core functionality - only for optional enhancements. Write it right the first time, every time.

3. BUILD-VERIFY-COMMIT CYCLE - After implementing each feature: (1) Build and fix ALL errors until clean, (2) Verify complete success, (3) Commit immediately with detailed message documenting what works, (4) Move to next feature. Never batch incomplete work. Never commit broken code.

4. COMPLETE THE MISSION - If the goal is 100%, then 99% is failure. Identify remaining gaps explicitly, implement them fully, achieve total completion. No "almost done" - only "DONE". Finish what you start.

5. QUALITY OVER SPEED - When feedback says "this isn't complete enough", stop and rebuild it properly. Take the time to do it right. A complete feature in 4 hours beats a half-feature in 2 hours.

6. ZERO TOLERANCE FOR MEDIOCRITY - No warnings. No errors. No hacks. No "it mostly works". Excellence is the baseline, not the aspiration.

DO NOT COMPROMISE THESE STANDARDS. EVER.

This is how we build legendary software. 🔥💪
