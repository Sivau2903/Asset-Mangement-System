using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class IssueMaterialViewModel
    {
        public int RequestID { get; set; }
        public string EmpID { get; set; }
        public string HODID { get; set; }
        public string AssetType { get; set; }
        public string MaterialCategory { get; set; }
        public string MSubCategory { get; set; }
        public int? RequestingQuantity { get; set; }
        public int ApprovedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int IssuingQuantity { get; set; }
        public string IssuedBy { get; set; }
        public string QRID { get; set; }
        public string QRImageBase64 { get; set; }
        public string QRCodeBase64 { get; set; } // <-- Add this property to fix CS1061
        public List<VendorMaterialViewModel> Vendors { get; set; }
        public string EmployeeID { get; internal set; }
        public string MaterialID { get; internal set; }
        public string QRTargetUrl { get; internal set; }
    }
}