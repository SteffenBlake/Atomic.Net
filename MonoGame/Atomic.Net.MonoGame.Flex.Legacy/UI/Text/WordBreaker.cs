namespace Atomic.Net.MonoGame.Flex.UI.Text;

public static class WordBreaker
{
    public static WordBreakResult FitToBoundingBox(ReadOnlySpan<char> input, Vector2 characterSize, Vector2 boundingBox)
    {
        var totalChars = input.Length;

        if (totalChars == 0)
        {
            return new WordBreakResult([], float.NaN);
        }

        var perCharRatio = characterSize.X / characterSize.Y;
        var targetRatio = boundingBox.X / boundingBox.Y;
        var targetLines = Math.Max(1, (int)Math.Round(Math.Sqrt(totalChars * perCharRatio / targetRatio)));
        var targetLength = (totalChars - (targetLines - 1)) / targetLines;

        var ranges = new List<Range>(targetLines);
        var start = 0;
        var longestLine = 0;

        while (start < totalChars)
        {
            var end = Math.Min(start + targetLength, totalChars);
            var left = end;
            var right = end;

            // Scan for closest space if possible
            while (end != totalChars && (left > start || right < totalChars))
            {
                if (left > start && input[left] == ' ')
                {
                    end = left;
                    break;
                }

                if (right < totalChars && input[right] == ' ')
                {
                    end = right;
                    break;
                }

                left--;
                right++;
            }

            var lineRange = start..end;
            ranges.Add(lineRange);

            longestLine = Math.Max(longestLine, end - start);

            start = end + 1;
        }

        var renderedWidth  = longestLine * characterSize.X;
        var renderedHeight = ranges.Count * characterSize.Y;

        var scaleFactor = Math.Min(
            boundingBox.X / renderedWidth,
            boundingBox.Y / renderedHeight
        );

        return new WordBreakResult(ranges, scaleFactor);
    }

    public static WordBreakResult FitToBoundingBoxV2(ReadOnlySpan<char> input, Vector2 characterSize, Vector2 boundingBox)
    {
        var totalChars = input.Length;
        if (totalChars == 0)
        {
            return new WordBreakResult([], float.NaN);
        }

        // Step 1: initial max scale based on perfect packing
        var areaScale = MathF.Sqrt(
            boundingBox.X * boundingBox.Y /
            (totalChars * characterSize.X * characterSize.Y)
        );
        var scaleFactor = areaScale; // slight dial-back
        var minScale = 0.01f;

        List<Range> lines = [];
        var longestLineCount = 0;

        while (scaleFactor > minScale)
        {
            longestLineCount = 0;
            var scaledWidth = characterSize.X * scaleFactor;
            var scaledHeight = characterSize.Y * scaleFactor;

            var maxCharsPerLine = Math.Max(1, (int)(boundingBox.X / scaledWidth));
            var maxLines = Math.Max(1, (int)(boundingBox.Y / scaledHeight));

            lines.Clear();
            var start = 0;

            while (start < totalChars && lines.Count < maxLines)
            {
                var end = Math.Min(start + maxCharsPerLine, totalChars);

                // Only adjust if line doesn't perfectly fit
                if (end < totalChars && input[end] != ' ')
                {
                    var lastSpace = input[start..end].LastIndexOf(' ');

                    // Scenario 3: normal break at last space
                    if (lastSpace > 0)
                    {
                        end = start + lastSpace;
                    }
                    // Scenario 4: single word too long → find next space or end, recalc scale
                    else
                    {
                        var nextSpace = input[end..].IndexOf(' ');
                        if (nextSpace < 0)
                        {
                            nextSpace = totalChars - end;
                        }

                        scaleFactor = boundingBox.X / ((end + nextSpace - start) * characterSize.X);
                        break; // retry outer loop with new scale
                    }
                }

                lines.Add(start..end);
                longestLineCount = Math.Max(longestLineCount, end - start);
                start = end + 1;
            }

            // Step 2: check overshoot of the last line
            var overshoot = totalChars - start;
            if (overshoot <= 0)
            {
                lines = [.. lines];
                break;
            }

            // Reduce scale proportionally to last line overshoot
            scaleFactor *= 0.99f;
        }

        if (lines.Count == 0)
        {
            lines = [0..totalChars];
        }

        var longestLineLength = longestLineCount * characterSize.X;
        var linesHeight = lines.Count * characterSize.Y;

        scaleFactor = Math.Min(boundingBox.X / longestLineLength, boundingBox.Y / linesHeight);

        return new WordBreakResult(lines, scaleFactor);
    }
}
