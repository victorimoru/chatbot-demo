using MathNet.Numerics.LinearAlgebra;
using WorkerAssistant.Client.Data;
using ChatMessage = WorkerAssistant.Client.Data.ChatMessage;

namespace WorkerAssistant.Client.Services
{
    public interface ILLMInteropService
    {
        Task CompleteStreamAsync(List<ChatMessage> messages, Func<ChatCompletionChunk, Task> onChunkReceived, Func<Task> onStreamCompleted);
        ValueTask DisposeAsync();
        Task HighlightCodeAsync();
        Task InitializeEngineAsync(CancellationToken cancellationToken = default);
        Task ToggleElementDisabledAsync(string elementId, bool isDisabled);
        Task InitializeEmbeddingModelAsync();
        Task<Vector<float>> GetEmbeddingAsync(string text);
        Task<ModelCacheStatus?> CheckModelCacheStatusAsync(string modelId);
        Task InitializeSpeechRecognitionAsync(object dotnetHelper);
        Task StartSpeechRecognitionAsync(string langCode);
        Task StopSpeechRecognitionAsync();
    }
}
