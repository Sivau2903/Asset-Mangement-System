using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class RaiseRequestModel
    {
        public string AssetType { get; set; }
        public string MaterialCategory { get; set; }
        public string MaterialSubCategory { get; set; }
        public int RequestingQuantity { get; set; }
    }
}