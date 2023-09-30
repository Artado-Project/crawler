using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crawler
{
    internal class Result
    {
        public string? Title { get; set; }
        public string? URL { get; set; }
        public string? Description { get; set; }
        public string? Keywords { get; set; }
        public int? Rank { get; set; }
        public string? Lang { get; set; }
    }
}
