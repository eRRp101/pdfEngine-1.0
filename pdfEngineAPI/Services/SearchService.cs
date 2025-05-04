using FaissMask;
using Microsoft.EntityFrameworkCore;
using ModelLibrary.Model;
using pdfEngineAPI.Data;
using System.Linq;

namespace pdfEngineAPI.Services
{
    public interface ISearchService
    {
        Task<ResponseContent> GenerateFallbackAnswerAsync(string query, List<ModelLibrary.Model.SearchResult> searchResults);
        Task<ResponseContent> GenerateFallbackAnswerWithHistoryAsync(string query, List<ModelLibrary.Model.SearchResult> searchResults, List<Message> chatHistory);
        Task<List<ModelLibrary.Model.SearchResult>> HybridSearchAsync(string query);
        Task<ResponseContent> GenerateGeneralAnswerAsync(string query, List<Message> history);
    }

    public class SearchService : ISearchService, IDisposable
    {
        private readonly PdfDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly IAnswerGenerationService _answerGenerationService;
        private readonly ILogger<SearchService> _logger;
        private readonly IndexFlatL2 _index;
        private bool _disposed = false;

        public SearchService(
            PdfDbContext context,
            IEmbeddingService embeddingService,
            IAnswerGenerationService answerGenerationService,
            ILogger<SearchService> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _answerGenerationService = answerGenerationService;
            _logger = logger;
            _index = new IndexFlatL2(1536); 

            LoadFaissIndex();
        }

        private void LoadFaissIndex()
        {
            var allEmbeddings = _context.Embeddings.ToList();
            if (allEmbeddings.Any())
            {
                var vectors = allEmbeddings.Select(e => e.Vector).ToArray();
                _index.Add(vectors);
            }
        }

        public async Task<List<ModelLibrary.Model.SearchResult>> HybridSearchAsync(string query)
        {
            var semanticResults = await SemanticSearchAsync(query);

            // Dynamic FAISS confidence threshold
            var avgScore = semanticResults.Any() ? semanticResults.Average(r => r.Score) : 0;
            if (avgScore < 0.8 || semanticResults.Count < 3)
            {
                _logger.LogInformation("FAISS confidence low, adding Fuzzy Search.");
                var fuzzyResults = FuzzySearch(query);

                return semanticResults
                    .Concat(fuzzyResults)
                    .GroupBy(r => r.Chunk) 
                    .Select(g => g.First())
                    .OrderByDescending(r => r.Score)
                    .Take(3)
                    .ToList();
            }

            return semanticResults;
        }

        private async Task<List<ModelLibrary.Model.SearchResult>> SemanticSearchAsync(string query)
        {
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
            var results = new List<ModelLibrary.Model.SearchResult>();

            var allEmbeddings = await _context.Embeddings
                .Include(e => e.Chunk)
                .ThenInclude(chunk => chunk.Document)
                .AsNoTracking()
                .ToListAsync();

            if (!allEmbeddings.Any()) return results;

            var searchResults = _index.Search(queryEmbedding, 5).ToList();

            foreach (var result in searchResults)
            {
                if (result.Label >= 0 && result.Label < allEmbeddings.Count)
                {
                    var embedding = allEmbeddings[(int)result.Label];
                    var chunk = embedding.Chunk;
                    var document = chunk.Document;

                    results.Add(new ModelLibrary.Model.SearchResult
                    {
                        DocumentId = Guid.Parse(document.FileGuid),
                        DocumentName = document.FileName,
                        PageNumber = chunk.PageNumber,
                        Chunk = chunk.ChunkText,
                        Score = 1 - (float)result.Distance
                    });
                }
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        private List<ModelLibrary.Model.SearchResult> FuzzySearch(string query)
        {
            _logger.LogInformation("Performing Fuzzy Search...");

            var results = _context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => EF.Functions.Like(c.ChunkText, $"%{query}%")) 
                .Select(c => new ModelLibrary.Model.SearchResult
                {
                    DocumentId = Guid.Parse(c.Document.FileGuid),
                    DocumentName = c.Document.FileName,
                    PageNumber = c.PageNumber,
                    Chunk = c.ChunkText,
                    Score = 0.6F 
                })
                .OrderByDescending(r => r.Score)
                .Take(5)
                .ToList();

            return results;
        }

        public async Task<ResponseContent> GenerateFallbackAnswerAsync(string query, List<ModelLibrary.Model.SearchResult> searchResults)
        {
            try
            {
                // 1. Fetch all uploaded document names (distinct, ordered)
                var docNames = await _context.FileMetaData
                    .AsNoTracking()
                    .Select(d => d.FileName)
                    .OrderBy(name => name)
                    .ToListAsync();

                // 2. Create the document context string
                var docContext = "Available uploaded documents:\n" + string.Join("\n", docNames);

                // 3. Combine document names and relevant search chunks as context
                var context = docContext + "\n\nRelevant content:\n" + string.Join("\n\n",
                    searchResults.Select(r =>
                        $"{r.DocumentName} (Page {r.PageNumber}): {r.Chunk}"));

                // 4. Generate the answer using the combined context
                return await _answerGenerationService.GenerateAnswerAsync(query, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate fallback answer");

                return new ResponseContent() {
                    Answer = "Couldn't generate a proper answer. Please re-try different query.",
                    AnswerContext = null };
            }
        }

        public async Task<ResponseContent> GenerateFallbackAnswerWithHistoryAsync(string query, List<ModelLibrary.Model.SearchResult> searchResults, List<Message> chatHistory)
        {
            var docNames = await _context.FileMetaData
                .AsNoTracking()
                .Select(d => d.FileName)
                .OrderBy(name => name)
                .ToListAsync();

            var docContext = "Available uploaded documents:\n" + string.Join("\n", docNames);

            var contentContext = string.Join("\n\n", searchResults.Select(r =>
                $"{r.DocumentName} (Page {r.PageNumber}): {r.Chunk}"));

            var fullContext = docContext + "\n\nRelevant content:\n" + contentContext;

            return await _answerGenerationService.GenerateAnswerWithHistoryAsync(chatHistory, query, fullContext);
        }

        public Task<ResponseContent> GenerateGeneralAnswerAsync(string query, List<Message> history)
        {
            return _answerGenerationService.GenerateGeneralAnswerWithHistoryAsync(history, query);
        }

        //faissIndex IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _index?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
