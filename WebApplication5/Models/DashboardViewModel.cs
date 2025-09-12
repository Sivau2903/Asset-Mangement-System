using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class DashboardViewModel
    {
        public List<HODRequestGroupedViewModel> Requests { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}