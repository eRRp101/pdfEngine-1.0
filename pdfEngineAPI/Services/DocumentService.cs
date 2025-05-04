using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.EntityFrameworkCore;
using ModelLibrary.Model;
using pdfEngineAPI.Data;

namespace pdfEngineAPI.Services
{
    public interface IDocumentService
    {
        Task<bool> DeleteAllDocumentsAsync();
        Task DeleteDocumentAsync(int docId);
        Task<List<FileMetaData>> GetDocumentsAsync();
        Task<List<DocumentChunk>> ProcessDocumentAsync(string filePath, int documentId);
        Task<FileMetaData> UploadDocumentAsync(IFormFile file);
    }

    public class DocumentService : IDocumentService
    {
        private readonly PdfDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(PdfDbContext context, IEmbeddingService embeddingService, ILogger<DocumentService> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<FileMetaData> UploadDocumentAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileGuid = Guid.NewGuid().ToString();
            var filePath = Path.Combine(uploadsDir, $"{fileGuid}{Path.GetExtension(file.FileName)}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var pages = ExtractTextWithPageNumbers(filePath);
            int totalPages = pages.Count;

            var metadata = new FileMetaData
            {
                FileGuid = fileGuid,
                FileName = file.FileName,
                FilePath = filePath,
                FileContent = new FileContent
                {
                    PageCount = totalPages,
                    UploadedAt = DateTime.UtcNow
                }
            };

            _context.FileMetaData.Add(metadata);
            await _context.SaveChangesAsync();

            var processedChunks = await ProcessDocumentAsync(filePath, metadata.Id);
            metadata.DocumentChunks.AddRange(processedChunks);

            // Generate and store summary
            metadata.FileContent.Summary = await _embeddingService.GenerateDocumentSummaryAsync(
                pages.Select(p => new DocumentPage(p.PageNumber, p.Text)).ToList());

            await _context.SaveChangesAsync();

            return metadata;
        }

        public async Task<List<DocumentChunk>> ProcessDocumentAsync(string filePath, int documentId)
        {
            var pages = ExtractTextWithPageNumbers(filePath);
            var chunks = new List<DocumentChunk>();
            var embeddings = new List<Embedding>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var (text, pageNumber) in pages)
            {
                foreach (var chunkText in SplitTextIntoChunks(text, 1000))
                {
                    var chunk = new DocumentChunk
                    {
                        PageNumber = pageNumber,
                        ChunkText = chunkText,
                        DocumentId = documentId
                    };
                    chunks.Add(chunk);
                }
            }

            await _context.DocumentChunks.AddRangeAsync(chunks);
            await _context.SaveChangesAsync();

            foreach (var chunk in chunks)
            {
                embeddings.Add(new Embedding
                {
                    Vector = await _embeddingService.GetEmbeddingAsync(chunk.ChunkText),
                    ChunkId = chunk.Id
                });
            }

            await _context.Embeddings.AddRangeAsync(embeddings);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return chunks;
        }

        private List<(string Text, int PageNumber)> ExtractTextWithPageNumbers(string filePath)
        {
            var result = new List<(string Text, int PageNumber)>();

            using (var reader = new PdfReader(filePath))
            using (var pdfDoc = new PdfDocument(reader))
            {
                for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(pageNum), strategy);
                    result.Add((pageText, pageNum));
                }
            }

            return result;
        }

        private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += maxChunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(maxChunkSize, text.Length - i)));
            }
            return chunks;
        }

        public async Task<List<FileMetaData>> GetDocumentsAsync()
        {
            return await _context.FileMetaData
                .Include(f => f.FileContent)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task DeleteDocumentAsync(int docId)
        {
            var document = await _context.FileMetaData
                .Include(f => f.DocumentChunks)
                .FirstOrDefaultAsync(f => f.Id == docId);

            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            _context.FileMetaData.Remove(document);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAllDocumentsAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var allDocuments = await _context.FileMetaData
                .Include(d => d.DocumentChunks)
                .ToListAsync();

            _context.DocumentChunks.RemoveRange(allDocuments.SelectMany(d => d.DocumentChunks));
            _context.FileMetaData.RemoveRange(allDocuments);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return allDocuments.Any();
        }
    }
}