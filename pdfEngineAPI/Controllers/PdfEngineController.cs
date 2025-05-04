using Microsoft.AspNetCore.Mvc;
using ModelLibrary.Model;
using pdfEngineAPI.Services;

namespace pdfEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfEngineController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ISearchService _searchService;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly IAnswerGenerationService _answerGen;
        private readonly IQueryClassifierService _queryClassifier;

        public PdfEngineController(
            IDocumentService documentService,
            ISearchService searchService,
            IChatHistoryService chatHistoryService,
            IAnswerGenerationService answerGen,
            IQueryClassifierService queryClassifier)
        {
            _documentService = documentService;
            _searchService = searchService;
            _chatHistoryService = chatHistoryService;
            _answerGen = answerGen;
            _queryClassifier = queryClassifier;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.ContentType != "application/pdf")
                return BadRequest("Invalid file type. Please upload a PDF file.");

            var fileMetaData = await _documentService.UploadDocumentAsync(file);
            return Ok(new { Message = "Document uploaded and indexed successfully.", FileName = fileMetaData.FileName, DocumentId = fileMetaData.FileGuid, Summary = fileMetaData.FileContent.Summary });
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            var documents = await _documentService.GetDocumentsAsync();
            return Ok(documents);
        }

        [HttpDelete("delete/{docId}")]
        public async Task<IActionResult> DeleteDocument(int docId)
        {
            await _documentService.DeleteDocumentAsync(docId);
            return Ok(new { Message = $"Document {docId} deleted successfully." });
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllDocuments()
        {
            var success = await _documentService.DeleteAllDocumentsAsync();
            if (success)
                return Ok(new { Message = "All documents deleted successfully." });
            else
                return BadRequest(new { Message = "Failed to delete all documents." });
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] string request)
        {
            if (string.IsNullOrWhiteSpace(request))
                return BadRequest("Query cannot be empty.");

            var searchResults = await _searchService.HybridSearchAsync(request);
            var strongResults = searchResults.Where(r => r.Score >= 0.7);
            var answer = await _searchService.GenerateFallbackAnswerAsync(request, searchResults);

            return Ok(new
            {
                Message = strongResults.Any() ? "Strong results" : "Weak results",
                Answer = answer,
                AnswerContext = answer.AnswerContext,
                References = strongResults.Select(r => new
                {
                    r.DocumentName,
                    r.PageNumber,
                    Strings = new[] { r.Chunk }
                })
            });
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromQuery] string sessionId, [FromBody] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty.");

            var userMessage = new Message
            {
                Content = query,
                IsUserMessage = true,
                TimeWritten = DateTime.UtcNow
            };

            _chatHistoryService.AddMessage(sessionId, userMessage);
            var history = _chatHistoryService.GetMessages(sessionId);

            // 
            var classification = await _queryClassifier.ClassifyQueryAsync(query);

            ResponseContent answer;
            List<SearchResult> resultList = new();
            IEnumerable<SearchResult> searchResults = Enumerable.Empty<SearchResult>();

            if (classification == "document-related")
            {
                resultList = (await _searchService.HybridSearchAsync(query)).ToList();
                searchResults = resultList;
                answer = await _searchService.GenerateFallbackAnswerWithHistoryAsync(query, resultList, history);
            }
            else if (classification == "hybrid")
            {
                resultList = (await _searchService.HybridSearchAsync(query)).ToList();
                searchResults = resultList;
                var docContext = string.Join("\n", resultList.Select(r => r.Chunk));
                answer = await _answerGen.GenerateAnswerWithHistoryAsync(history, query, docContext);
            }
            else // general
            {
                answer = await _answerGen.GenerateGeneralAnswerWithHistoryAsync(history, query);
            }

            var referencesList = classification != "general" && searchResults.Any()
                ? searchResults.Select(r => new SearchResultReference
                {
                    DocumentName = r.DocumentName,
                    PageNumber = r.PageNumber,
                    Strings = new List<string> { r.Chunk }
                }).ToList()
                : null;

            string formattedContext = referencesList != null
                ? string.Join("\n", referencesList.Select(r => $"{r.DocumentName} (Page {r.PageNumber}): {string.Join(" ", r.Strings)}"))
                : string.Empty;

            var assistantMessage = new Message
            {
                Content = answer.Answer,
                IsUserMessage = false,
                AnswerContext = formattedContext,
                TimeWritten = DateTime.UtcNow,
                References = referencesList
            };

            _chatHistoryService.AddMessage(sessionId, assistantMessage);

            return Ok(new ResponseContent
            {
                Answer = answer.Answer,
                AnswerContext = formattedContext,
                References = referencesList
            });
        }

            [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok("API is up!");
        }
    }
}
