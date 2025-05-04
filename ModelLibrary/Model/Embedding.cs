using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModelLibrary.Model
{
    public class Embedding
    {
        [Key] 
        public string Id { get; set; } 
        public int ChunkId { get; set; } // Foreign key to the DocumentChunk
        public float[] ?Vector { get; set; }
        public DocumentChunk ?Chunk { get; set; }
    }
}
