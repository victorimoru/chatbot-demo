using Microsoft.JSInterop;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public class ChatCompletionHelper(Func<ChatCompletionChunk, Task> onChunkReceived, Func<Task> onStreamCompleted)
    {
        [JSInvokable]
        public async Task ReceiveChunkCompletion(ChatCompletionChunk chunk)
        {
            if (onChunkReceived is not null)
            {
                await onChunkReceived(chunk);
            }
        }

        [JSInvokable]
        public async Task HandleStreamCompletion()
        {
            if (onStreamCompleted is not null)
            {
                await onStreamCompleted();
            }
        }

        [JSInvokable]
        public void HandleError(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
        }
    }
}
