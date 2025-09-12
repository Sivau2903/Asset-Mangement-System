using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class CentralPurchaseOrderGroupedViewModel
    {
        public int PONumber { get; set; }

        public DateTime PODate { get; set; }
        public int? CopiesOfInvoice { get; set; }
        //public DateTime? OrderedDate { get; set; }
        //public string VendorName { get; set; }

        public List<CentralPurchaseOrderItem> CentralPurchaseOrderItems { get; set; }
    }
}