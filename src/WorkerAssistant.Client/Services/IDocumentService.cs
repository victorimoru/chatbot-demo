using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public interface IDocumentService
    {
        Task<List<KnowledgeBaseEntry>> LoadDocumentChunksAsync(string documentUrl);
    }
}
