using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
//   


    public class VendorDetailViewModel
    {
        [Required]
        public string VendorName { get; set; }

        [Required, EmailAddress]
        public string EmailID { get; set; }

        [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "Enter valid 10-digit phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string GSTNO { get; set; }

        [Required]
        public string PanNumber { get; set; }

        [Required]
        public string Address { get; set; }
        public int UniversityID { get; set; }

        public List<MaterialSelection> Materials { get; set; } = new List<MaterialSelection>();
    }

    public class MaterialSelection
    {
        [Required]
        public int AssetTypeID { get; set; }

        [Required]
        public int MaterialCategoryID { get; set; }

        [Required]
        public int MaterialSubCategoryID { get; set; }

        [Required, Range(0, 100)]
        public string GSTPercentage { get; set; }

        [Required, Range(0.01, 999999.99)]
        public decimal PricePerUnit { get; set; }
    }


}