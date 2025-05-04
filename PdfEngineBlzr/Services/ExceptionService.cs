using System.Text.Json;

namespace PdfEngineBlzr.Services
{
    public class ExceptionService
    {
        private readonly ILogger<ExceptionService> _logger;

        public ExceptionService(ILogger<ExceptionService> logger)
        {
            _logger = logger;
        }

        //ErrorContext/Message
        public void LogException(Exception ex, string context)
        {
            _logger.LogError(ex, $"An error occurred in {context}: {ex.Message}");
        }

        //CentralizedHttpExceptions (UI Messages)
        public async Task<T> ExecuteHttpRequestAsync<T>(Func<Task<T>> requestFunc, string context)
        {
            try
            {
                return await requestFunc();
            }
            catch (HttpRequestException ex)
            {
                LogException(ex, context);
                throw new ApiServiceException("Network error.\n Couldn't reach the API. Check your connection.", ex);
            }
            catch (JsonException ex)
            {
                LogException(ex, context);
                throw new ApiServiceException("Response format issue.\n API sent unexpected data.", ex);
            }
            catch (Exception ex)
            {
                LogException(ex, context);
                throw new ApiServiceException("An unexpected error occurred.\n Please try again later.", ex);
            }
        }
    }

    // Custom exception for API service
    public class ApiServiceException : Exception
    {
        public ApiServiceException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
