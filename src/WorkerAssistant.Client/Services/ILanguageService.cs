using Microsoft.JSInterop;

namespace WorkerAssistant.Client.Services
{
    public interface ILanguageService
    {
        string CurrentLanguage { get; }
        event Action OnLanguageChanged;
        void SetLanguage(string languageCode);
    }

    public class LanguageService(IJSRuntime jsRuntime) : ILanguageService
    {
        private IJSObjectReference? _module;

        public string CurrentLanguage { get; private set; } = "ru";
        public event Action? OnLanguageChanged;

        public async Task InitializeAsync()
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./webllm-interop.js");
            var savedLang = await _module.InvokeAsync<string>("getCookie", "user_lang");
            if (!string.IsNullOrEmpty(savedLang) && (savedLang == "en" || savedLang == "ru"))
            {
                CurrentLanguage = savedLang;
            }
        }

        public async void SetLanguage(string languageCode)
        {
            if (languageCode != CurrentLanguage && _module is not null)
            {
                CurrentLanguage = languageCode;
                await _module.InvokeVoidAsync("setCookie", "user_lang", languageCode, 365);
                OnLanguageChanged?.Invoke();
            }
        }
    }
}
