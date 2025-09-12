using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class CentralAccountantController : BaseController
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        // GET: CentralAccountant 

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

        public ActionResult DashBoard()
        {
            return View();
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
            //var draftPOs = _db.CentralPurchaseOrders
            //                  .Where(po => po.Status == "Draft")
            //                  .OrderByDescending(po => po.PODate)
            //                  .ToList();

            var deliveredPOs = _db.CentralPurchaseOrders
                                  .Where(po => po.Status == "Sent to Accountant")
                                  .OrderByDescending(po => po.PODate)
                                  .ToList();

            //ViewBag.DraftPOs = draftPOs;
            ViewBag.DeliveredPOs = deliveredPOs;

            return View();
        }


        [HttpGet]
        public ActionResult PODetails(string poNumber)
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            }

            if (string.IsNullOrEmpty(poNumber))
            {
                return View(); // return empty if no PO number is passed
            }

            var po = _db.CentralPurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po == null)
            {
                ViewBag.Error = $"No Purchase Order found with PO Number {poNumber}.";
                return View();
            }

            var items = _db.CentralPurchaseOrderItems.Where(i => i.PONumber.ToString() == poNumber).ToList();

            var viewModel = new CentralGeneratePOViewModel
            {
                PONumber = po.PONumber.ToString(),
                PODate = (DateTime)po.PODate,
                CentralDepartmentName = po.CentralDepartmentName,
                CentralDepartmentAddress = po.CentralDepartmentAddress,
                CentralDepartmentPhone = po.CentralDepartmentPhone,
                CentralDepartmentEmail = po.CentralDepartmentEmail,
                RequisitionNo = po.RequisitionNo,
                ShipTo = po.ShipTo,
                RequisitionedBy = po.RequisitionedBy,
                WhenShip = po.WhenShip,
                ShipVia = po.ShipVia,
                FOBPoint = po.FOBPoint,
                Terms = po.Terms,
                CopiesOfInvoice = po.CopiesOfInvoice ?? 0,
                AuthorizedBy = po.AuthorizedBy,
                StoreUploads = po.StoreUploads,
                PurchaseDepartmentUploads = po.PurchaseDepartmentUploads,
                Statement = po.Statement,


                PurchaseOrderItems = items.Select(item => new PurchaseOrderItem
                {
                    POItemID = item.POItemID,
                    QtyOrdered = item.QtyOrdered ?? 0,
                    QtyReceived = item.QtyReceived,
                    Description = item.Description,
                    UnitPrice = item.UnitPrice ?? 0,
                    Total = (item.QtyOrdered ?? 0) * (item.UnitPrice ?? 0),
                    Remarks = item.Remarks,
                    VendorEmail = item.VendorEmail
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult InitiatePayment(string poNumber)
        {
            var po = _db.CentralPurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po != null)
            {
                po.Status = "Initiated"; // Or whatever the status field is
                _db.SaveChanges();
                TempData["SuccessMessage"] = $"Payment initiated for PO Number: {poNumber}.";
            }
            else
            {
                TempData["SuccessMessage"] = $"PO Number {poNumber} not found.";
            }

            return RedirectToAction("PODetails", new { poNumber = poNumber });
        }


        //[HttpPost]
        //public ActionResult PODetails(string poNumber)
        //{
        //    //if (string.IsNullOrEmpty(poNumber))
        //    //{
        //    //    ViewBag.Error = "Please enter a PO Number.";
        //    //    return View("PODetails");
        //    //}

        //    var po = _db.CentralPurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
        //    if (po == null)
        //    {
        //        ViewBag.Error = $"No Purchase Order found with PO Number {poNumber}.";
        //        return View("PODetails");
        //    }

        //    var items = _db.CentralPurchaseOrderItems.Where(i => i.PONumber.ToString() == poNumber).ToList();


        //    var viewModel = new CentralGeneratePOViewModel
        //    {
        //        PONumber = po.PONumber.ToString(),
        //        PODate = (DateTime)po.PODate,
        //        CentralDepartmentName = po.CentralDepartmentName,
        //        CentralDepartmentAddress = po.CentralDepartmentAddress,
        //        CentralDepartmentPhone = po.CentralDepartmentPhone,
        //        CentralDepartmentEmail = po.CentralDepartmentEmail,
        //        RequisitionNo = po.RequisitionNo,
        //        ShipTo = po.ShipTo,
        //        RequisitionedBy = po.RequisitionedBy,
        //        WhenShip = po.WhenShip,
        //        ShipVia = po.ShipVia,
        //        FOBPoint = po.FOBPoint,
        //        Terms = po.Terms,
        //        CopiesOfInvoice = po.CopiesOfInvoice ?? 0,
        //        AuthorizedBy = po.AuthorizedBy,
        //        CentralPurchaseOrderItems = items.Select(item => new CentralPurchaseOrderItem
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
            var accountant = _db.CentralAccountants.FirstOrDefault(a => a.CentralAccountantID == userId);

            if (accountant == null)
            {
                return HttpNotFound("Central Accountant not found.");
            }

            return View(accountant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBudget(decimal AvailableBudget)
        {
            string userId = (string)Session["UserID"];
            var accountant = _db.CentralAccountants.FirstOrDefault(a => a. CentralAccountantID == userId);

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