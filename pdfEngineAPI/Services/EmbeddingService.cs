using ModelLibrary.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
    Task<string> GenerateDocumentSummaryAsync(List<DocumentPage> pages);
}

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["OpenAI:ApiKey"];
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty.");
        }

        var requestBody = new
        {
            input = text,
            model = "text-embedding-ada-002"
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, options);
            if (embeddingResponse?.Data == null || embeddingResponse.Data.Length == 0)
            {
                throw new Exception("Embedding data is null or empty in the API response.");
            }

            return embeddingResponse.Data[0].Embedding;
        }
        else
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to generate embedding: {response.StatusCode}. Response: {errorResponse}");
        }
    }

    public async Task<string> GenerateDocumentSummaryAsync(List<DocumentPage> pages)
    {
        var pageSummaries = new List<PageSummary>();

        foreach (var page in pages)
        {
                var pageSummary = await GenerateSinglePageSummaryAsync(page.Text);
                pageSummaries.Add(new PageSummary
                {
                    PageNumber = page.PageNumber,
                    Text = pageSummary
                });
        }

        return await GenerateConsolidatedSummaryAsync(pageSummaries);
    }

    private async Task<string> GenerateSinglePageSummaryAsync(string pageText)
    {
            var prompt = $@"Generate a 1-2 sentence summary focusing on key concepts and results in this page:
                       TEXT: {TruncateText(pageText, 1500)}";

            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new {
                role = "user",
                content = prompt
            }},
                max_tokens = 80,
                temperature = 0.4
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                jsonContent);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"OpenAI Response: {responseBody}");

            using var jsonDoc = JsonDocument.Parse(responseBody);
            var content = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content?.Trim();
    }

    private async Task<string> GenerateConsolidatedSummaryAsync(List<PageSummary> pageSummaries)
    {
            var context = string.Join("\n---\n",
                pageSummaries.OrderBy(p => p.PageNumber)
                             .Select(p => $"PAGE {p.PageNumber}: {p.Text}"));

            var prompt = $@"Create a comprehensive document summary using these guidelines:

                STRUCTURE:
                Main Topic: 1 sentence identifying core subject
                Key Points: 2-3 sentences of most significant findings
                Conclusions: 1-2 sentences of outcomes/implications
                Application: 1 sentence of practical uses (if relevant)

                RULES:
                - Use exact technical terms from the source
                - Include specific numbers and names
                - Omit generic statements
                - Maximum 6 sentences total

                CONTENT:
                {context}";

            var request = new
            {
                model = "gpt-3.5-turbo-16k",
                messages = new[] { new {
                role = "system",
                content = "You are a technical documentation specialist."
            }, new {
                role = "user",
                content = prompt
            }},
                max_tokens = 300,
                temperature = 0.4
            };

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }), Encoding.UTF8, "application/json"));

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"Full API response: {responseBody}");

            using var jsonDoc = JsonDocument.Parse(responseBody);
            var content = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content?.Trim() ?? "[Consolidated summary unavailable]";
    }

    private string TruncateText(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "[...]";

    private class ChatCompletionResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public Choice[] Choices { get; set; }
        public Usage Usage { get; set; }
    }

    private class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; }
        public string FinishReason { get; set; }
    }

    private class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    private class EmbeddingResponse
    {
        public string Object { get; set; }
        public EmbeddingData[] Data { get; set; }
        public string Model { get; set; }
        public Usage Usage { get; set; }
    }

    private class EmbeddingData
    {
        public string Object { get; set; }
        public int Index { get; set; }
        public float[] Embedding { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}

public class PageSummary
{
    public int PageNumber { get; set; }
    public string Text { get; set; }
}