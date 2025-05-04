using Microsoft.AspNetCore.Components.Forms;
using PdfEngineBlzr.Model;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using ModelLibrary.Model;

namespace PdfEngineBlzr.Services
{
    interface IPdfApiService
    {
        Task<HttpResponseMessage> DeleteAllDocumentsAsync();
        Task<HttpResponseMessage> DeleteDocumentAsync(int id);
        Task<List<PdfData>> GetDocumentsAsync();
        Task<ResponseContent> QueryAsync(string query);
        Task<ResponseContent> ChatAsync(string input, string sessionId);
        Task<HttpResponseMessage> UploadDocumentAsync(IBrowserFile file);
        Task<bool> IsConnected();
    }

    class PdfApiService : IPdfApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ExceptionService _exceptionService;

        public PdfApiService(HttpClient httpClient, ExceptionService apiExceptionService)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7031/api/pdfengine/");
            _exceptionService = apiExceptionService;
        }

        public async Task<HttpResponseMessage> UploadDocumentAsync(IBrowserFile file)
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                using var content = new MultipartFormDataContent();
                var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB max size
                var streamContent = new StreamContent(fileStream);

                content.Add(streamContent, "file", file.Name);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                var response = await _httpClient.PostAsync("upload", content);

                return response;

            }, "Upload document to API");
        }

        public async Task<List<PdfData>> GetDocumentsAsync()
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                var pdfDataList = new List<PdfData>();
                var response = await _httpClient.GetAsync("documents");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                pdfDataList = JsonSerializer.Deserialize<List<PdfData>>(json) ?? new List<PdfData>();
                return pdfDataList;

            }, "Get documents from API");
        }


        public async Task<HttpResponseMessage> DeleteAllDocumentsAsync()
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                return await _httpClient.DeleteAsync("delete-all");

            }, "Delete all documents from API");
        }

        public async Task<HttpResponseMessage> DeleteDocumentAsync(int docId)
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                return await _httpClient.DeleteAsync($"delete/{docId}");

            }, "Delete document from API");
        }

        public async Task<ResponseContent> QueryAsync(string query)
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                var jsonString = $"\"{query}\"";
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Debug the actual request content
                var requestContent = await content.ReadAsStringAsync();

                var response = await _httpClient.PostAsync("query", content);
                response.EnsureSuccessStatusCode();

                var queryResponse = await response.Content.ReadAsStringAsync() ?? "API response is empty";



                var jsonDocument = JsonDocument.Parse(queryResponse);
                // Go inside the "answer" object
                var answerElement = jsonDocument.RootElement.GetProperty("answer");

                var answer = answerElement.GetProperty("answer").GetString() ?? "Answer is empty";
                var answerContext = answerElement.GetProperty("answerContext").GetString() ?? "AnswerContext is empty";

                return new ResponseContent() { Answer = answer, AnswerContext = answerContext };

            }, "Query documents from API");
        }

        public async Task<ResponseContent> ChatAsync(string userInput, string sessionId)
        {

            var response = await _httpClient.PostAsJsonAsync($"chat?sessionId={sessionId}", userInput);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ResponseContent>();
        }

        //HealthCheck
        public async Task<bool> IsConnected ()
        {
            return await _exceptionService.ExecuteHttpRequestAsync(async () =>
            {
                var response = await _httpClient.GetAsync("health");
                return response.IsSuccessStatusCode;

            }, "Couldn't establish connection to the API health endpoint.");
        }
    }
}
