using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class RequestViewModel
    {
        public int SNo { get; set; }
        public int RequestID { get; set; }
        public int EmpID { get; set; }
        public string AssetType { get; set; }
        public string MaterialCategory { get; set; }
        public string MSubCategory { get; set; }
        public int AvailableQuantity { get; set; }
        public int RequestingQuantity { get; set; }
        public int ApprovedQuantity { get; set; }
        public int IssuingQuantity { get; set; }
        public int PendingQuantity { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public int ClosingQuantity { get; set; }
        public int HODID { get; set; }

        public string Remarks { get; set; }

    }
}