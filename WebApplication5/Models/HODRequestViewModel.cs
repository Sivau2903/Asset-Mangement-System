using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class HODRequestViewModel
    {
        public int SNo { get; set; }
        public int HODRequestID { get; set; }
        public string AssetType { get; set; }
        public string MaterialCategory { get; set; }
        public string MSubCategory { get; set; }
        public int AvailableQuantity { get; set; }
        public int RequestingQuantity { get; set; }
        public int IssuingQuantity { get; set; }
        public int PendingQuantity { get; set; }
        public string Status { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}