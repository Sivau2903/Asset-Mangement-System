using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class MaterialItem
    {
        public string MaterialName { get; set; }
        public int QtyOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}