using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class HODDashboardViewModel
    {
        public List<RequestGroupedViewModel> Requests { get; set; }
        public List<HODRequestGroupedViewModel> HODRequests { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string RequestType { get; set; }

    }
}