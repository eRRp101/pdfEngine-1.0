namespace ModelLibrary.Model
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _content = string.Empty;
        public string? Content
        {
            get => _content.Trim();
            set => _content = value ?? string.Empty;
        }

        public string AnswerContext { get; set; } = string.Empty;

        public bool IsUserMessage { get; set; }
        public DateTime TimeWritten { get; set; } = DateTime.UtcNow;
        public List<SearchResultReference>? References { get; set; }
    }
}
