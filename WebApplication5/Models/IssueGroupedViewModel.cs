using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class IssueGroupedViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public List<Request> Materials { get; set; }
    }
}