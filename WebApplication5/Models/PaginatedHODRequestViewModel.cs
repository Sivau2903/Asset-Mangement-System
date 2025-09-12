using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class PaginatedHODRequestViewModel
    {
        public List<HODRequestGroupedViewModel> Requests { get; set; } = new List<HODRequestGroupedViewModel>();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

}