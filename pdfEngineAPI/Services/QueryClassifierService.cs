using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModelLibrary;

namespace pdfEngineAPI.Services
{
    public interface IQueryClassifierService
    {
        Task<string> ClassifyQueryAsync(string query);
    }

    public class QueryClassifierService : IQueryClassifierService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QueryClassifierService> _logger;

        public QueryClassifierService(HttpClient httpClient, IConfiguration configuration, ILogger<QueryClassifierService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> ClassifyQueryAsync(string query)
        {
            var systemPrompt = "Classify the user query as one of the following: \"document-related\", \"general\", or \"hybrid\". Only respond with one of these exact terms.";
            var userPrompt = $"Query: {query}";

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.0
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Query classification failed: {response.StatusCode}");
                return "general"; // fallback to safe default
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(body);
            var answer = result.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString()
                               ?.Trim().ToLower();

            return answer switch
            {
                "document-related" or "general" or "hybrid" => answer,
                _ => "general" // fallback
            };
        }
    }
}
