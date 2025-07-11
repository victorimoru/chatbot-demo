using System.Text.Json.Serialization;

namespace WorkerAssistant.Client.Data
{
    public record ChatMessage(
       [property: JsonPropertyName("role")] string Role,
       [property: JsonPropertyName("content")] string Content,
       [property: JsonPropertyName("sources")] List<string> Sources,
       [property: JsonPropertyName("current-time")] DateTime TimeStamp
    );
}
