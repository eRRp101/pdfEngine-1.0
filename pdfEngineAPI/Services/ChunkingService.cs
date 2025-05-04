using pdfEngineAPI.Services;

namespace pdfEngineAPI.Services
{
    public interface IChunkingService
    {
        List<(string Chunk, int PageNumber, string DocumentId)> ChunkText(List<(string Text, int PageNumber)> pages, string documentId);
    }

    public class ChunkingService : IChunkingService
    {
        private const int MaxChunkSize = 500;

        public List<(string Chunk, int PageNumber, string DocumentId)> ChunkText(List<(string Text, int PageNumber)> pages, string documentId)
        {
            var chunks = new List<(string Chunk, int PageNumber, string DocumentId)>();

            foreach (var page in pages)
            {
                string[] words = page.Text.Split(' ');
                int currentChunkSize = 0;
                List<string> currentChunkWords = new List<string>();

                foreach (var word in words)
                {
                    if (currentChunkSize + word.Length + 1 <= MaxChunkSize)
                    {
                        currentChunkWords.Add(word);
                        currentChunkSize += word.Length + 1;
                    }
                    else
                    {
                        chunks.Add((string.Join(" ", currentChunkWords), page.PageNumber, documentId));
                        currentChunkWords.Clear();
                        currentChunkSize = 0;
                    }
                }

                if (currentChunkWords.Any())
                {
                    chunks.Add((string.Join(" ", currentChunkWords), page.PageNumber, documentId));
                }
            }

            return chunks;
        }
    }
}
