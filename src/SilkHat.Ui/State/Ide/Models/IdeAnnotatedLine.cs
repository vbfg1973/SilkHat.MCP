namespace SilkHat.Ui.State.Ide.Models;

public enum IdeLineDecoration
{
    None = 0,
    Added = 1,
    Deleted = 2,
    Highlight = 3
}

public sealed record IdeAnnotatedLine(int? LineNumber, string Text, IdeLineDecoration Decoration);
