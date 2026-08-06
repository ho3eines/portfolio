using Blazored.LocalStorage;
using System.Text.Json;

namespace Portfolio.Web.Services;

/// <summary>
/// Loads the two supported interface dictionaries and keeps language changes
/// transactional: the visible language is only changed after its dictionary
/// has been read successfully. This avoids a flash of raw translation keys
/// when a request is slow, stale or offline.
/// </summary>
public sealed class TranslationService : IDisposable
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "fa"
    };

    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly SemaphoreSlim _changeLock = new(1, 1);
    private readonly Dictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, string> _dictionary = new();
    private string _lang = "en";

    public event Action? OnChange;

    public TranslationService(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public string Lang => _lang;
    public bool IsRtl => string.Equals(_lang, "fa", StringComparison.OrdinalIgnoreCase);
    public string Direction => IsRtl ? "rtl" : "ltr";
    public string FontFamily => IsRtl
        ? "'Vazirmatn','Tahoma',sans-serif"
        : "'Manrope',system-ui,-apple-system,'Segoe UI',sans-serif";

    /// <summary>True after a valid preference has been loaded or chosen.</summary>
    public bool LanguageSelected { get; private set; }

    /// <summary>Useful for the language gate when a translation file cannot be read.</summary>
    public string? LastError { get; private set; }

    public string this[string key] => _dictionary.TryGetValue(key, out var value) ? value : key;

    public async Task InitAsync()
    {
        string? savedLanguage = null;
        try
        {
            savedLanguage = await _storage.GetItemAsync<string>("lang");
        }
        catch
        {
            // A privacy-restricted browser can reject localStorage. The user
            // can still choose a language for the current session.
        }

        if (IsSupported(savedLanguage) && await TryActivateAsync(savedLanguage!, persist: false, notify: false))
            return;

        // Keep the gate visible when a saved value is invalid or the matching
        // file is unavailable. English is loaded as a safe dictionary for
        // shared controls, but is deliberately not persisted as a selection.
        LanguageSelected = false;
        _lang = "en";
        var fallback = await TryGetDictionaryAsync("en");
        if (fallback is not null)
            _dictionary = fallback;

        if (!string.IsNullOrWhiteSpace(savedLanguage))
        {
            try { await _storage.RemoveItemAsync("lang"); }
            catch { /* Storage is optional. */ }
        }
    }

    /// <summary>
    /// Selects a supported language. Returns false without changing the current
    /// UI if its dictionary cannot be fetched or parsed.
    /// </summary>
    public async Task<bool> SetLangAsync(string? lang)
    {
        if (!IsSupported(lang))
        {
            LastError = "Unsupported language.";
            return false;
        }

        if (string.Equals(lang, _lang, StringComparison.OrdinalIgnoreCase) && LanguageSelected)
            return true;

        return await TryActivateAsync(lang!, persist: true, notify: true);
    }

    public Task<bool> ToggleAsync() => SetLangAsync(IsRtl ? "en" : "fa");

    private async Task<bool> TryActivateAsync(string lang, bool persist, bool notify)
    {
        lang = lang.ToLowerInvariant();
        await _changeLock.WaitAsync();
        try
        {
            var dictionary = await TryGetDictionaryAsync(lang);
            if (dictionary is null)
                return false;

            // Commit only after all asynchronous work that can fail has
            // succeeded. Components therefore never render an empty map.
            _dictionary = dictionary;
            _lang = lang.ToLowerInvariant();
            LanguageSelected = true;
            LastError = null;

            if (persist)
            {
                try { await _storage.SetItemAsync("lang", _lang); }
                catch { /* The current session still works without storage. */ }
            }

            if (notify)
                OnChange?.Invoke();

            return true;
        }
        finally
        {
            _changeLock.Release();
        }
    }

    private async Task<Dictionary<string, string>?> TryGetDictionaryAsync(string lang)
    {
        if (_cache.TryGetValue(lang, out var cached))
            return cached;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var json = await _http.GetStringAsync($"lang/{lang}.json", timeout.Token);
            using var document = JsonDocument.Parse(json);

            var flattened = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Flatten(document.RootElement, string.Empty, flattened);

            // A syntactically valid but incomplete response is not a usable
            // language file. Keep the current UI intact in that situation.
            if (flattened.Count == 0 ||
                !flattened.ContainsKey("site.lang") ||
                !flattened.ContainsKey("site.direction"))
            {
                LastError = "Translation file is incomplete.";
                return null;
            }

            _cache[lang] = flattened;
            return flattened;
        }
        catch (Exception)
        {
            LastError = "Translation file could not be loaded.";
            return null;
        }
    }

    private static bool IsSupported(string? lang) =>
        !string.IsNullOrWhiteSpace(lang) && SupportedLanguages.Contains(lang);

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> destination)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.String)
                destination[key] = property.Value.GetString() ?? key;
            else
                Flatten(property.Value, key, destination);
        }
    }

    public void Dispose()
    {
        OnChange = null;
        _changeLock.Dispose();
    }
}
