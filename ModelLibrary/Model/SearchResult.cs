namespace ModelLibrary.Model
{
    public class SearchResult
    {
        public Guid DocumentId { get; set; } 
        public string DocumentName { get; set; }       
        public int PageNumber { get; set; }           
        public string Chunk { get; set; }      
        public string [] Chunks { get; set; }
        public float Score { get; set; }     
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    }
}
