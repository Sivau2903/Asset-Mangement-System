using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class BulkPORequest
    {
        public string VendorId { get; set; }
        public string Email { get; set; }
        public List<MaterialItem> Materials { get; set; }
    }
}