using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class MaterialSummaryViewModel
    {
        public string MaterialName { get; set; }
        public int TotalIssuedQuantity { get; set; }
        public int ClosingQuantity { get; set; }
    }
}