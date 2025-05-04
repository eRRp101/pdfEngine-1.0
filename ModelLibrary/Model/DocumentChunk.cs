using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModelLibrary.Model
{
    public class DocumentChunk
    {
        [Key] 
        public int Id { get; set; } 
        [ForeignKey("FileMetaData")]
        public int DocumentId { get; set; } // Foreign key FileMetaData
        public int PageNumber { get; set; }
        public string ?ChunkText { get; set; } 
        public List<Embedding> ?Embeddings { get; set; } 
        public FileMetaData Document { get; set; }
    }
}
