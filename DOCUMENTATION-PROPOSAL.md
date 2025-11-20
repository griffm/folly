# Documentation Improvement Proposal

## Executive Summary

The Folly documentation has grown organically and now contains:
- **Outdated planning documents** taking up significant space (PLAN.md: 1,065 lines)
- **Historical implementation details** marked as such but still in active docs (FONT-SYSTEM-ROADMAP.md: 1,456 lines)
- **Some redundancy** between README.md, PLAN.md, and docs/ directory
- **Minor CLAUDE.md guideline violations** (specific example counts)

**Recommendation**: Archive historical content, consolidate current docs, and establish clear hierarchy.

---

## Specific Issues

### 1. PLAN.md - Completed Refactoring Plan (1,065 lines)

**Current State:**
- All phases marked complete (✅)
- Only pending: NuGet publishing (🔄)
- Contains detailed implementation history of package refactoring
- Describes the journey from monolithic to modular architecture

**Issues:**
- Title "PLAN.md" suggests future roadmap, but it's historical
- New contributors may be confused about what's planned vs. completed
- 1,000+ lines of implementation details no longer actionable

**Recommendation:**
```
Option A (Preferred): Archive and Summarize
1. Move to `docs/history/REFACTORING.md`
2. Replace PLAN.md with concise ROADMAP.md focusing on:
   - Current milestone status
   - Upcoming features (M4 completion items)
   - NuGet publishing plan
   - Future directions

Option B: Radical Simplification
1. Keep PLAN.md but reduce to ~200 lines
2. Summary of completed refactoring
3. Clear section on what's next
4. Link to archived details for those interested
```

### 2. FONT-SYSTEM-ROADMAP.md - Historical Planning Doc (1,456 lines)

**Current State:**
- Starts with: "Note: This is a historical planning document"
- Contains TrueType/OpenType implementation plans (completed)
- Extremely detailed phase-by-phase breakdown
- Located in `docs/architecture/` (active docs area)

**Issues:**
- 1,456 lines of historical content in active architecture docs
- Confusing to have "roadmap" that's actually history
- Takes focus away from current architecture docs

**Recommendation:**
```
Move to docs/history/FONT-SYSTEM-IMPLEMENTATION.md
OR
Delete entirely (implementation is complete, details are in git history)

Replace with concise docs/architecture/font-system.md (~300-500 lines):
- Current architecture overview
- How fonts work in Folly
- TrueType/OpenType support details
- API usage examples
- Performance characteristics
```

### 3. README.md - Specific Metrics Violations

**Current State:**
- Line 134: "39 XSL-FO Examples"
- Line 173: "29 SVG Examples"
- CLAUDE.md guideline: Avoid specific numeric metrics

**Issues:**
- Requires manual updates when examples added
- Minor violation of project guidelines

**Recommendation:**
```diff
- ### XSL-FO Examples (39)
+ ### XSL-FO Examples

- ### SVG Examples (29)
+ ### SVG Examples

OR (if counts are valuable):
- ### XSL-FO Examples (39)
+ ### Extensive XSL-FO Examples
```

### 4. Documentation Overlap and Redundancy

**Current Situation:**
```
README.md
├── Quick Start (installation, basic usage)
├── Architecture (high-level overview)
├── Features (detailed list)
├── Current Status (milestone progress)
└── Examples (descriptions)

PLAN.md
├── Executive Summary (refactoring goals)
├── Architecture Assessment (detailed analysis)
├── Proposed Architecture (package structure)
├── Phase-by-phase implementation (completed)
└── Success Metrics

docs/guides/getting-started.md
├── Installation
├── Quick Start
├── Basic Concepts
└── Common Tasks

docs/architecture/overview.md
├── System Architecture
├── Core Components
├── Data Flow
└── Design Principles
```

**Issues:**
- Architecture described in 3 places (README, PLAN, docs/architecture/overview.md)
- Quick start in 2 places (README, docs/guides/getting-started.md)
- Unclear which is "source of truth"

**Recommendation:**
```
Establish Clear Hierarchy:

README.md (300-400 lines max)
├── What is Folly? (2-3 paragraphs)
├── Key Features (qualitative bullet points)
├── Quick Start (minimal - installation + hello world)
├── Documentation Links → docs/README.md
├── Building from Source
├── Contributing
└── License

docs/README.md (Documentation Hub)
├── Getting Started → guides/getting-started.md
├── Architecture → architecture/overview.md
├── Guides → guides/
├── API Reference (future)
└── Examples

docs/architecture/overview.md
└── Detailed architecture (current is good)

docs/guides/getting-started.md
└── Comprehensive tutorial (current is good)

ROADMAP.md (New - replaces PLAN.md)
├── Current Status (M0-M4 milestones)
├── Upcoming Features
├── NuGet Publishing Plan
└── Future Directions

docs/history/ (New directory)
├── REFACTORING.md (archived PLAN.md)
└── FONT-SYSTEM-IMPLEMENTATION.md (archived roadmap)
```

### 5. Unclear Documentation Entry Points

**Current Issues:**
- README.md doesn't clearly direct users to docs/ for more info
- docs/README.md exists but isn't prominently linked
- Architecture docs are excellent but hidden

**Recommendation:**
```markdown
Add to README.md (after Quick Start):

## Documentation

Folly has comprehensive documentation covering all aspects:

- **[Getting Started Guide](docs/guides/getting-started.md)** - Installation, tutorials, and common tasks
- **[Architecture Overview](docs/architecture/overview.md)** - System design and component structure
- **[Performance Guide](docs/guides/performance.md)** - Performance characteristics and optimization
- **[Limitations](docs/guides/limitations.md)** - Current constraints and workarounds
- **[Examples](examples/README.md)** - Extensive working examples
- **[Roadmap](ROADMAP.md)** - Current status and future plans

For API details, see the [Documentation Index](docs/README.md).
```

---

## Proposed Action Plan

### Phase 1: Archive Historical Content (Low Risk)

**Create docs/history/ directory:**
```bash
mkdir -p docs/history

# Move PLAN.md (after extracting current roadmap)
git mv PLAN.md docs/history/REFACTORING.md

# Move or delete FONT-SYSTEM-ROADMAP.md
git mv docs/architecture/FONT-SYSTEM-ROADMAP.md docs/history/FONT-SYSTEM-IMPLEMENTATION.md
# OR
git rm docs/architecture/FONT-SYSTEM-ROADMAP.md
```

**Add docs/history/README.md:**
```markdown
# Historical Documentation

This directory contains historical planning and implementation documents
that are no longer actively maintained but preserved for reference.

- **[REFACTORING.md](REFACTORING.md)** - Package refactoring journey (2024-2025)
- **[FONT-SYSTEM-IMPLEMENTATION.md](FONT-SYSTEM-IMPLEMENTATION.md)** - TrueType/OpenType implementation plan

These documents describe completed work. For current plans, see [ROADMAP.md](../../ROADMAP.md).
```

### Phase 2: Create Concise ROADMAP.md (Medium Priority)

**New ROADMAP.md (200-300 lines):**
```markdown
# Folly Roadmap

## Current Status (January 2025)

### Milestone M0: Foundation ✅ (Completed)
Core FO parsing, property system, basic layout

### Milestone M1: Basic Layout ✅ (Completed)
Multi-page layout, line breaking, text measurement

### Milestone M2: Tables, Images, Lists ✅ (Completed)
Complex tables, image embedding (5 formats), list formatting

### Milestone M3: Pagination Mastery ✅ (Completed)
Keep constraints, markers, footnotes, multi-column layout

### Milestone M4: Full Spec & Polish 🚧 (In Progress)
Advanced features, performance tuning, comprehensive testing

---

## Package Architecture (Completed ✅)

Folly has been successfully refactored into 10 focused packages:

**Tier 1: Foundation Libraries** (Zero Dependencies)
- Folly.Typography - BiDi, hyphenation, line breaking
- Folly.Images - JPEG, PNG, BMP, GIF, TIFF parsers
- Folly.Svg - SVG 1.1 parser with CSS
- Folly.Fonts - TrueType/OpenType parsing

**Tier 2: Layout Abstraction**
- Folly.Layout - Format-agnostic Area Tree

**Tier 3: Format-Specific**
- Folly.Xslfo.Model - FO DOM and properties
- Folly.Xslfo.Layout - Layout engine
- Folly.Pdf.Core - PDF generation

**Tier 4: Composition**
- Folly.Core - High-level API
- Folly.Fluent - Fluent document builder

**Benefits Realized:**
- ✅ Independent packages usable outside XSL-FO
- ✅ Clear separation of concerns
- ✅ Improved testability
- ✅ Zero breaking changes for users

For refactoring details, see [docs/history/REFACTORING.md](docs/history/REFACTORING.md).

---

## Upcoming Work

### NuGet Publishing (Next Priority)

**Status:** All packages ready, pending publication

**Packages to publish:**
1. Folly.Typography
2. Folly.Images
3. Folly.Svg
4. Folly.Fonts (already exists)
5. Folly.Layout
6. Folly.Xslfo.Model
7. Folly.Xslfo.Layout
8. Folly.Pdf.Core
9. Folly.Core
10. Folly.Fluent

**Blockers:** None - all tests passing, zero warnings

### M4 Completion Items

**High Priority:**
- [ ] Additional XSL-FO 1.1 spec coverage (remaining properties)
- [ ] Performance optimization for large documents (>1000 pages)
- [ ] Comprehensive error messages with XPath locations
- [ ] PDF/A validation improvements

**Medium Priority:**
- [ ] Advanced SVG features (masks, advanced filters)
- [ ] Additional hyphenation languages
- [ ] Font loading optimizations
- [ ] Memory usage profiling for large documents

**Low Priority:**
- [ ] API documentation generation (DocFX)
- [ ] Additional examples
- [ ] Tutorial videos/documentation

---

## Future Directions (Post M4)

### Milestone M5: Production Hardening (Proposed)

**Goals:**
- Production-grade error handling
- Extensive logging and diagnostics
- Performance monitoring hooks
- Stress testing (100k+ page documents)

### Beyond M5: Ecosystem Expansion

**Potential New Formats:**
- HTML to PDF (using Folly.Layout + new parser)
- Markdown to PDF
- ePub generation
- RTF support

**Advanced Features:**
- Interactive PDF forms
- Digital signatures
- PDF/A-2 and PDF/A-3 compliance
- Tagged PDF (accessibility)
- PDF/X support (print production)

**Developer Experience:**
- Visual FO editor
- PDF inspection tools
- Performance profiler
- Live preview capabilities

---

## Contributing to the Roadmap

We welcome suggestions for new features and priorities. Please:

1. Check existing issues on GitHub
2. Discuss major features before implementation
3. Follow contribution guidelines in README.md
4. Ensure backward compatibility

---

## Version History

- **v0.1** - M0/M1 complete (Basic rendering)
- **v0.2** - M2 complete (Tables, images, lists)
- **v0.3** - M3 complete (Pagination mastery)
- **v0.4** - M4 in progress (Full spec & polish)
- **v1.0** - Target: M4 complete + NuGet published

---

## Resources

- [Main README](README.md) - Project overview
- [Documentation Hub](docs/README.md) - Comprehensive docs
- [Architecture Overview](docs/architecture/overview.md) - System design
- [Historical Plans](docs/history/) - Completed implementation details
```

### Phase 3: Simplify README.md (Medium Priority)

**Changes:**
1. Remove specific example counts (39, 29)
2. Add prominent "Documentation" section linking to docs/
3. Reduce architecture detail (link to docs/architecture/overview.md)
4. Keep feature list qualitative

**Before:**
```markdown
### XSL-FO Examples (39)
- **Hello World** - Basic document...
(39 detailed examples listed)

### SVG Examples (29)
- **Basic Shapes** - Rectangle, circle...
(29 examples listed)
```

**After:**
```markdown
### Extensive XSL-FO Examples
The examples directory contains comprehensive demonstrations including:
- Basic documents (hello world, multi-page)
- Complex tables (spanning, headers, footers)
- Images (all supported formats)
- Advanced features (BiDi, SVG, custom fonts)

See [examples/README.md](examples/README.md) for complete list.

### Comprehensive SVG Support
Includes working examples of:
- All SVG shapes and paths
- Transforms and gradients
- Text rendering (including textPath)
- Complex features (clipping, patterns, filters)

See [examples/svg-examples/README.md](examples/svg-examples/README.md) for details.
```

### Phase 4: Create Concise Architecture Docs (Low Priority)

**New docs/architecture/font-system.md** (300-500 lines):
Replace FONT-SYSTEM-ROADMAP.md with practical current documentation:
- How fonts work in Folly
- TrueType/OpenType support
- Font fallback and discovery
- Custom font loading
- Performance characteristics
- Usage examples

---

## Implementation Priority

**Week 1 (High Priority):**
- [x] Create this proposal
- [ ] Review and approve proposed changes
- [ ] Create docs/history/ directory
- [ ] Move PLAN.md → docs/history/REFACTORING.md
- [ ] Create new ROADMAP.md

**Week 2 (Medium Priority):**
- [ ] Move/delete FONT-SYSTEM-ROADMAP.md
- [ ] Update README.md (remove specific counts, add docs links)
- [ ] Update docs/README.md to be better documentation hub
- [ ] Create concise docs/architecture/font-system.md

**Week 3 (Low Priority):**
- [ ] Review all docs for consistency
- [ ] Update CLAUDE.md if needed
- [ ] Ensure all cross-references are valid
- [ ] Final consistency pass

---

## Success Metrics

**After implementation, documentation should:**
- ✅ Have clear entry points (README → docs/README → specific docs)
- ✅ Separate historical from current content
- ✅ Avoid specific numeric metrics that require updates
- ✅ Have single source of truth for each topic
- ✅ Be easy to navigate and understand
- ✅ Follow CLAUDE.md guidelines

**Questions to validate:**
1. Can a new user quickly understand what Folly is? → README.md
2. Can they get started in 5 minutes? → Quick Start + Getting Started Guide
3. Can they understand the architecture? → docs/architecture/overview.md
4. Can they find information about specific features? → docs/ structure
5. Is it clear what's planned vs. completed? → ROADMAP.md vs. docs/history/

---

## Alternatives Considered

### Alternative 1: Keep Everything As-Is
**Pros:** No work required
**Cons:** Confusion continues, docs get more messy over time

### Alternative 2: Delete Historical Content
**Pros:** Clean slate
**Cons:** Lose valuable context for future contributors

### Alternative 3: Collapse All Docs into README
**Pros:** Single file to maintain
**Cons:** README becomes huge, hard to navigate

**Chosen Approach:** Archive historical content, create clear hierarchy
- Balances preservation with clarity
- Establishes maintainable structure
- Minimal breaking changes (mostly moving files)

---

## Open Questions

1. **FONT-SYSTEM-ROADMAP.md**: Move to history or delete entirely?
   - Recommendation: Move to history (preserves context)

2. **PLAN.md content**: How much detail in new ROADMAP.md?
   - Recommendation: ~200-300 lines, link to archived version for details

3. **README.md length**: Current 353 lines - target length?
   - Recommendation: 300-400 lines max, move details to docs/

4. **docs/architecture/font-system.md**: Create new or skip?
   - Recommendation: Create (~300 lines) - practical guide replacing roadmap

---

## Conclusion

The documentation has grown organically and served the project well, but now needs
consolidation to improve clarity and maintainability. The proposed changes:

- **Archive** historical planning documents (PLAN.md, FONT-SYSTEM-ROADMAP.md)
- **Create** concise ROADMAP.md focusing on current status and next steps
- **Simplify** README.md and establish clear documentation hierarchy
- **Preserve** historical context in docs/history/ for reference

**Estimated effort:** 1-2 days of focused work
**Risk:** Low (mostly file moves and content editing)
**Benefit:** Significantly improved documentation clarity and maintainability
