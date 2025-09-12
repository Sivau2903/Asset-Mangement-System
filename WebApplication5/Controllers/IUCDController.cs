using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class IUCDController : Controller
    {
        private readonly ASPEntities2 _db = new ASPEntities2();

        // GET: Auditor
        public ActionResult Home()
        {
            string userId = Session["UserID"]?.ToString();

            var alerts = _db.PurchaseOrders
                .Where(po => po.AuditorSentID == userId && po.Status.StartsWith("Sent Back"))
                .ToList();

            ViewBag.Alerts = alerts;

            return View();
        }

        [HttpPost]
        public ActionResult UploadCPDDocuments(string PONumber)
        {
            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == PONumber);
            if (po == null)
            {
                return HttpNotFound("Purchase Order not found.");
            }

            foreach (string fileKey in Request.Files)
            {
                var file = Request.Files[fileKey];
                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var uploadPath = Server.MapPath("~/UploadedDocs/");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, fileName);
                    file.SaveAs(filePath);
                    var virtualPath = "~/UploadedDocs/" + fileName;

                    // Store in correct field based on input name
                    if (fileKey == "CommityApprovedDoc")
                    {
                        po.IUCDApprovalDoc = virtualPath;
                    }

                    po.Status = "Uploaded";
                    //else if (fileKey == "InvoiceDetails")
                    //{
                    //    po.InvoiceDetails = virtualPath;
                    //}
                }
            }

            _db.SaveChanges();
            TempData["UploadSuccess"] = "Documents uploaded successfully.";
            return RedirectToAction("Home");
        }

        public ActionResult RequestsRecevied()
        {
            string purchaseDepartmentID = (string)Session["UserID"];

            var raisedRequests = (from request in _db.SavetoCentrals
                                  where request.IUCDID == purchaseDepartmentID &&
                                        (request.Status == "Sent to IUCD")
                                  orderby request.Material, request.RequestedDate descending
                                  select new CentralRequestViewModel
                                  {
                                      ID = request.ID,
                                      UniversityID = request.UniversityID,
                                      UniversityName = _db.Universities
                                                           .Where(u => u.UniversityId == request.UniversityID)
                                                           .Select(u => u.UniversityName)
                                                           .FirstOrDefault(),
                                      MaterialName = request.Material,
                                      RequestedDate = request.RequestedDate,
                                      OrderQuantity = request.Order_Quantity,
                                      
                                      PurchaseDepartmentUploads = request.PurchaseDepartmentUploads,
                                      CentralID = request.CentralID,
                                      Status = request.Status
                                  }).ToList();

            return View(raisedRequests);
        }





        [HttpPost]
        public ActionResult UpdateOrderQuantitiesWithDocument(string updatesJson)
        {
            if (string.IsNullOrEmpty(updatesJson))
                return Json(new { success = false, message = "No update data received." });

            var updates = JsonConvert.DeserializeObject<List<OrderUpdateModel>>(updatesJson);

            string purchaseDepartmentID = Session["UserID"]?.ToString();
            var files = Request.Files;

            // ✅ Get the upload folder path
            var uploadPath = Server.MapPath("~/UploadedDocs/");

            // ✅ Ensure the folder exists
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            foreach (var update in updates)
            {
                var request = _db.SavetoCentrals.FirstOrDefault(r => r.ID == update.ID);
                if (request != null)
                {
                    request.IUCDApprovedQty = update.IUCDApprovedQty;
                    request.Status = "Sent to Central";
                    request.CentralID = "IURCPD1";

                    // ✅ Only one file is expected for all rows
                    if (files.Count > 0)
                    {
                        var file = files[0]; // take the first uploaded file
                        if (file != null && file.ContentLength > 0)
                        {
                            // Generate a unique file name (optional but safer)
                            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var path = Path.Combine(uploadPath, fileName);

                            file.SaveAs(path);

                            // Save the virtual path (for link generation)
                            request.IUCDApprovalDoc = "~/UploadedDocs/" + fileName;

                            // Optional metadata
                            // request.IUCDDocUploadedBy = purchaseDepartmentID;
                            // request.IUCDDocUploadedDate = DateTime.Now;
                        }
                    }
                }
            }

            _db.SaveChanges();

            return Json(new { success = true, message = "Updated successfully." });
        }











        public JsonResult GetAvailableBudget()
        {
            string sessionID = (string)Session["UserID"]; // Adjust based on your session handling
            var budget = _db.IUCD_
                            .Where(lpd => lpd.ID.ToString() == sessionID)
                            .Select(lpd => lpd.Budget)
                            .FirstOrDefault();

            return Json(budget, JsonRequestBehavior.AllowGet);
        }

    }
}