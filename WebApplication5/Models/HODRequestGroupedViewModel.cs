using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class HODRequestGroupedViewModel
    {
        public int SNo { get; set; }
        public int HODRequestID { get; set; }
        public string HODID { get; set; }
        public string Status { get; set; }
        public DateTime RequestedDate { get; set; }

        [NotMapped]
        public int CurrentPage { get; set; }

        [NotMapped]
        public int PageSize { get; set; }

        [NotMapped]
        public int TotalCount { get; set; }
        public string FirstName { get; internal set; }
        [NotMapped]
        public List<Request> Requests { get; set; }
       
        public List<HODRequestViewModel> AssetDetails { get; set; }  // List of assets under this request
    }
}