using MathNet.Numerics.LinearAlgebra;
using System.Diagnostics;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public class VectorStoreService(IDocumentService documentService, ILLMInteropService llmService) : IVectorStoreService
    {
        // This list will hold our entire indexed knowledge base in memory
        private readonly List<DocumentChunk> _vectorIndex = [];

        public bool IsIndexReady { get; private set; } = false;


        /// <summary>
        /// Builds the vector index by loading documents, generating embeddings in parallel,
        /// and normalizing the vectors for efficient similarity search.
        /// </summary>
        public async Task BuildIndexAsync(string documentUrl)
        {
            if (IsIndexReady)
            {
                Console.WriteLine("Vector index is already built.");
                return;
            }

            Console.WriteLine("Starting to build vector index...");
            await llmService.InitializeEmbeddingModelAsync();

            var entries = await documentService.LoadDocumentChunksAsync(documentUrl);
            Console.WriteLine($"Document loaded with {entries.Count} entries.");

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Console.WriteLine($"Generating embedding {i + 1}/{entries.Count}");

                var embedding = await llmService.GetEmbeddingAsync(entry.Text);

                var norm = (float)embedding.L2Norm();
                var normalizedEmbedding = embedding / norm;

                _vectorIndex.Add(new DocumentChunk(entry.Text, normalizedEmbedding, entry.Source, entry.Topic, entry.Tags));
            }

            IsIndexReady = _vectorIndex.Count > 0;
            Console.WriteLine("Vector index built successfully.");
        }

        /// <summary>
        /// Finds the most similar document chunks to a given query embedding using an efficient
        /// top-k search with a PriorityQueue.
        /// </summary>
        /// <param name="queryEmbedding">The vector representation of the query.</param>
        /// <param name="count">The number of similar chunks to return.</param>
        /// <returns>A list of the most similar DocumentChunks.</returns>
        public Task<List<DocumentChunk>> FindSimilarChunksAsync(Vector<float> queryEmbedding, string userQuery, int count = 4)
        {
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"Finding {count} most similar chunks... Using Priority Queues");
            if (!IsIndexReady)
            {
                throw new InvalidOperationException("The vector index has not been built yet.");
            }

            var queryWords = userQuery.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Find chunks that have at least one matching tag from the user's query
            var filteredIndex = _vectorIndex
                .Where(chunk => chunk.Tags.Any(tag => queryWords.Contains(tag.ToLower())))
                .ToList();

            // If no tags match, fall back to searching the entire index
            if (!filteredIndex.Any())
            {
                filteredIndex = _vectorIndex;
            }

            Console.WriteLine($"Pre-filtering reduced search space from {_vectorIndex.Count} to {filteredIndex.Count} chunks.");
            // --- End Pre-filtering ---

            var queryNorm = queryEmbedding.L2Norm();
            var normalizedQueryEmbedding = queryEmbedding / (float)queryNorm;

            // Use a bounded-size priority queue to efficiently track top-k most similar chunks
            var topChunks = new PriorityQueue<DocumentChunk, float>(count);

            foreach (var item in _vectorIndex)
            {
                float similarityScore = normalizedQueryEmbedding.DotProduct(item.Embedding);

                if (topChunks.Count < count)
                {
                    topChunks.Enqueue(item, similarityScore);
                }
                else
                {
                    // Safely check current lowest similarity threshold using TryPeek
                    if (topChunks.TryPeek(out _, out float minSimilarityInQueue) && similarityScore > minSimilarityInQueue)
                    {
                        topChunks.Dequeue();
                        topChunks.Enqueue(item, similarityScore);
                    }
                }
            }

            // Extract and sort final results by descending similarity
            var mostSimilarChunks = topChunks.UnorderedItems
                .OrderByDescending(x => x.Priority)
                .Select(x => x.Element)
                .ToList();

            Console.WriteLine($"Search took {sw.ElapsedMilliseconds} ms");
            sw.Stop();
            return Task.FromResult(mostSimilarChunks);
            
        }

        public async Task<List<DocumentChunk>> FindSimilarChunksAsync2(Vector<float> queryEmbedding, int count = 4)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"Finding {count} most similar chunks... Without using Priority Queues");
            if (!IsIndexReady)
            {
                throw new InvalidOperationException("The vector index has not been built yet.");
            }

            var similarities = _vectorIndex.Select(chunk => new
            {
                Chunk = chunk, // Keep the whole chunk object
                Similarity = queryEmbedding.DotProduct(chunk.Embedding) / (queryEmbedding.L2Norm() * chunk.Embedding.L2Norm())
            }).ToList();

            var mostSimilarChunks = similarities
                .OrderByDescending(s => s.Similarity)
                .Take(count)
                .Select(s => s.Chunk) // Select the full DocumentChunk
                .ToList();
            Console.WriteLine($"Search took {sw.ElapsedMilliseconds} ms");
            sw.Stop();
            return await Task.FromResult(mostSimilarChunks);
        }
    }
}
