namespace WorkerAssistant.Client.Data
{
    public class Message
    {
        public bool IsUser { get; set; }
        public string? Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
