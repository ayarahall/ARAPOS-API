using Microsoft.JSInterop;

namespace Ayapos.EndUser.Services.Ui;

public sealed class UiLanguageState
{
    private const string StorageKey = "ayapos.enduser.ui.language.v1";
    private readonly IJSRuntime _jsRuntime;

    public UiLanguageState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string Current { get; private set; } = "ar";

    public bool IsArabic => string.Equals(Current, "ar", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        var stored = await _jsRuntime.InvokeAsync<string?>("ayaposSession.get", StorageKey);
        if (!string.IsNullOrWhiteSpace(stored))
            Current = NormalizeLanguage(stored);

        await _jsRuntime.InvokeVoidAsync("ayaposUi.applyLanguage", Current);
    }

    public async Task SetLanguageAsync(string language)
    {
        Current = NormalizeLanguage(language);
        await _jsRuntime.InvokeVoidAsync("ayaposSession.set", StorageKey, Current);
        await _jsRuntime.InvokeVoidAsync("ayaposUi.applyLanguage", Current);
    }

    public async Task ToggleAsync()
        => await SetLanguageAsync(IsArabic ? "en" : "ar");

    public string Text(string english, string arabic)
        => IsArabic ? arabic : english;

    private static string NormalizeLanguage(string? value)
        => string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
}
