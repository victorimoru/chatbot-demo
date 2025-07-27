using MathNet.Numerics.LinearAlgebra;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public interface IVectorStoreService
    {
        bool IsIndexReady { get; }
        Task BuildIndexAsync(string documentUrl);
        Task<List<DocumentChunk>> FindSimilarChunksAsync(Vector<float> queryEmbedding, string userQuery, int count = 4);
        Task<List<DocumentChunk>> FindSimilarChunksAsync2(Vector<float> queryEmbedding, int count = 4);
        Task<List<DocumentChunk>> FindSimilarChunksNewAsync(Vector<float> queryEmbedding, string userQuery, int count = 3);
        Task<(string, List<DocumentChunk>)> FindSimilarChunksNewRussianAsync(Vector<float> queryEmbedding, string userQuery, int count = 3);
    }
}