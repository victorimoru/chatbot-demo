using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;
using System.Text.Json.Serialization;

namespace WorkerAssistant.Client.Data
{
    public class Conversation
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Preview { get; set; }
        public DateTime LastMessageTime { get; set; }
        public ConversationStatus Status { get; set; }
        public List<Message> Messages { get; set; } = [];
        public List<ChatMessage> LLMResponses { get; set; } = [];
        public string? Icon { get; set; } 
        public string? LastMessagePreview { get; set; }
    }
    public enum ConversationStatus
    {
        Active,
        Pending,
        Closed
    }

    // Represents the 'delta' part of a streaming chunk
    public record ChatCompletionDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
    // Represents a single 'choice' in a streaming chunk
    public record ChatCompletionChoice
    {
        [JsonPropertyName("delta")]
        public ChatCompletionDelta Delta { get; init; } = default!;

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }
    }

    // Represents the entire chunk object received from JavaScript
    public record ChatCompletionChunk
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; init; } = [];
    }
    public record DocumentChunk(string Content, Vector<float> Embedding, string Source, string Topic, List<string> Tags);
    public record KnowledgeBaseEntry
    {
        [JsonPropertyName("source")]
        public string Source { get; init; } = "";

        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        [JsonPropertyName("topic")] 
        public string Topic { get; init; } = "";

        [JsonPropertyName("tags")] 
        public List<string> Tags { get; init; } = [];
    }
}
