using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class QRDetailsViewModel
    {
        public string QRID { get; set; }  
        public Employee Employee { get; set; }  
        public string MaterialSubCategory { get; set; } 
        public int TotalIssuedQty { get; set; }  
        public List<VendorMaterialViewModel> Vendors { get; set; }  
        public HOD HOD { get; set; }

      
    }

}