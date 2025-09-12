using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class GeneratePOViewModel
    {
        internal List<PurchaseOrderItem> purchaseOrderItems;

        public string PONumber { get; set; }
        public string VendorName { get; set; }
        public string VendorEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string RequisitionNo { get; set; }
        public string UniversityName { get; set; }
        public string UniversityPhone { get; set; }
        public string UniversityEmail { get; set; }
        public string UniversityAddress { get; set; }
        public DateTime PODate { get; set; }

        public string AuditorID { get; set; }
        public string ShipTo { get; set; }
        public string RequisitionedBy { get; set; }
        public string WhenShip { get; set; }
        public string ShipVia { get; set; }
        public string FOBPoint { get; set; }
        public string Terms { get; set; }
        public string AuthorizedBy { get; set; }
        public int? CopiesOfInvoice { get; set; }
        public string Status { get; set; }
        //public int InvoiceCopies { get; set; }

        public string CertificationFileName { get; set; }
        public HttpPostedFileBase MRVFile { get; set; }
        public string StoreUploads { get; set; }
        public string MRVDetails { get; set; }

        public string PurchaseDepartmentUploads { get; set; }

        public List<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        public HttpPostedFileBase CertificationFile { get; set; }

        //public static implicit operator GeneratePOViewModel(GeneratePOViewModel v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}