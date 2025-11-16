namespace Folly.Layout;

/// <summary>
/// Implements the Knuth-Plass optimal line breaking algorithm from TeX.
/// Based on "Breaking Paragraphs into Lines" by Knuth and Plass (1981).
/// This is a pure .NET implementation with zero external dependencies.
///
/// All algorithm parameters (stretch, shrink, penalties) are now configurable via LayoutOptions.
/// </summary>
internal sealed class KnuthPlassLineBreaker
{
    // Constant for infinity badness (not configurable)
    private const double InfinityBadness = 10000.0;

    // Configurable parameters (passed via constructor from LayoutOptions)
    private readonly double _linePenalty;
    private readonly double _flaggedDemerit;
    private readonly double _fitnessDemerit;
    private readonly double _spaceStretchRatio;
    private readonly double _spaceShrinkRatio;

    // Fitness classes for line endings (controls looseness/tightness transitions)
    private enum FitnessClass
    {
        Tight = 0,      // Line is tighter than ideal
        Normal = 1,     // Line is close to ideal
        Loose = 2,      // Line is looser than ideal
        VeryLoose = 3   // Line is much looser than ideal
    }

    /// <summary>
    /// Represents an item in the paragraph (box, glue, or penalty).
    /// This is the fundamental model from Knuth-Plass.
    /// </summary>
    private abstract class Item
    {
        public double Width { get; init; }
    }

    /// <summary>
    /// A box represents non-breakable content (word or character).
    /// </summary>
    private sealed class Box : Item
    {
        public string Content { get; init; } = "";
    }

    /// <summary>
    /// Glue represents stretchable/shrinkable whitespace (typically a space between words).
    /// </summary>
    private sealed class Glue : Item
    {
        public double Stretch { get; init; }  // How much this glue can expand
        public double Shrink { get; init; }   // How much this glue can compress
    }

    /// <summary>
    /// A penalty point where a line break is possible.
    /// </summary>
    private sealed class Penalty : Item
    {
        public double Cost { get; init; }      // Penalty for breaking here
        public bool Flagged { get; init; }     // True for hyphenated breaks
    }

    /// <summary>
    /// Represents a possible breakpoint in the dynamic programming solution.
    /// </summary>
    private sealed class BreakNode
    {
        public int Position { get; init; }            // Position in item list
        public int Line { get; init; }                // Line number if we break here
        public FitnessClass Fitness { get; init; }    // Fitness class of this line
        public double TotalDemerits { get; init; }    // Accumulated demerits up to this point
        public BreakNode? Previous { get; init; }     // Previous breakpoint in optimal path
        public double TotalWidth { get; init; }       // Accumulated width up to this point
        public double TotalStretch { get; init; }     // Accumulated stretch up to this point
        public double TotalShrink { get; init; }      // Accumulated shrink up to this point
    }

    private readonly Fonts.FontMetrics _fontMetrics;
    private readonly double _lineWidth;
    private readonly double _tolerance;  // How much variation from ideal width we tolerate

    public KnuthPlassLineBreaker(
        Fonts.FontMetrics fontMetrics,
        double lineWidth,
        double tolerance = 1.0,
        double spaceStretchRatio = 0.5,
        double spaceShrinkRatio = 0.333,
        double linePenalty = 10.0,
        double flaggedDemerit = 100.0,
        double fitnessDemerit = 100.0)
    {
        _fontMetrics = fontMetrics;
        _lineWidth = lineWidth;
        _tolerance = tolerance;
        _spaceStretchRatio = spaceStretchRatio;
        _spaceShrinkRatio = spaceShrinkRatio;
        _linePenalty = linePenalty;
        _flaggedDemerit = flaggedDemerit;
        _fitnessDemerit = fitnessDemerit;
    }

    /// <summary>
    /// Finds optimal breakpoints in the given text using the Knuth-Plass algorithm.
    /// Returns the indices in the original text where lines should break.
    /// </summary>
    public List<int> FindOptimalBreakpoints(string text, List<string> words, List<(int start, int end)> wordPositions)
    {
        // Convert words into items (boxes, glue, penalties)
        var items = BuildItemList(words, wordPositions);

        if (items.Count == 0)
            return new List<int>();

        // Run dynamic programming to find optimal breaks
        var breakpoints = FindBreaksViaDynamicProgramming(items);

        // Convert item positions back to character positions in original text
        return ConvertToTextPositions(breakpoints, wordPositions);
    }

    /// <summary>
    /// Converts words into a list of items (boxes, glue, penalties).
    /// This is the first step of the Knuth-Plass algorithm.
    /// </summary>
    private List<Item> BuildItemList(List<string> words, List<(int start, int end)> wordPositions)
    {
        var items = new List<Item>();
        var spaceWidth = _fontMetrics.MeasureWidth(" ");

        // Use configurable stretch and shrink ratios (from LayoutOptions)
        var spaceStretch = spaceWidth * _spaceStretchRatio;
        var spaceShrink = spaceWidth * _spaceShrinkRatio;

        for (int i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var wordWidth = _fontMetrics.MeasureWidth(word);

            // Add box for the word
            items.Add(new Box
            {
                Content = word,
                Width = wordWidth
            });

            // Add glue (space) after the word, except after the last word
            if (i < words.Count - 1)
            {
                items.Add(new Glue
                {
                    Width = spaceWidth,
                    Stretch = spaceStretch,
                    Shrink = spaceShrink
                });

                // Add a penalty point (potential line break after space)
                // Zero penalty means breaking here is free
                items.Add(new Penalty
                {
                    Width = 0,
                    Cost = 0,
                    Flagged = false
                });
            }
        }

        // Add a final penalty to force a break at the end
        items.Add(new Penalty
        {
            Width = 0,
            Cost = -InfinityBadness,  // Negative infinity = forced break
            Flagged = false
        });

        return items;
    }

    /// <summary>
    /// Implements the dynamic programming algorithm to find optimal breakpoints.
    /// This is the core of Knuth-Plass.
    /// </summary>
    private List<int> FindBreaksViaDynamicProgramming(List<Item> items)
    {
        // Active nodes represent potential breakpoints we're considering
        // We maintain one active node per fitness class to avoid exploring all combinations
        var activeNodes = new Dictionary<FitnessClass, List<BreakNode>>();

        // Initialize with start of paragraph (position 0)
        var startNode = new BreakNode
        {
            Position = 0,
            Line = 0,
            Fitness = FitnessClass.Normal,
            TotalDemerits = 0,
            Previous = null,
            TotalWidth = 0,
            TotalStretch = 0,
            TotalShrink = 0
        };

        foreach (FitnessClass fitness in Enum.GetValues<FitnessClass>())
        {
            activeNodes[fitness] = new List<BreakNode>();
        }
        activeNodes[FitnessClass.Normal].Add(startNode);

        // Accumulate widths as we scan through items
        double currentWidth = 0;
        double currentStretch = 0;
        double currentShrink = 0;

        // Process each potential breakpoint
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // Update accumulated widths
            if (item is Box box)
            {
                currentWidth += box.Width;
            }
            else if (item is Glue glue)
            {
                currentWidth += glue.Width;
                currentStretch += glue.Stretch;
                currentShrink += glue.Shrink;
            }
            else if (item is Penalty penalty)
            {
                // This is a potential breakpoint
                if (penalty.Cost < InfinityBadness)
                {
                    // Try breaking here from each active node
                    var newActiveNodes = new Dictionary<FitnessClass, List<BreakNode>>();
                    foreach (FitnessClass fitness in Enum.GetValues<FitnessClass>())
                    {
                        newActiveNodes[fitness] = new List<BreakNode>();
                    }

                    foreach (var fitnessClass in activeNodes.Keys)
                    {
                        foreach (var activeNode in activeNodes[fitnessClass])
                        {
                            // Calculate line properties if we break here
                            var lineWidth = currentWidth - activeNode.TotalWidth;
                            var lineStretch = currentStretch - activeNode.TotalStretch;
                            var lineShrink = currentShrink - activeNode.TotalShrink;

                            // Calculate adjustment ratio (how much we need to stretch/shrink)
                            var adjustment = CalculateAdjustmentRatio(lineWidth, lineStretch, lineShrink);

                            // Calculate badness (how far from ideal this line is)
                            var badness = CalculateBadness(adjustment, lineStretch, lineShrink);

                            // Skip if line is too bad
                            if (badness > InfinityBadness * _tolerance)
                                continue;

                            // Determine fitness class for this line
                            var lineFitness = ClassifyFitness(adjustment);

                            // Calculate demerits for this break
                            var demerits = CalculateDemerits(badness, penalty.Cost, penalty.Flagged,
                                activeNode.Fitness, lineFitness);

                            // Create new breakpoint
                            var newNode = new BreakNode
                            {
                                Position = i,
                                Line = activeNode.Line + 1,
                                Fitness = lineFitness,
                                TotalDemerits = activeNode.TotalDemerits + demerits,
                                Previous = activeNode,
                                TotalWidth = currentWidth,
                                TotalStretch = currentStretch,
                                TotalShrink = currentShrink
                            };

                            // Keep only the best node for each fitness class
                            var existing = newActiveNodes[lineFitness];
                            if (existing.Count == 0 || newNode.TotalDemerits < existing[0].TotalDemerits)
                            {
                                newActiveNodes[lineFitness].Clear();
                                newActiveNodes[lineFitness].Add(newNode);
                            }
                        }
                    }

                    // Merge new active nodes with existing ones
                    foreach (var fitness in newActiveNodes.Keys)
                    {
                        if (newActiveNodes[fitness].Count > 0)
                        {
                            activeNodes[fitness].AddRange(newActiveNodes[fitness]);
                        }
                    }
                }

                currentWidth += penalty.Width;
            }
        }

        // Find the best ending node
        BreakNode? bestNode = null;
        double bestDemerits = double.MaxValue;

        foreach (var fitnessClass in activeNodes.Keys)
        {
            foreach (var node in activeNodes[fitnessClass])
            {
                if (node.Position == items.Count - 1 && node.TotalDemerits < bestDemerits)
                {
                    bestDemerits = node.TotalDemerits;
                    bestNode = node;
                }
            }
        }

        // Reconstruct the optimal path
        var breakpoints = new List<int>();
        var current = bestNode;
        while (current != null && current.Previous != null)
        {
            breakpoints.Add(current.Position);
            current = current.Previous;
        }

        breakpoints.Reverse();
        return breakpoints;
    }

    /// <summary>
    /// Calculates how much we need to adjust (stretch or shrink) the line.
    /// Returns 0 for ideal, positive for stretching, negative for shrinking.
    /// </summary>
    private double CalculateAdjustmentRatio(double actualWidth, double stretch, double shrink)
    {
        var difference = _lineWidth - actualWidth;

        if (difference > 0)
        {
            // Need to stretch
            return stretch > 0 ? difference / stretch : InfinityBadness;
        }
        else if (difference < 0)
        {
            // Need to shrink
            return shrink > 0 ? -difference / shrink : InfinityBadness;
        }
        else
        {
            // Perfect fit
            return 0;
        }
    }

    /// <summary>
    /// Calculates the "badness" of a line (how far from ideal it is).
    /// Returns a value from 0 (perfect) to InfinityBadness (too bad).
    /// </summary>
    private double CalculateBadness(double adjustmentRatio, double stretch, double shrink)
    {
        if (adjustmentRatio < -1 || adjustmentRatio > 1)
        {
            return InfinityBadness;
        }

        // TeX formula: badness = 100 * |r|^3
        return 100.0 * Math.Abs(adjustmentRatio) * Math.Abs(adjustmentRatio) * Math.Abs(adjustmentRatio);
    }

    /// <summary>
    /// Classifies a line into a fitness class based on its adjustment ratio.
    /// </summary>
    private FitnessClass ClassifyFitness(double adjustmentRatio)
    {
        if (adjustmentRatio < -0.5)
            return FitnessClass.Tight;
        else if (adjustmentRatio < 0.5)
            return FitnessClass.Normal;
        else if (adjustmentRatio < 1.0)
            return FitnessClass.Loose;
        else
            return FitnessClass.VeryLoose;
    }

    /// <summary>
    /// Calculates demerits for a potential line break.
    /// Demerits penalize bad breaks and encourage consistency.
    /// Uses configurable penalties from LayoutOptions.
    /// </summary>
    private double CalculateDemerits(double badness, double penalty, bool flagged,
        FitnessClass previousFitness, FitnessClass currentFitness)
    {
        double demerits;

        if (penalty >= 0)
        {
            // Normal penalty
            demerits = Math.Pow(_linePenalty + badness + penalty, 2);
        }
        else if (penalty > -InfinityBadness)
        {
            // Bonus for breaking here
            demerits = Math.Pow(_linePenalty + badness, 2) - Math.Pow(penalty, 2);
        }
        else
        {
            // Forced break
            demerits = Math.Pow(_linePenalty + badness, 2);
        }

        // Additional penalty for flagged breaks (e.g., consecutive hyphens)
        if (flagged)
        {
            demerits += _flaggedDemerit;
        }

        // Penalty for fitness class changes (encourages consistency)
        if (Math.Abs((int)currentFitness - (int)previousFitness) > 1)
        {
            demerits += _fitnessDemerit;
        }

        return demerits;
    }

    /// <summary>
    /// Converts item positions to character positions in the original text.
    /// </summary>
    private List<int> ConvertToTextPositions(List<int> itemBreakpoints, List<(int start, int end)> wordPositions)
    {
        var textPositions = new List<int>();

        // Convert item indices to word indices (every 3 items = box + glue + penalty)
        foreach (var itemPos in itemBreakpoints)
        {
            // Each word corresponds to 3 items: box, glue, penalty
            // (except the last word which is just box + final penalty)
            int wordIndex = itemPos / 3;

            if (wordIndex < wordPositions.Count)
            {
                // Break after this word (at the end position)
                textPositions.Add(wordPositions[wordIndex].end);
            }
        }

        return textPositions;
    }
}
