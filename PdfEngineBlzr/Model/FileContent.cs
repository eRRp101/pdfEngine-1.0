using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PdfEngineBlzr.Model
{
    public class FileContent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pageCount")]
        public int PageCount { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("uploadedAt")]
        public DateTime UploadedAt { get; set; } 

    }
}
