using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Models;

public sealed class IdeOpenFileTab
{
    public IdeOpenFileTab(
        string repositoryPath,
        string displayPath,
        string name,
        string content)
    {
        RepositoryPath = repositoryPath;
        DisplayPath = displayPath;
        Name = name;
        Content = content;
        RenderLines = IdeTabHelpers.BuildAnnotatedLines(content, Array.Empty<GitFileDiffLineModel>(), false, null, null, null);
    }

    public string RepositoryPath { get; }
    public string DisplayPath { get; }
    public string Name { get; }
    public string Content { get; }
    public string? LastCommitAuthor { get; set; }
    public string? LastCommitAuthorEmail { get; set; }
    public DateTimeOffset? LastCommitDateUtc { get; set; }
    public string? AbbreviatedSha { get; set; }
    public string? LastCommitSubject { get; set; }
    public int? ChangeCount { get; set; }
    public bool ShowDiff { get; set; }
    public bool DiffLoaded { get; set; }
    public List<GitFileDiffLineModel> DiffLines { get; set; } = new();
    public List<IdeAnnotatedLine> RenderLines { get; set; }
    public int? HighlightLine { get; set; }
    public int? HighlightStartLine { get; set; }
    public int? HighlightEndLine { get; set; }

    public bool HasCommitInfo =>
        LastCommitDateUtc.HasValue
        && !string.IsNullOrWhiteSpace(LastCommitAuthor)
        && !string.IsNullOrWhiteSpace(LastCommitAuthorEmail)
        && !string.IsNullOrWhiteSpace(AbbreviatedSha);
}
