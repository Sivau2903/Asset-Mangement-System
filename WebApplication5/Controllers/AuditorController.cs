using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class AuditorController : Controller
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        // GET: Summarizer
        public ActionResult Home()
        {
            return View();
        }

        public ActionResult AllPOs()
        {
            var loggedUserId = Session["UserID"]?.ToString();

            var poNumbers = _db.PurchaseOrderItems
                .Where(p => p.AuditorID == loggedUserId)
                .Select(p => p.PONumber)
                .Distinct()
                .ToList();

            var groupedByUniversity = _db.PurchaseOrders
                .Where(po => poNumbers.Contains(po.PONumber) && po.Status != "Approved by Auditor")
                .GroupBy(po => po.UniversityName)
                .ToList();

            return View(groupedByUniversity);
        }



        [HttpGet]
        public ActionResult PODetails(string poNumber)
        {
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po == null)
            {
                return HttpNotFound();
            }

            var viewModel = new AuditPOViewModel
            {
                PONumber = po.PONumber.ToString(), // Add this for routing
                UniversityName = po.UniversityName,
                IUCDApprovalDoc = po.IUCDApprovalDoc,
                PODetails = po.PODetails,  // 🟢 Keep as byte[]
                StoreUploads = po.StoreUploads,
                MRVDetails = po.MRVDetails,
                //InvoiceDetails = po.InvoiceDetails
            };

            return View(viewModel);
        }


        public ActionResult GetPOFile(string poNumber)
        {
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po == null || po.PODetails == null)
            {
                return HttpNotFound("PO file not found.");
            }

            return File(po.PODetails, "application/pdf", $"PO_{poNumber}.pdf");
        }

        [HttpPost]
        public ActionResult ApprovePO(string PONumber, string AccountantMessage)
        {
            // Get logged-in Auditor ID
            string auditorId = Session["UserID"]?.ToString();

            // Fetch the PO
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == PONumber);
            if (po != null)
            {
                po.Status = "Approved by Auditor";
                po.Statement = AccountantMessage;

                // Step 1: Get the university
                var university = _db.Universities
                    .FirstOrDefault(u => u.UniversityName.Trim().Equals(po.UniversityName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (university != null)
                {
                    // Step 2: Find Local Accountant for this UniversityID
                    var localAccountant = _db.LocalAccountants
                        .FirstOrDefault(la => la.UniversityID == university.UniversityId);

                    if (localAccountant != null)
                    {
                        po.AuditorSentID = localAccountant.LocalAccountantID;
                    }
                }

                // Optionally store who approved it
                //po.ApprovedBy = auditorId;

                _db.SaveChanges();
            }

            return RedirectToAction("AllPOs");
        }


        [HttpPost]
        public ActionResult SendBackPO(string PONumber, string Remarks, string SendBackTo)
        {
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == PONumber);
            if (po != null)
            {
                po.Status = $"Sent Back to {SendBackTo}";
                po.Remarks = Remarks;

                if (SendBackTo == "IUCD")
                {
                    po.AuditorSentID = "2025IUCD1";
                }
                else if (SendBackTo == "PurchaseDepartment")
                {
                    po.AuditorSentID = "IURCPD1";
                }
                else if (SendBackTo == "StoreDepartment")
                {
                    // Get UniversityID from UniversityName
                    var university = _db.Universities
                        .FirstOrDefault(u => u.UniversityName.Trim().Equals(po.UniversityName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (university != null)
                    {
                        // Find StoreAdmin for the university
                        var storeAdmin = _db.StoreAdmins
                            .FirstOrDefault(sa => sa.UniversityID == university.UniversityId);

                        if (storeAdmin != null)
                        {
                            po.AuditorSentID = storeAdmin.StoreAdminID;
                        }
                    }
                }

                _db.SaveChanges();
            }

            return RedirectToAction("AllPOs");
        }



        //[HttpPost]
        //public ActionResult InitiatePayment(CentralGeneratePOViewModel model)
        //{
        //    var userID = Session["UserID"]?.ToString();

        //    if (string.IsNullOrEmpty(userID))
        //    {
        //        TempData["ErrorMessage"] = "User session expired. Please login again.";
        //        return RedirectToAction("LoginPage", "Login");
        //    }

        //    var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == model.PONumber);

        //    if (po == null)
        //    {
        //        TempData["ErrorMessage"] = "Purchase Order not found.";
        //        return RedirectToAction("AllPOs");
        //    }

        //    po.Statement = model.Statement;
        //    po.Status = "Sent to Accountant";
        //    po.SummarizerID = userID;
        //    po.CentralAccountantID = "IURCACC1";

        //    _db.SaveChanges();

        //    TempData["SuccessMessage"] = "Purchase Order successfully sent to Central Accountant.";
        //    return RedirectToAction("AllPOs");
        //}
    }
}