using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class AuditPOViewModel
    {
        public string UniversityName { get; set; }
        public string IUCDApprovalDoc { get; set; }
        public byte[] PODetails { get; set; }  // 🟢 Change from string to byte[]
        public string StoreUploads { get; set; }
        public string MRVDetails { get; set; }
        public string InvoiceDetails { get; set; }
        public string PONumber { get; set; }
        public string CommityApprovedDoc { get; set; }
        public string Statement { get; set; } // 🟢 Add this for the Statement
    }
}