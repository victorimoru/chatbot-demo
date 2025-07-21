using System.Text.Json.Serialization;

namespace WorkerAssistant.Client.Data
{
    public class TranslationResponse
    {
        [JsonPropertyName("responseData")]
        public ResponseData ResponseData { get; set; }
    }

    public class ResponseData
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}
