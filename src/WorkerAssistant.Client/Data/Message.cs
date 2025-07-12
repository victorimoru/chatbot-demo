using System.Text.Json.Serialization;

namespace WorkerAssistant.Client.Data
{
    public class Message
    {
        public bool IsUser { get; set; }
        public string? Text { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public record ModelCacheStatus
    {
        // progress will be 1 if the model is fully downloaded
        [JsonPropertyName("progress")]
        public double Progress { get; init; }

        // text gives a human-readable status like "Ready" or "Fetching..."
        [JsonPropertyName("text")]
        public string StatusText { get; init; } = "";
    }
}
