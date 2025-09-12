using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class LocalAccountantController : BaseController
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        // GET: LocalAccountant
        public ActionResult LADashBoard()
        {
            return View();
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["UserID"] == null) // Check if session exists
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Login", action = "Loginpage" })
                );
            }
            base.OnActionExecuting(filterContext);
        }
        //public ActionResult PurchaseDepartmentRequests()
        //{
        //    int purchaseDepartmentID = Convert.ToInt32(Session["UserID"]);

        //    var raisedRequests = (from request in _db.LocalSentRequests
        //                          where request.AccountantID == purchaseDepartmentID
        //                          orderby request.OrderedDate descending
        //                          select request).ToList();

        //    return View(raisedRequests);
        //}

        public ActionResult AllPOs()
        {
            var loggedUserId = Session["UserID"]?.ToString();

            //var poNumbers = _db.PurchaseOrderItems
            //    .Where(p => p.AuditorID == loggedUserId)
            //    .Select(p => p.PONumber)
            //    .Distinct()
            //    .ToList();

            var groupedByUniversity = _db.PurchaseOrders
                .Where(po => po.Status == "Approved by Auditor")
                .GroupBy(po => po.UniversityName)
                .ToList();

            return View(groupedByUniversity);
        }

        public ActionResult LocalAllPOs()
        {
            var loggedUserId = Session["UserID"]?.ToString();

            //var poNumbers = _db.PurchaseOrderItems
            //    .Where(p => p.AuditorID == loggedUserId)
            //    .Select(p => p.PONumber)
            //    .Distinct()
            //    .ToList();

            var draftPOs = _db.PurchaseOrders
                       .Where(po => po.Status == "Draft")
                       .OrderByDescending(po => po.PODate)
                       .ToList();

            var deliveredPOs = _db.PurchaseOrders
                                  .Where(po => po.Status == "Delivered")
                                  .OrderByDescending(po => po.PODate)
                                  .ToList();

            ViewBag.DraftPOs = draftPOs;
            ViewBag.DeliveredPOs = deliveredPOs;


            return View();
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
                Statement = po.Statement, // 🟢 Add this for the
                CommityApprovedDoc = po.CommityApprovedDoc, // 🟢 Add this for the Commity Approved Document
                IUCDApprovalDoc = po.IUCDApprovalDoc,
                PODetails = po.PODetails,  // 🟢 Keep as byte[]
                StoreUploads = po.StoreUploads,
                MRVDetails = po.MRVDetails,
                InvoiceDetails = po.InvoiceDetails
            };

            return View(viewModel);
        }
        [HttpGet]
        public ActionResult LocalPODetails(string poNumber)
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
                //Statement = po.Statement, // 🟢 Add this for the
                CommityApprovedDoc = po.CommityApprovedDoc, // 🟢 Add this for the Commity Approved Document
                //IUCDApprovalDoc = po.IUCDApprovalDoc,
                PODetails = po.PODetails,  // 🟢 Keep as byte[]
                StoreUploads = po.StoreUploads,
                MRVDetails = po.MRVDetails,
                InvoiceDetails = po.InvoiceDetails
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
        public ActionResult InitiatePayment(string poNumber)
        {
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po != null)
            {
                po.Status = "Payment Initiated"; // Or whatever the status field is
                _db.SaveChanges();
                TempData["SuccessMessage"] = $"Payment initiated for PO Number: {poNumber}.";
            }
            else
            {
                TempData["SuccessMessage"] = $"PO Number {poNumber} not found.";
            }

            return RedirectToAction("PODetails", new { poNumber = poNumber });
        }

        //public ActionResult PODetails()
        //{
        //    if (TempData["SuccessMessage"] != null)
        //    {
        //        ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
        //    }

        //    return View(); // just the empty search form initially
        //}

        //[HttpPost]
        //public ActionResult PODetails(string poNumber)
        //{
        //    if (string.IsNullOrEmpty(poNumber))
        //    {
        //        ViewBag.Error = "Please enter a PO Number.";
        //        return View("PODetails");
        //    }

        //    var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
        //    if (po == null)
        //    {
        //        ViewBag.Error = $"No Purchase Order found with PO Number {poNumber}.";
        //        return View("PODetails");
        //    }

        //    var items = _db.PurchaseOrderItems.Where(i => i.PONumber.ToString() == poNumber).ToList();


        //    var viewModel = new GeneratePOViewModel
        //    {
        //        PONumber = po.PONumber.ToString(),
        //        PODate = (DateTime)po.PODate,
        //        UniversityName = po.UniversityName,
        //        UniversityAddress = po.UniversityAddress,
        //        UniversityPhone = po.UniversityPhone,
        //        UniversityEmail = po.UniversityEmail,
        //        RequisitionNo = po.RequisitionNo,
        //        ShipTo = po.ShipTo,
        //        RequisitionedBy = po.RequisitionedBy,
        //        WhenShip = po.WhenShip,
        //        ShipVia = po.ShipVia,
        //        FOBPoint = po.FOBPoint,
        //        Terms = po.Terms,
        //        CopiesOfInvoice = po.CopiesOfInvoice ?? 0,
        //        AuthorizedBy = po.AuthorizedBy,
        //        PurchaseOrderItems = items.Select(item => new PurchaseOrderItem
        //        {
        //            POItemID = item.POItemID,
        //            QtyOrdered = item.QtyOrdered ?? 0,
        //            QtyReceived = item.QtyReceived,
        //            Description = item.Description,
        //            UnitPrice = item.UnitPrice ?? 0,
        //            Total = (item.QtyOrdered ?? 0) * (item.UnitPrice ?? 0),
        //            Remarks = item.Remarks,
        //            VendorEmail = item.VendorEmail
        //        }).ToList()
        //    };
        //    return View(viewModel);
        //}

        //public ActionResult VendorSettlement()
        //{
        //    return View();
        //}
        public ActionResult UpdateBudget()
        {
            string userId = (string)Session["UserID"];
            var accountant = _db.LocalAccountants.FirstOrDefault(a => a.LocalAccountantID == userId);

            if (accountant == null)
            {
                return HttpNotFound("Local Accountant not found.");
            }

            return View(accountant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBudget(decimal AvailableBudget)
        {
            string userId = (string)Session["UserID"];
            var accountant = _db.LocalAccountants.FirstOrDefault(a => a.LocalAccountantID == userId);

            if (accountant == null)
            {
                return HttpNotFound("Local Accountant not found.");
            }

            accountant.Budget = AvailableBudget;
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Budget updated successfully.";
            return RedirectToAction("UpdateBudget");
        }


        //public ActionResult GenerateReport()
        //{
        //    return View();
        //}
    }
}