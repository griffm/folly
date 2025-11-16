# AGENT EXECUTION GUIDELINES

## 1. Environment Setup

### Install .NET SDK 8.0
Execute exactly:

apt-get install -y dotnet-sdk-8.0
dotnet --version

Do not run apt-get update before installation.
Expected version format: 8.0.xxx.

### Handling Shallow Git Checkouts
Claude Code on the web sometimes performs a shallow git checkout, which can cause build failures when the build process requires full git history.

To check if the repository is shallow:

git rev-parse --is-shallow-repository

If the output is `true`, unshallow the repository:

git fetch --unshallow

This command fetches the complete git history and resolves build issues related to shallow clones.

## 2. Running Examples

### Location
examples/Folly.Examples/

### Commands

dotnet run --project examples/Folly.Examples/Folly.Examples.csproj

# Alternative
cd examples
dotnet run --project Folly.Examples/Folly.Examples.csproj

### Output
All generated PDFs are written to:
examples/output/

## 3. PDF Inspection Tools

### poppler-utils

apt-get install -y poppler-utils
pdftoppm -v

# Render page 3 at 300 DPI
pdftoppm -png -f 3 -l 3 -r 300 examples/output/21-flatland.pdf page

# Metadata
pdfinfo examples/output/21-flatland.pdf


### qpdf

apt-get install -y qpdf
qpdf --version

qpdf --check examples/output/21-flatland.pdf
qpdf --linearize input.pdf output.pdf
qpdf --qdf input.pdf output.qdf

## 4. Code Requirements

### Zero Dependencies (Core)
Core directories must use only the .NET 8 standard library:

src/Folly.Core/
src/Folly.Pdf/

External dependencies allowed only in:
tests/
examples/
source generators/

Agents must enforce this rule on all edits.

## 5. TODO Policy

### Core
No TODOs for required functionality.
No provisional, partial, or temporary logic.
TODOs allowed only for optional enhancements.

### Non-Core
Tests, examples, source generators, and tooling may use TODOs freely.

## 6. Documentation Synchronization

Agents must update the following with each completed feature or change:

1. README.md  
   - Status sections  
   - Feature lists  
   - QA metrics  

2. PLAN.md  
   - Completed items  
   - Milestone status  
   - Brief notes if needed  

3. Examples  
   - Add or update demonstrations  

Documentation changes must be committed together with the code.

## 7. Agent Execution Rules

### Rule 1: Complete Implementations
No partial, placeholder, or simplified logic in core components.

### Rule 2: Production-Ready on First Write
All core modifications must meet production standards immediately:
- No temporary logic
- No silent assumptions
- No incomplete handling

### Rule 3: Build–Verify–Commit
Steps after implementing a feature:
1. Build with zero errors/warnings  
2. Verify expected behavior  
3. Commit with clear summary  

### Rule 4: No Unfinished Work
Features started must be finished in the same change.

### Rule 5: Quality Priority
If quality and speed conflict, choose quality.

### Rule 6: Maintain Codebase Integrity
Do not introduce:
- Workarounds  
- Inconsistent patterns  
- Temporary patches  
- Structural regressions  

## 8. Summary for Automated Agents

- Install tools exactly as specified  
- Use the defined project paths and commands  
- Enforce zero external dependencies in core  
- Use TODOs only where allowed  
- Update documentation with each completed feature  
- Produce complete, correct, production-standard implementations  
- Build, verify, and commit after each finished unit of work  