using Microsoft.AspNetCore.Components;

namespace WorkerAssistant.Client.Services
{
    public interface ILanguageService
    {
        string CurrentLanguage { get; }
        event Action OnLanguageChanged;
        void SetLanguage(string languageCode);
    }

    public class LanguageService : ILanguageService
    {
        public string CurrentLanguage { get; private set; } = "en";
        public event Action? OnLanguageChanged;

        public void SetLanguage(string languageCode)
        {
            if (languageCode != CurrentLanguage)
            {
                CurrentLanguage = languageCode;
                OnLanguageChanged?.Invoke();
            }
        }
    }
}
