namespace SilkHat.Ui.Models
{
    public sealed record VisualizationThemePalette(
        string Primary,
        string Secondary,
        string? Tertiary,
        string Background,
        string Surface,
        string TextPrimary,
        string TextSecondary,
        string? Lines)
    {
        public string GetKey()
        {
            return string.Join('|', new[]
            {
                Primary,
                Secondary,
                Tertiary ?? string.Empty,
                Background,
                Surface,
                TextPrimary,
                TextSecondary,
                Lines ?? string.Empty
            });
        }
    }
}