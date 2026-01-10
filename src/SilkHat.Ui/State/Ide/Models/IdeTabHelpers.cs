using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Models;

public static class IdeTabHelpers
{
    public static List<IdeAnnotatedLine> BuildAnnotatedLines(
        string content,
        IReadOnlyList<GitFileDiffLineModel> diffLines,
        bool showDiff,
        int? highlightLine,
        int? highlightStartLine,
        int? highlightEndLine)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var rawLines = normalized.Split('\n', StringSplitOptions.None);
        var deletionsByLine = new Dictionary<int, List<GitFileDiffLineModel>>();
        var additions = new HashSet<int>();

        if (showDiff)
        {
            foreach (var line in diffLines)
            {
                if (line.Kind == GitDiffLineKind.Add)
                {
                    additions.Add(line.LineNumber);
                }
                else
                {
                    if (!deletionsByLine.TryGetValue(line.LineNumber, out var bucket))
                    {
                        bucket = new List<GitFileDiffLineModel>();
                        deletionsByLine[line.LineNumber] = bucket;
                    }
                    bucket.Add(line);
                }
            }
        }

        var result = new List<IdeAnnotatedLine>();
        for (var index = 0; index < rawLines.Length; index++)
        {
            var lineNumber = index + 1;
            if (deletionsByLine.TryGetValue(lineNumber, out var deletes))
            {
                foreach (var deleted in deletes)
                {
                    result.Add(new IdeAnnotatedLine(lineNumber, deleted.Content, IdeLineDecoration.Deleted));
                }
            }

            var decoration = additions.Contains(lineNumber) ? IdeLineDecoration.Added : IdeLineDecoration.None;
            var highlightRange = highlightStartLine.HasValue && highlightEndLine.HasValue
                && lineNumber >= highlightStartLine.Value
                && lineNumber <= highlightEndLine.Value;
            if ((highlightRange || (highlightLine.HasValue && highlightLine.Value == lineNumber))
                && decoration == IdeLineDecoration.None)
            {
                decoration = IdeLineDecoration.Highlight;
            }
            result.Add(new IdeAnnotatedLine(lineNumber, rawLines[index], decoration));
        }

        foreach (var key in deletionsByLine.Keys.Where(key => key > rawLines.Length).OrderBy(key => key))
        {
            foreach (var deleted in deletionsByLine[key])
            {
                result.Add(new IdeAnnotatedLine(key, deleted.Content, IdeLineDecoration.Deleted));
            }
        }

        return result;
    }
}
