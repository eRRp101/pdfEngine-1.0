using System.ComponentModel.DataAnnotations;

namespace ModelLibrary.Model
{
    public class FileContent
    {
        [Key]
        public int Id { get; set; }
        public int PageCount { get; set; }
        public string? Summary { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
       
    }
}
