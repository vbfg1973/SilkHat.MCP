namespace SilkHat.Ui.Models
{
    public sealed record IndexStatusViewModel(
        string JobType,
        string State,
        int Percent,
        string? Message);
}