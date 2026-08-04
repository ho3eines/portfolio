using Blazored.LocalStorage;
using System.Text.Json;

namespace Portfolio.Web.Services;

/// <summary>
/// Bilingual service — T("key") returns translated text.
/// Loads wwwroot/lang/{code}.json, caches in memory.
/// Fires OnLanguageChanged → components call StateHasChanged.
/// Usage: @inject TranslationService T  →  @T["nav.work"]
/// </summary>
public class TranslationService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private Dictionary<string, string> _dict = new();
    private string _lang = "en";

    public event Action? OnChange;

    public TranslationService(HttpClient http, ILocalStorageService storage)
    { _http = http; _storage = storage; }

    public string Lang => _lang;
    public bool IsRtl => _lang == "fa";
    public bool LanguageSelected { get; private set; }

    public string this[string key] => _dict.TryGetValue(key, out var v) ? v : key;

    public async Task InitAsync()
    {
        var saved = await _storage.GetItemAsync<string>("lang");
        if (!string.IsNullOrEmpty(saved))
        {
            _lang = saved;
            LanguageSelected = true;
        }
        await LoadAsync(_lang);
    }

    public async Task SetLangAsync(string lang)
    {
        if (lang == _lang && LanguageSelected) return;
        _lang = lang;
        LanguageSelected = true;
        await _storage.SetItemAsync("lang", lang);
        await LoadAsync(lang);
        OnChange?.Invoke();
    }

    public async Task ToggleAsync() => await SetLangAsync(_lang == "en" ? "fa" : "en");

    public string Direction => _lang == "fa" ? "rtl" : "ltr";
    public string FontFamily => _lang == "fa"
        ? "'Vazirmatn','Tahoma',sans-serif"
        : "'Inter',system-ui,sans-serif";

    private async Task LoadAsync(string lang)
    {
        try
        {
            var json = await _http.GetStringAsync($"lang/{lang}.json");
            _dict = new();
            using var doc = JsonDocument.Parse(json);
            Flatten(doc.RootElement, "", _dict);
        }
        catch { _dict = new(); }
    }

    private static void Flatten(JsonElement el, string prefix, Dictionary<string, string> d)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
            {
                var k = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}";
                if (p.Value.ValueKind == JsonValueKind.String)
                    d[k] = p.Value.GetString() ?? k;
                else
                    Flatten(p.Value, k, d);
            }
        }
    }

    public void Dispose() => OnChange = null;
}
