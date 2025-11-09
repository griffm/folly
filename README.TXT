Project Brief — Folly: XSL-FO 1.1 → PDF Renderer for .NET 8

1) Objective & constraints
	•	Name: Folly — a standalone .NET 8 library.
	•	Purpose: Fully compliant XSL-FO 1.1 processor that outputs PDF 1.7 only (no XPS/SVG in v1).
	•	Platform: .NET 8 (C#), managed code only.
	•	Dependencies: Zero runtime dependencies beyond System.* (dev/test dependencies allowed).
	•	Input: .fo XML files and a fluent C# API (Folly.Fluent) to build FO documents in memory.
	•	Output: PDF via internal renderer.
	•	CI/CD: GitHub Actions with automatic build, test, and publish on merges to main.
	•	Versioning: Nerdbank.GitVersioning.

⸻

2) Features (v1)

Must-have
	•	XSL-FO 1.1 Pipeline
	•	Parse FO XML → immutable FO DOM with property resolution & validation.
	•	Full layout: pagination, block/inline model, conditional page masters, tables, footnotes, floats, markers, white-space, inheritance, keeps, breaks, bidi hooks, etc.
	•	Build deterministic Area Tree.
	•	PDF Renderer (only backend)
	•	Produce PDF 1.7 with:
	•	Font embedding/subsetting for TTF/OTF.
	•	Stable text placement, links, metadata, bookmarks.
	•	Images (JPEG passthrough, PNG decoding via built-in libraries).
	•	Graphic primitives: borders, backgrounds, rounded corners, fills.
	•	Performance targets:
	•	200-page typical mixed document in <10 seconds; memory footprint <600MB.
	•	Fluent API: Folly.Fluent to build FO constructs programmatically.
	•	Developer ergonomics: Rich validation diagnostics, XPath-locatable error messages.

Nice-to-have (future)
	•	PDF/A, tagged PDF.
	•	Pluggable hyphenation dictionaries.
	•	CLI tools for FO diffing and visual inspection.
	•	Multi-thread layout or streaming for extreme workloads.

⸻

3) Repo structure

/src
  /Folly.Core      // FO DOM, property system, layout, area tree
  /Folly.Pdf       // PDF renderer
  /Folly.Fluent    // Fluent builder API (optional reference)
/tests
  /Folly.SpecTests // conformance
  /Folly.UnitTests // component/unit tests
/build
  version.json
  Directory.Build.props
  folly-ci.yml


⸻

4) Key APIs

Load & Render

using Folly;

var fo = FoDocument.Load("input.fo");
using var pdf = File.Create("output.pdf");
fo.SavePdf(pdf);

Fluent API

using Folly.Fluent;
using static Folly.Fluent.Fo;

var doc = Document(d => d
  .LayoutMasters(lm => lm
    .SimplePageMaster("A4", a => a.PageSize(595, 842)
      .Margins(36).RegionBody().RegionBefore(36).RegionAfter(36)))
  .PageSequence("A4", ps => ps
    .StaticContent(Region.Before, s => s.Block("Title Page"))
    .Flow("xsl-region-body", f => f
      .Block("Hello Folly!")
      .Table(t => t.Columns(3, 200, 200, 195)
        .Body(body => body
          .Row(r => r
            .Cell("A").Cell("B").Cell("C")))))))
.SavePdf("output.pdf");


⸻

5) Testing & conformance
	•	Coverage metrics: FO-specific traceability matrix tied to spec clauses.
	•	PDF validation: parse output, assert structure, font subsets, resource usage, text placement.
	•	Golden AreaTree JSON snapshots for deterministic layout verification.
	•	Fuzzing: malformed XML, extreme nesting, table stress.
	•	CI: Blocks merge if coverage or conformance decreases.

⸻

6) Versioning & CI
	•	Nerdbank.GitVersioning configured via version.json:

{ "version": "0.1.0", "publicReleaseRefSpec": [ "^refs/heads/main$" ] }


	•	GitHub Actions (folly-ci.yml) triggered on PR and push to main:
	•	Build, test, conformance verification.
	•	On main: pack & publish to NuGet.

⸻

7) Milestones
	1.	M0: Foundation (1–2 weeks)
Repo, CI, versioning, empty PDF writer, FO parser skeleton.
	2.	M1: Basic layout (3–4 weeks)
Block/inline, simple pagination, fonts, text rendering → “Hello World” PDF.
	3.	M2: Tables, images, lists (4–6 weeks)
Full table model, borders, images, break/keep logic.
	4.	M3: Pagination mastery (3–4 weeks)
Markers, footnotes, floats, conditional masters.
	5.	M4: Full spec & polish (4–6 weeks)
Final compliance pass, docs, samples, publish 1.0.0 to NuGet.

⸻

8) Code snippets (starter)

FoDocument.cs:

namespace Folly;

public sealed class FoDocument : IDisposable
{
    public static FoDocument Load(string path, FoLoadOptions? options = null);
    public static FoDocument Load(Stream xml, FoLoadOptions? options = null);

    public void SavePdf(Stream output, PdfOptions? options = null)
    {
        var areaTree = BuildAreaTree();
        using var renderer = new Pdf.PdfRenderer(output, options ?? new PdfOptions());
        renderer.Render(areaTree);
    }

    public AreaTree BuildAreaTree(LayoutOptions? options = null) { /* ... */ }

    public void Dispose() { /* ... */ }
}

