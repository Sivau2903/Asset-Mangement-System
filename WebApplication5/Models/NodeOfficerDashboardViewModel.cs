using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class NodeOfficerDashboardViewModel
    {
        public int TotalAssets { get; set; }
        public int AssetsWithQRCode { get; set; }
        public int ExpiredAssets { get; set; }
    }

}