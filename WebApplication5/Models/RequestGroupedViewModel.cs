using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class RequestGroupedViewModel
    {
        public int SNo { get; set; }
        public int RequestID { get; set; }
        public String EmpID { get; set; }
        public int HODID { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public DateTime RequestDate { get; set; }
       
    

        [NotMapped]
        public int CurrentPage { get; set; }

        [NotMapped]
        public int PageSize { get; set; }

        [NotMapped]
        public int TotalCount { get; set; }
        public string FirstName { get; internal set; }
        [NotMapped]
        public List<Request> Requests { get; set; }
        public List<HODRequest> HODRequests { get; set; }
        public List<EmployeeIssueMaterial> EmployeeIssueMaterials { get; set; }
        public List<RequestViewModel> AssetDetails { get; set; }  // List of assets under this request

       
    }
}