using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelLibrary.Model;

namespace pdfEngineAPI.Services
{
    public interface IAnswerGenerationService
    {
        Task<ResponseContent> GenerateAnswerAsync(string query, string context);
        Task<ResponseContent> GenerateAnswerWithHistoryAsync(List<Message> history, string query, string context);
        Task<ResponseContent> GenerateGeneralAnswerWithHistoryAsync(List<Message> history, string query);

    }

    public class AnswerGenerationService : IAnswerGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnswerGenerationService> _logger;

        public AnswerGenerationService(HttpClient httpClient, IConfiguration configuration, ILogger<AnswerGenerationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<ResponseContent> GenerateAnswerAsync(string query, string context)
        {
            if (await IsGibberishAsync(query))
            {
                var gibberishContent = new ResponseContent
                {
                    Answer = "That seems like gibberish... Please try rephrasing your question.",
                    AnswerContext = string.Empty
                };
                return gibberishContent;
            }

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant providing concise and articulate answers based on the given context." +
                "Format your answers with a newline for better readability." },
                new { role = "user", content = $"Context:{context}, \n\nQuestion: {query}" }
            }
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var completionResponse = System.Text.Json.JsonSerializer.Deserialize<CompletionResponse>(responseBody);
                var responseAnswer = completionResponse.Choices?.FirstOrDefault()?.Message.Content ?? "No response generated";

                var responseContent = new ResponseContent
                {
                    Answer = responseAnswer,
                    AnswerContext = context
                };
                return responseContent;

            }
            else
            {
                throw new Exception($"Failed to generate answer: {response.StatusCode}");
            }
        }

        public async Task<ResponseContent> GenerateAnswerWithHistoryAsync(List<ModelLibrary.Model.Message> history, string query, string context)
        {
            var messages = new List<object>
    {
        new { role = "system", content = "You are a helpful assistant. Use context to answer concisely." }
    };

            messages.AddRange(history.Select(m => new
            {
                role = m.IsUserMessage ? "user" : "assistant",
                content = m.Content
            }));

            messages.Add(new { role = "user", content = $"Context:\n{context}\n\nQuestion:\n{query}" });

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages,
                max_tokens = 500,
                temperature = 0.5
            };

            var contentPayload = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", contentPayload);

            var responseBody = await response.Content.ReadAsStringAsync();
            var completion = JsonSerializer.Deserialize<CompletionResponse>(responseBody);
            var reply = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "[No response]";

            return new ResponseContent
            {
                Answer = reply,
                AnswerContext = context
            };
        }

        public async Task<ResponseContent> GenerateGeneralAnswerWithHistoryAsync(List<ModelLibrary.Model.Message> history, string query)
        {
            var messages = new List<object>
    {
        new { role = "system", content = "You are an intelligent and helpful assistant. You are not referencing any external documents, only your general knowledge and the current conversation." }
    };

            messages.AddRange(history.Select(m => new
            {
                role = m.IsUserMessage ? "user" : "assistant",
                content = m.Content
            }));

            messages.Add(new { role = "user", content = query });

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages,
                max_tokens = 500,
                temperature = 0.7
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"General chat response failed: {response.StatusCode}");
                return new ResponseContent
                {
                    Answer = "Sorry, I couldn’t generate a response. Try again later.",
                    AnswerContext = null
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var completion = JsonSerializer.Deserialize<CompletionResponse>(responseBody);
            var reply = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "[No response]";

            return new ResponseContent
            {
                Answer = reply,
                AnswerContext = "Response based on general knowledge and conversation history."
            };
        }

        public async Task<bool> IsGibberishAsync(string query)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
            new { role = "system", content = "You are a language expert. Detect if userinput is gibberish, random characters, or nonsensical. Only reply with 'Yes' or 'No'." },
            new { role = "user", content = $"Is this gibberish? \"{query}\"" }
        }
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Gibberish check failed: {response.StatusCode}");
                return false; // fallback: treat as not gibberish
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var completionResponse = System.Text.Json.JsonSerializer.Deserialize<CompletionResponse>(responseBody);
            var answer = completionResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim().ToLower();

            return answer != null && answer.StartsWith("yes");
        }



        //wrappers
        private class CompletionResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; }
        }

        private class Message
        {
            [JsonPropertyName("content")]

            public string Content { get; set; }
        }
    }
}