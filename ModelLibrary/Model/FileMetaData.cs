using System.ComponentModel.DataAnnotations;

namespace ModelLibrary.Model
{
    public class FileMetaData
    {
        [Key] 
        public int Id { get; set; }
        public string FileGuid { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public FileContent FileContent { get; set; } = new FileContent();
        public List<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();
        public FileMetaData() { }
    }

}