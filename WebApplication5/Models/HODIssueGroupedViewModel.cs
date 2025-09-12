using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class HODIssueGroupedViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public List<HODIssueMaterial> Materials { get; set; }
    }
}