using Microsoft.JSInterop;

namespace WorkerAssistant.Client.Services
{
    public class WebSpeechTtsService(IJSRuntime jsRuntime) : IWebSpeechTtsService
    {
        public async Task SpeakAsync(string text, string language = "en-US")
        {
            await jsRuntime.InvokeVoidAsync("speak", text, language);
        }

        public async Task<string[]> GetVoicesAsync()
        {
            return await jsRuntime.InvokeAsync<string[]>("getVoices");
        }
    }
}
