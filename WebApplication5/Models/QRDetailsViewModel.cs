using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class QRDetailsViewModel
    {
        public string QRID { get; set; }  // Unique QR code ID
        public Employee Employee { get; set; }  // Employee details (assuming Employee is your entity)
        public string MaterialSubCategory { get; set; } // Main material category or subcategory name
        public int TotalIssuedQty { get; set; }  // Total quantity issued
        public List<VendorMaterialViewModel> Vendors { get; set; }  // Vendor-wise issued quantity details
    }

}