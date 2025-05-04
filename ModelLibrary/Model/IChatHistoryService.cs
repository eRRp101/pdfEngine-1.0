using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelLibrary.Model
{
    public interface IChatHistoryService
    {
        void AddMessage(string sessionId, Message message);
        List<Message> GetMessages(string sessionId);
        void ClearHistory(string sessionId);
    }
}
