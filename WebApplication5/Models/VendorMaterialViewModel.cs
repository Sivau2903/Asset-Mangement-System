using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class VendorMaterialViewModel
    {
        //public int VendorID { get; set; }
        public string VendorName { get; set; }
        public string vendorEmail { get; set; }
        public string VendorPhoneNumber { get; set; }
        public string MaterialName { get; set; }
        public string Unit { get; set; }
        public string Make { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ExpiryDate { get; set; }


        public int AvailableQuantity { get; set; }
        public int SelectedQuantity { get; set; } // For user input
    }
}