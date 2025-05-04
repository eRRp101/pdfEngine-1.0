using ModelLibrary.Model;

namespace pdfEngineAPI.Services
{
    public class InMemoryChatHistoryService : IChatHistoryService
    {
        private readonly Dictionary<string, List<Message>> _sessions = new();

        public void AddMessage(string sessionId, Message message)
        {
            if (!_sessions.ContainsKey(sessionId))
                _sessions[sessionId] = new List<Message>();

            _sessions[sessionId].Add(message);
        }

        public List<Message> GetMessages(string sessionId)
        {
            return _sessions.ContainsKey(sessionId)
                ? _sessions[sessionId]
                : new List<Message>();
        }

        public void ClearHistory(string sessionId)
        {
            if (_sessions.ContainsKey(sessionId))
                _sessions.Remove(sessionId);
        }
    }
}
