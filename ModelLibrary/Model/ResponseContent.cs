using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelLibrary.Model
{
    public class ResponseContent
    {
        public string Answer { get; set; } = string.Empty;
        public string AnswerContext { get; set; } = string.Empty;
        public List<SearchResultReference>? References { get; set; }
    }
}
