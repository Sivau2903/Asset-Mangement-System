using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class CentralRequestViewModel
    {
        public int ID { get; set; }
        public int UniversityID { get; set; }
        public string UniversityName { get; set; }
        public string MaterialName { get; set; }
        public DateTime RequestedDate { get; set; }
        public int OrderQuantity { get; set; }
        public int IUCDApprovedQty { get; set; }
        public string CentralID { get; set; }
        public string Status { get; set; }
        public string PurchaseDepartmentUploads { get; set; }
        public List<SavetoCentral> savetoCentrals { get; set; }
    }
}