using MathNet.Numerics.LinearAlgebra;
using Microsoft.JSInterop;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public class LLMInteropService(IJSRuntime jsRuntime) : IAsyncDisposable, ILLMInteropService
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", "./webllm-interop.js").AsTask());

        public async Task InitializeEngineAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("initializeEngine");
        }

        public async Task CompleteStreamAsync(
            List<ChatMessage> messages,
            Func<ChatCompletionChunk, Task> onChunkReceived,
            Func<Task> onStreamCompleted)
        {
            var module = await _moduleTask.Value;

            var helper = new ChatCompletionHelper(onChunkReceived, onStreamCompleted);
            var dotnetHelper = DotNetObjectReference.Create(helper);

            await module.InvokeVoidAsync("completeStream", messages, dotnetHelper);
        }

        public async Task ToggleElementDisabledAsync(string elementId, bool isDisabled)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("toggleElementDisabled", elementId, isDisabled);
        }
        public async Task HighlightCodeAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("highlightCode");
        }

        public async Task InitializeEmbeddingModelAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("initializeEmbeddingModel");
        }

        public async Task<Vector<float>> GetEmbeddingAsync(string text)
        {
            var module = await _moduleTask.Value;
            var embeddingArray = await module.InvokeAsync<float[]>("generateEmbedding", text);
            return Vector<float>.Build.DenseOfArray(embeddingArray);
        }
        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
