namespace ModelLibrary.Model
{
    public class DocumentPage
    {
        public int PageNumber { get; set; }
        public string Text { get; set; }
        public DocumentPage(int pageNumber, string text)
        {
            PageNumber = pageNumber;
            Text = text;
        }
    }
}
