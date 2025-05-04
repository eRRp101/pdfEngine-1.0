using System.Text.Json.Serialization;

namespace PdfEngineBlzr.Model
{
    public class PdfData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } 

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("fileContent")]
        public FileContent FileContent { get; set; } = new FileContent();
        public bool IsDeleting { get; set; }
        public PdfData() { }
    }
}
