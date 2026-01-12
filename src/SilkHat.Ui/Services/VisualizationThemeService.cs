using MudBlazor;
using SilkHat.Ui.Models;

namespace SilkHat.Ui.Services
{
    public sealed class VisualizationThemeService
    {
        private readonly MudTheme _theme = new();
        private readonly ThemeService _themeService;

        public VisualizationThemeService(ThemeService themeService)
        {
            _themeService = themeService;
        }

        public VisualizationThemePalette GetPalette()
        {
            object palette = _themeService.IsDarkMode ? _theme.PaletteDark : _theme.PaletteLight;

            return new VisualizationThemePalette(
                GetColor(palette, "Primary", "#1e88e5"),
                GetColor(palette, "Secondary", "#26a69a"),
                GetOptionalColor(palette, "Tertiary"),
                GetColor(palette, "Background", "#ffffff"),
                GetColor(palette, "Surface", "#ffffff"),
                GetColor(palette, "TextPrimary", "#222222"),
                GetColor(palette, "TextSecondary", "#5f6368"),
                GetOptionalColor(palette, "LinesDefault"));
        }

        private static string GetColor(object palette, string propertyName, string fallback)
        {
            var value = GetOptionalColor(palette, propertyName);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string? GetOptionalColor(object palette, string propertyName)
        {
            var property = palette.GetType().GetProperty(propertyName);
            return property?.GetValue(palette) as string;
        }
    }
}