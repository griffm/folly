using Folly;
using Folly.Layout;
using Folly.Xslfo.Layout.Tests.Helpers;
using Xunit;

namespace Folly.Xslfo.Layout.Tests;

/// <summary>
/// Tests for proportional, percentage, and auto column widths (Phase 11.2).
/// Priority 1 (Critical) - 8 tests.
/// </summary>
public class ColumnWidthTests
{
    private TableArea? FindTableArea(AreaTree areaTree)
    {
        foreach (var page in areaTree.Pages)
        {
            var table = FindTableAreaInChildren(page.Areas);
            if (table != null)
                return table;
        }
        return null;
    }

    private TableArea? FindTableAreaInChildren(IReadOnlyList<Area> areas)
    {
        foreach (var area in areas)
        {
            if (area is TableArea tableArea)
                return tableArea;

            if (area is BlockArea blockArea && blockArea.Children.Count > 0)
            {
                var table = FindTableAreaInChildren(blockArea.Children);
                if (table != null)
                    return table;
            }
        }
        return null;
    }

    [Fact]
    public void PercentageColumnWidth_SingleColumn()
    {
        // Arrange: Create table with 25% width column in a 400pt wide body
        var table = FoSnippetBuilder.CreateTable(
            new[] { "25%" },
            FoSnippetBuilder.CreateTableRow("Test"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "400pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act: Build area tree to trigger column width calculation
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert: Find the table area and check column widths
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Single(tableArea.ColumnWidths);

        // 25% of 400pt = 100pt (no spacing since only one column)
        Assert.Equal(100.0, tableArea.ColumnWidths[0], precision: 1);
    }

    [Fact]
    public void PercentageColumnWidth_MultipleColumns()
    {
        // Arrange: Create table with three columns: 25%, 50%, 25% in 400pt body
        var table = FoSnippetBuilder.CreateTable(
            new[] { "25%", "50%", "25%" },
            FoSnippetBuilder.CreateTableRow("A", "B", "C"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "400pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(3, tableArea.ColumnWidths.Count);

        // 25%, 50%, 25% of available width (after spacing)
        // Total = 100%, so columns get proportional shares
        Assert.Equal(100.0, tableArea.ColumnWidths[0], precision: 1);
        Assert.Equal(200.0, tableArea.ColumnWidths[1], precision: 1);
        Assert.Equal(100.0, tableArea.ColumnWidths[2], precision: 1);
    }

    [Fact]
    public void ProportionalColumnWidth_Equal()
    {
        // Arrange: Create table with three 1* columns (equal) in 300pt body
        var table = FoSnippetBuilder.CreateTable(
            new[] { "proportional-column-width(1)", "proportional-column-width(1)", "proportional-column-width(1)" },
            FoSnippetBuilder.CreateTableRow("A", "B", "C"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "300pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(3, tableArea.ColumnWidths.Count);

        // Three equal columns should each be 100pt (300 / 3)
        Assert.Equal(100.0, tableArea.ColumnWidths[0], precision: 1);
        Assert.Equal(100.0, tableArea.ColumnWidths[1], precision: 1);
        Assert.Equal(100.0, tableArea.ColumnWidths[2], precision: 1);
    }

    [Fact]
    public void ProportionalColumnWidth_Weighted()
    {
        // Arrange: Create table with columns: 1*, 2*, 1* in 400pt body
        var table = FoSnippetBuilder.CreateTable(
            new[] { "proportional-column-width(1)", "proportional-column-width(2)", "proportional-column-width(1)" },
            FoSnippetBuilder.CreateTableRow("A", "B", "C"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "400pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(3, tableArea.ColumnWidths.Count);

        // Total proportional = 1 + 2 + 1 = 4
        // Column 1: 1/4 * 400 = 100pt
        // Column 2: 2/4 * 400 = 200pt
        // Column 3: 1/4 * 400 = 100pt
        Assert.Equal(100.0, tableArea.ColumnWidths[0], precision: 1);
        Assert.Equal(200.0, tableArea.ColumnWidths[1], precision: 1);
        Assert.Equal(100.0, tableArea.ColumnWidths[2], precision: 1);
    }

    [Fact]
    public void AutoColumnWidth_ContentBased()
    {
        // Arrange: Create table with two auto columns
        // One with short content, one with longer content
        var table = FoSnippetBuilder.CreateTable(
            new[] { "auto", "auto" },
            FoSnippetBuilder.CreateTableRow("Short", "This is a much longer content string"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "400pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(2, tableArea.ColumnWidths.Count);

        // Auto columns should be sized based on content
        // The second column should be wider than the first
        Assert.True(tableArea.ColumnWidths[1] > tableArea.ColumnWidths[0],
            $"Expected second column ({tableArea.ColumnWidths[1]:F1}pt) to be wider than first ({tableArea.ColumnWidths[0]:F1}pt)");

        // Both should be at least MinimumColumnWidth (50pt)
        Assert.True(tableArea.ColumnWidths[0] >= 50.0);
        Assert.True(tableArea.ColumnWidths[1] >= 50.0);
    }

    [Fact]
    public void MixedColumnWidths()
    {
        // Arrange: Test mixed widths: 100pt, 25%, proportional-column-width(2), auto
        var table = FoSnippetBuilder.CreateTable(
            new[] { "100pt", "25%", "proportional-column-width(2)", "auto" },
            FoSnippetBuilder.CreateTableRow("A", "B", "C", "D"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "800pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(4, tableArea.ColumnWidths.Count);

        // Column 1: Fixed at 100pt
        Assert.Equal(100.0, tableArea.ColumnWidths[0], precision: 1);

        // Column 2: 25% of available width (after spacing)
        // Should be approximately 200pt (25% of 800pt)
        Assert.InRange(tableArea.ColumnWidths[1], 190.0, 210.0);

        // Columns 3 and 4 share the remaining width
        // Both should be at least MinimumColumnWidth
        Assert.True(tableArea.ColumnWidths[2] >= 50.0,
            $"Column 2 ({tableArea.ColumnWidths[2]:F1}pt) should be >= 50pt");
        Assert.True(tableArea.ColumnWidths[3] >= 50.0,
            $"Column 3 ({tableArea.ColumnWidths[3]:F1}pt) should be >= 50pt");

        // Verify mixed widths are calculated correctly
        // All columns should have positive widths
        Assert.All(tableArea.ColumnWidths, width => Assert.True(width > 0));
    }

    [Fact]
    public void PercentageColumnWidth_OverConstraint()
    {
        // Arrange: Test percentages > 100% (should clamp or scale)
        // Three columns with 50% each = 150% total
        var table = FoSnippetBuilder.CreateTable(
            new[] { "50%", "50%", "50%" },
            FoSnippetBuilder.CreateTableRow("A", "B", "C"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "300pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(3, tableArea.ColumnWidths.Count);

        // Each column should get 50% of available width
        // Total will exceed available width, but columns are calculated independently
        Assert.Equal(150.0, tableArea.ColumnWidths[0], precision: 1);
        Assert.Equal(150.0, tableArea.ColumnWidths[1], precision: 1);
        Assert.Equal(150.0, tableArea.ColumnWidths[2], precision: 1);
    }

    [Fact]
    public void ColumnWidth_MinimumWidth()
    {
        // Arrange: Test that MinimumColumnWidth (50pt) constraint is enforced
        // Create a table with very narrow percentage columns in a small table
        var table = FoSnippetBuilder.CreateTable(
            new[] { "10pt", "5%" },
            FoSnippetBuilder.CreateTableRow("A", "B"));

        var foDoc = FoSnippetBuilder.CreateSimpleDocument(
            table,
            pageWidth: "200pt",
            pageHeight: "600pt",
            margin: "0pt");

        // Act
        var areaTree = foDoc.BuildAreaTree(new LayoutOptions());

        // Assert
        var tableArea = FindTableArea(areaTree);
        Assert.NotNull(tableArea);
        Assert.Equal(2, tableArea.ColumnWidths.Count);

        // Both columns should be at least MinimumColumnWidth (50pt)
        Assert.True(tableArea.ColumnWidths[0] >= 50.0,
            $"Column 0 width ({tableArea.ColumnWidths[0]:F1}pt) should be >= 50pt");
        Assert.True(tableArea.ColumnWidths[1] >= 50.0,
            $"Column 1 width ({tableArea.ColumnWidths[1]:F1}pt) should be >= 50pt");
    }
}
