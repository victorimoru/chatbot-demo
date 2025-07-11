using System.Net.Http.Json;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public class DocumentService(HttpClient httpClient) : IDocumentService
    {
        public async Task<List<KnowledgeBaseEntry>> LoadDocumentChunksAsync(string documentUrl)
        {
            try
            {
                var entries = await httpClient.GetFromJsonAsync<List<KnowledgeBaseEntry>>(documentUrl);
                return entries ?? [];
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading document: {ex.Message}");
                return [];
            }
        }
    }
}
