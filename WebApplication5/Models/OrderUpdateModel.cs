using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class OrderUpdateModel
    {
        public int ID { get; set; } 
        public int OrderQuantity { get; set; } 
        public int IUCDApprovedQty { get; set; }
    }
}