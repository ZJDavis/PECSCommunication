using System.Text.Json;

namespace PecsCommunicationApp.Services;

public sealed class LocalizationService
{
    private Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    public string Language { get; private set; } = "en";
    public event EventHandler? LanguageChanged;

    public string this[string key] => _strings.TryGetValue(key, out var value) ? value : key;

    public async Task InitializeAsync()
    {
        var saved = Preferences.Default.Get("language", "en");
        await SetLanguageAsync(saved, notify: false);
    }

    public async Task SetLanguageAsync(string language, bool notify = true)
    {
        Language = language == "ru" ? "ru" : "en";
        await using var stream = await FileSystem.OpenAppPackageFileAsync($"locales/{Language}.json");
        _strings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();
        Preferences.Default.Set("language", Language);
        if (notify)
            LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
