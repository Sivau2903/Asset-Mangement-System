using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class OrderUpdateModel
    {
        public int ID { get; set; } // ID is used to find the row
        public int OrderQuantity { get; set; } // Only this value is used
        public int IUCDApprovedQty { get; set; }
    }
}