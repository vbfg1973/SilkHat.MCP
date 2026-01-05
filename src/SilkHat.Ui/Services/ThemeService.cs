using Microsoft.JSInterop;

namespace SilkHat.Ui.Services;

public sealed class ThemeService
{
    private const string StorageKey = "silkhat.theme";
    private readonly IJSRuntime _jsRuntime;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode { get; private set; }

    public async Task InitializeAsync()
    {
        var stored = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        IsDarkMode = string.Equals(stored, "dark", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetDarkModeAsync(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        var value = isDarkMode ? "dark" : "light";
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, value);
    }
}
