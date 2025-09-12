using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class PurchaseOrderItemViewModel
    {
        public int QtyOrdered { get; set; }
        public int QtyReceived { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public string VendorEmail { get; set; }
        public int POItemID { get; internal set; }
        public string Remarks { get; internal set; }

        public void CalculateTotal()
        {
            Total = (QtyOrdered ) * (UnitPrice);
        }
    }
}