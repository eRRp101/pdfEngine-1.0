using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelLibrary.Model
{
    public class SearchResultReference
    {
        public string DocumentName { get; set; }
        public int PageNumber { get; set; }
        public List<string> Strings { get; set; }
    }
}
