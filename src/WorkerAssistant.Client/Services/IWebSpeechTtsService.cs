namespace WorkerAssistant.Client.Services
{
    public interface IWebSpeechTtsService
    {
        Task<string[]> GetVoicesAsync();
        Task SpeakAsync(string text, string language = "en-US");
    }
}