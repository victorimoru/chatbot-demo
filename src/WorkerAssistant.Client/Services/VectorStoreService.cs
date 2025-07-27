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

        public Task<List<DocumentChunk>> FindSimilarChunksNewAsync(Vector<float> queryEmbedding, string userQuery, int count = 3)
        {
            if (!IsIndexReady)
            {
                throw new InvalidOperationException("The vector index has not been built yet.");
            }

            var sw = Stopwatch.StartNew();

            // --- Pre-filtering Step ---
            var queryWords = userQuery.ToLower().Split([' '], StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            var keywordFilteredChunks = _vectorIndex
                .Where(chunk => chunk.Tags.Any(tag => queryWords.Contains(tag.ToLower())))
                .ToList();

            Console.WriteLine($"Keyword pre-filtering found {keywordFilteredChunks.Count} potential chunks.");

            // --- NEW: Hybrid Search Logic ---

            // This list will hold our final results.
            var finalChunks = new List<DocumentChunk>();
            finalChunks.AddRange(keywordFilteredChunks); // Add all keyword matches first.

            // Check if we have enough chunks already.
            if (finalChunks.Count < count)
            {
                Console.WriteLine("Keyword search didn't find enough chunks, performing broader semantic search...");

                // Create a list of chunks that were NOT found by the keyword search.
                var remainingChunks = _vectorIndex.Except(keywordFilteredChunks).ToList();

                // Calculate similarities ONLY for the remaining chunks.
                var semanticSearchResults = remainingChunks.Select(chunk => new
                {
                    Chunk = chunk,
                    Similarity = queryEmbedding.DotProduct(chunk.Embedding) / (queryEmbedding.L2Norm() * chunk.Embedding.L2Norm())
                })
                .OrderByDescending(s => s.Similarity)
                .Select(s => s.Chunk)
                .ToList();

                // How many more chunks do we need?
                int needed = count - finalChunks.Count;

                // Add the best from the semantic search until we have enough.
                finalChunks.AddRange(semanticSearchResults.Take(needed));
            }

            // Ensure we only return the desired number of chunks, just in case.
            var mostSimilarChunks = finalChunks.Distinct().Take(count).ToList();

            // --- End of Hybrid Search Logic ---

            sw.Stop();
            Console.WriteLine($"Hybrid search took {sw.ElapsedMilliseconds} ms and found {mostSimilarChunks.Count} chunks.");

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

        public async Task<(string, List<DocumentChunk>)> FindSimilarChunksNewRussianAsync(Vector<float> queryEmbedding, string userQuery, int count = 3)
        {
            if (!IsIndexReady)
            {
                throw new InvalidOperationException("The vector index has not been built yet.");
            }

            var sw = Stopwatch.StartNew();

            var queryWords = userQuery.ToLower()
                .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .Concat(userQuery.ToLower().Split([' '], StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(w => new[] { w, w + " " + w.Split(' ')[0] })) // Handle phrases
                .ToHashSet();

            var keywordFilteredChunks = _vectorIndex
                .Where(chunk => chunk.Tags.Any(tag => queryWords.Contains(tag.ToLower())) ||
                                chunk.Topic.ToLower().Split([' ']).Any(t => queryWords.Contains(t)) ||
                                chunk.Content.ToLower().Split([' ']).Any(t => queryWords.Contains(t)))
                .ToList();

            //Console.WriteLine($"Keyword pre-filtering found {keywordFilteredChunks.Count} potential chunks.");

            // --- Hybrid Search Logic ---
            var finalChunksWithScores = new List<(DocumentChunk Chunk, double Score)>();

            // Add keyword matches with a base score
            foreach (var chunk in keywordFilteredChunks)
            {
                double score = 0.7; // Base score for keyword match
                score += queryWords.Intersect(chunk.Tags.Select(t => t.ToLower())).Count() * 0.1; // Boost for tag matches
                finalChunksWithScores.Add((chunk, score));
            }

            // If not enough chunks, perform semantic search on remaining
            if (finalChunksWithScores.Count < count && _vectorIndex.Any())
            {
                Console.WriteLine("Keyword search didn't find enough chunks, performing semantic search...");

                var remainingChunks = _vectorIndex.Except(keywordFilteredChunks).ToList();
                var semanticSearchResults = remainingChunks.Select(chunk => (
                    Chunk: chunk,
                    Similarity: queryEmbedding.DotProduct(chunk.Embedding) / (queryEmbedding.L2Norm() * chunk.Embedding.L2Norm())
                )).Where(r => !double.IsNaN(r.Similarity) && !double.IsInfinity(r.Similarity)) // Validate similarity
                  .OrderByDescending(r => r.Similarity)
                  .Select(r => (r.Chunk, Score: r.Similarity * 0.6)) // Weight semantic score
                  .ToList();

                finalChunksWithScores.AddRange(semanticSearchResults.Take(count - finalChunksWithScores.Count));
            }

            // Sort by combined score and take top results
            var mostSimilarChunks = finalChunksWithScores
                .OrderByDescending(x => x.Score)
                .Select(x => x.Chunk)
                .Distinct()
                .Take(count)
                .ToList();

            var aggregatedResponse = string.Empty;
            // --- Optional: Aggregate Related Entries ---
            if (mostSimilarChunks.Count > 0)
            {
                aggregatedResponse = AggregateRelatedChunks(mostSimilarChunks, userQuery);
                //Console.WriteLine($"Aggregated response: {aggregatedResponse}");
            }

            sw.Stop();
            Console.WriteLine($"Hybrid search took {sw.ElapsedMilliseconds} ms and found {mostSimilarChunks.Count} chunks.");

            return await Task.FromResult((aggregatedResponse, mostSimilarChunks));
        }

        private static string AggregateRelatedChunks(List<DocumentChunk> chunks, string userQuery)
        {
            if (chunks == null || !chunks.Any()) return "Извините, у меня нет информации.";

            var groupedByTopic = chunks.GroupBy(c => c.Topic.ToLowerInvariant());
            var responseParts = new List<string>();
            userQuery = userQuery.ToLowerInvariant();

            foreach (var group in groupedByTopic)
            {
                string topic = group.Key;
                var contents = group.Select(c => c.Content).ToList();

                if (topic.Contains("оплата") && userQuery.Contains("платить"))
                {
                    responseParts.Add($"Вы несете ответственность за {string.Join(", ", contents.Select(c => c.Replace("Вы несете ответственность за ", "").Trim()))}.");
                }
                else if (topic.Contains("агенты") && userQuery.Contains("доверять"))
                {
                    responseParts.Add($"Доверяйте только {string.Join(" и ", contents.Select(c => c.Split('.').First().Trim()))}.");
                }
                else if (topic.Contains("виза") && userQuery.Contains("получить"))
                {
                    responseParts.Add($"Чтобы получить визу, {string.Join(" и ", contents.Select(c => c.Trim()))}.");
                }
                else if (topic.Contains("безопасность") && userQuery.Contains("безопасн"))
                {
                    responseParts.Add($"{string.Join(" ", contents.Select(c => c.Trim()))}.");
                }
                else if (topic.Contains("поддержка") && userQuery.Contains("проблем"))
                {
                    responseParts.Add($"{string.Join(" ", contents.Select(c => c.Trim()))}.");
                }
                else if (contents.Any())
                {
                    responseParts.Add(string.Join(" ", contents.Select(c => c.Trim())));
                }
            }

            return responseParts.Any() ? string.Join(" ", responseParts.Distinct()) : "Извините, у меня нет информации.";
        }
    }
}
