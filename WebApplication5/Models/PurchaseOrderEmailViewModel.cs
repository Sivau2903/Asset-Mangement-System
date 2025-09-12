using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class PurchaseOrderEmailViewModel
    {
        public PurchaseOrder PO { get; set; }
        public List<PurchaseOrderItem> Items { get; set; }
    }
}