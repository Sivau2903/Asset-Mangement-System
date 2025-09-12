using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Controls;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    
    public class HomeController : BaseController
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        private List<HODRequestGroupedViewModel> pagedrequests;

        public ActionResult StoreAdminDasBoard(DateTime? fromDate, DateTime? toDate, string requestType = "Employee", int page = 1, int pageSize = 10)
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
                    if (fileKey == "MRVDetails")
                    {
                        po.MRVDetails = virtualPath;
                    }
                    else if (fileKey == "StoreUploads")
                    {
                        po.StoreUploads = virtualPath;
                    }

                    po.Status = "Uploaded";
                }
            }

            _db.SaveChanges();
            TempData["UploadSuccess"] = "Documents uploaded successfully.";
            return RedirectToAction("Home");
        }

        public ActionResult EmployeeRequests(DateTime? fromDate, DateTime? toDate, string requestType = "Employee", int page = 1, int pageSize = 10)
        {

            // Validate session
            string userID = Session["UserID"] as string;
            string userRole = Session["UserRole"] as string;

            System.Diagnostics.Debug.WriteLine($"Session ID: {userID}, Role: {userRole}");

            if (string.IsNullOrEmpty(userID) || userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                // Return to some safe page (avoid infinite loop)
                return RedirectToAction("StoreAdminDasBoard", "Home");
            }

            // Find the StoreAdmin
            var storeadmin = _db.StoreAdmins.FirstOrDefault(e => e.StoreAdminID.ToString() == userID);
            if (storeadmin == null)
            {
                System.Diagnostics.Debug.WriteLine("Error: StoreAdmin not found in the database for the given email.");
                TempData["ErrorMessage"] = "StoreAdmin details not found.";
                return RedirectToAction("StoreAdminDasBoard", "Home");
            }

            string storeadminID = storeadmin.StoreAdminID;
            System.Diagnostics.Debug.WriteLine($"StoreAdminID Retrieved: {storeadminID}");



            // Base query: Approved or Ongoing requests for this StoreAdmin
            var query = _db.Requests
                .Where(r => r.StoreAdminID == storeadminID
                         && (r.Status == "Approved" || r.Status == "Ongoing"))
                .OrderByDescending(r => r.RequestDate)
                .ToList()
                .GroupBy(r => r.RequestID)
                .Select((group, index) => new RequestGroupedViewModel
                {
                    SNo = index + 1,
                    RequestID = group.Key ?? 0,
                    EmpID = group.First().EmpID,
                    RequestDate = group.First().RequestDate ?? DateTime.Now,
                    Status = group.First().Status ?? "Unknown",
                    AssetDetails = group.Select(r => new RequestViewModel
                    {
                        AssetType = r.AssetType ?? "N/A",
                        MaterialCategory = r.MaterialCategory ?? "N/A",
                        MSubCategory = r.MSubCategory ?? "N/A",
                        AvailableQuantity = r.AvailableQuantity ?? 0,
                        RequestingQuantity = r.RequestingQuantity,
                        ApprovedQuantity = r.ApprovedQuantity ?? 0,
                        //IssuingQuantity = r.IssuingQuantity ?? 0,
                        PendingQuantity = r.PendingQuantity ?? 0
                    }).ToList()
                })
                .ToList();

            // Apply filters
            //if (fromDate.HasValue)
            //{
            //    query = query.Where(r => r.RequestDate >= fromDate.Value).ToList();
            //}
            //if (toDate.HasValue)
            //{
            //    query = query.Where(r => r.RequestDate <= toDate.Value).ToList();
            //}
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.RequestDate <= inclusiveToDate).ToList();
            }

            //if (fromDate.HasValue)
            //{
            //    query = query.Where(r => r.RequestDate >= fromDate.Value.Date);
            //}

            //if (toDate.HasValue)
            //{
            //    DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            //    query = query.Where(r => r.RequestDate <= inclusiveToDate);
            //}


            // Pagination
            int totalRequests = query.Count();
            var pagedRequests = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Create the wrapper ViewModel
            var model = new HODDashboardViewModel
            {
                Requests = pagedRequests,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalRequests
            };

            return View(model);

        }


        public ActionResult IssueForm(int requestId, string msubCategory)
        {
            Console.WriteLine($"Received IssueForm Call: {requestId}, {msubCategory}");

            if (!Request.IsAjaxRequest())
                return Json(new { success = false, message = "Request is not AJAX." });

            string userID = Session["UserID"] as string;
            if (string.IsNullOrEmpty(userID))
                return Json(new { success = false, message = "User session expired. Please log in again." });

            var storeAdmin = _db.StoreAdmins.FirstOrDefault(s => s.StoreAdminID.ToString() == userID);
            if (storeAdmin == null)
                return Json(new { success = false, message = "StoreAdmin not found." });

            var request = _db.Requests.FirstOrDefault(r => r.RequestID == requestId && r.MSubCategory == msubCategory);
            if (request == null)
                return Json(new { success = false, message = "Request not found." });

            int issuingQuantity = request.IssuingQuantity ?? 0;
            int availableQuantity = request.AvailableQuantity ?? 0;

            var issuingModel = new IssueMaterialViewModel
            {
                RequestID = request.RequestID ?? 0,
                EmpID = request.EmpID,
                HODID = request.HODID,
                AssetType = request.AssetType,
                MaterialCategory = request.MaterialCategory,
                MSubCategory = request.MSubCategory,
                RequestingQuantity = request.RequestingQuantity,
                ApprovedQuantity = request.ApprovedQuantity ?? 0,
                AvailableQuantity = availableQuantity,
                IssuingQuantity = issuingQuantity,
                IssuedBy = storeAdmin.StoreAdminID.ToString()
            };

            // ✅ Fetch vendors who have this material
            var vendors = _db.MaterialAvailables
                .Where(m => m.MaterialName == msubCategory && m.AvailableQty > 0)
                .Select(m => new VendorMaterialViewModel
                {
                    //VendorID = m.VendorID,
                    VendorName = m.VendorName,
                    vendorEmail = m.VendorEmail,
                    VendorPhoneNumber = m.VendorPhoneNumber,
                    MaterialName = m.MaterialName,
                    AvailableQuantity = (int)m.AvailableQty,
                    ExpiryDate = (DateTime)m.ExpiryDate,
                    Make = m.Make,
                    Unit = m.Unit
                }).ToList();

            issuingModel.Vendors = vendors;

            return PartialView("_IssueForm", issuingModel);
        }


      

        [HttpPost]
        public JsonResult IssueMaterial(IssueMaterialViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("===== POST: IssueMaterial HIT =====");

            try
            {
                // Deserialize vendors from JSON
                if (!string.IsNullOrEmpty(Request["VendorsJson"]))
                {
                    model.Vendors = Newtonsoft.Json.JsonConvert
                        .DeserializeObject<List<VendorMaterialViewModel>>(Request["VendorsJson"]);
                }

                var request = _db.Requests.FirstOrDefault(m => m.RequestID == model.RequestID &&
                                                               m.MSubCategory == model.MSubCategory);
                if (request == null)
                {
                    return Json(new { success = false, message = "Request not found." });
                }

                // ✅ Calculate quantities
                int approvedQty = request.ApprovedQuantity ?? 0;
                int previousIssuedQty = request.IssuingQuantity ?? 0;
                int newIssueInput = model.Vendors.Sum(v => v.SelectedQuantity);
                int totalIssuingQty = previousIssuedQty + newIssueInput;

                if (totalIssuingQty > approvedQty)
                {
                    return Json(new { success = false, message = $"Total issuing quantity ({totalIssuingQty}) exceeds approved quantity ({approvedQty})." });
                }

                if (newIssueInput > (request.AvailableQuantity ?? 0))
                {
                    return Json(new { success = false, message = $"New issuing quantity ({newIssueInput}) exceeds available stock ({request.AvailableQuantity})." });
                }

                int closingQty = (request.AvailableQuantity ?? 0) - newIssueInput;

                // ✅ Update Request Table
                request.IssuingQuantity = totalIssuingQty;
                request.ClosingQuantity = closingQty;
                request.IssuedDate = DateTime.Now;
                request.IssuedBy = model.IssuedBy;
                request.Status = (request.IssuingQuantity == request.RequestingQuantity) ? "Issued" : "Ongoing";
                //request.AvailableQuantity = closingQty;

                _db.Entry(request).State = System.Data.Entity.EntityState.Modified;

                // ✅ Save QR Details
                string qrID = "ISSUE" + new Random().Next(10000, 99999);
                var issuedData = new IssuedQRDetail
                {
                    QRID = qrID,
                    RequestID = model.RequestID,
                    EmployeeID = model.EmpID,
                    MaterialSubCategory = model.MSubCategory,
                    TotalIssuedQty = newIssueInput,
                    VendorDetails = Newtonsoft.Json.JsonConvert.SerializeObject(model.Vendors),
                    CreatedDate = DateTime.Now
                };
                _db.IssuedQRDetails.Add(issuedData);

                // ✅ Update MaterialMasterList
                var stock = _db.MaterialMasterLists.FirstOrDefault(s => s.MaterialSubCategory == model.MSubCategory);
                if (stock != null)
                {
                    stock.AvailableQuantity = closingQty;
                    stock.MaterialUpdatedDate = DateTime.Now;
                    stock.UpdatedBy = model.IssuedBy;
                    _db.Entry(stock).State = System.Data.Entity.EntityState.Modified;
                }

                // ✅ Update Available Quantity vendor-wise
                foreach (var vendor in model.Vendors)
                {
                    var materialAvailable = _db.MaterialAvailables
                        .FirstOrDefault(ma => ma.MaterialName == model.MSubCategory &&
                                               ma.VendorName == vendor.VendorName);

                    if (materialAvailable != null)
                    {
                        materialAvailable.AvailableQty -= vendor.SelectedQuantity;
                        if (materialAvailable.AvailableQty < 0)
                            materialAvailable.AvailableQty = 0;

                        _db.Entry(materialAvailable).State = System.Data.Entity.EntityState.Modified;
                    }
                }

                _db.SaveChanges(); // Save all changes together

                // ✅ Generate QR
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                string longUrl = baseUrl + Url.Action("ViewQRDetails", "Home", new { qrId = qrID });
                string shortUrl = longUrl;

                string qrBase64;
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(shortUrl, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    using (Bitmap bitmap = qrCode.GetGraphic(20))
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        qrBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Material issued successfully!",
                    qrID = qrID,
                    qrImage = qrBase64,
                    qrUrl = shortUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private string GetPublicBaseUrl()
        {
            return Request.Url.GetLeftPart(UriPartial.Authority);
        }

        private string ShortenUrl(string longUrl)
        {
            // ✅ For now, just return long URL
            return longUrl;
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult ViewQRDetails(string qrId)
        {
            System.Diagnostics.Debug.WriteLine($"[Debug] ViewQRDetails method hit: {qrId}");

            var qrData = _db.IssuedQRDetails.FirstOrDefault(x => x.QRID == qrId);
            if (qrData == null) return HttpNotFound();

            var vendorList = Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<VendorMaterialViewModel>>(qrData.VendorDetails);

            var model = new QRDetailsViewModel
            {
                QRID = qrData.QRID,
                Employee = _db.Employees.FirstOrDefault(e => e.EmpID == qrData.EmployeeID),
                MaterialSubCategory = qrData.MaterialSubCategory,
                TotalIssuedQty = (int)qrData.TotalIssuedQty,
                Vendors = vendorList
            };

            return View(model);
        }


        public ActionResult IssuedRequest(DateTime? fromDate, DateTime? toDate)
        {

            var userID = Session["UserID"] as string;
            var userRole = Session["UserRole"] as string;

            if (string.IsNullOrEmpty(userID) || userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("StoreAdminDasBoard");
            }

            var storeAdmin = _db.StoreAdmins.FirstOrDefault(e => e.StoreAdminID.ToString() == userID);
            if (storeAdmin == null)
            {
                TempData["ErrorMessage"] = "StoreAdmin details not found.";
                return RedirectToAction("StoreAdminDasBoard");
            }

            // Step 1: Fetch issued materials
            var issuedRequests = _db.Requests
                .Where(r => r.IssuedBy == storeAdmin.StoreAdminID.ToString() && r.Status == "Issued")
                .OrderByDescending(r => r.IssuedDate)
                .ToList();

            // Step 2: Group by RequestID
            var groupedData = issuedRequests
                .GroupBy(r => r.RequestID)
                .Select(g => new IssueGroupedViewModel
                {
                    RequestID = (int)g.Key,
                    RequestDate = (DateTime)g.First().IssuedDate,
                    Materials = g.ToList() // All materials under this RequestID
                })
                .ToList();

            if (fromDate.HasValue)
            {
                groupedData = groupedData.Where(r => r.RequestDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                groupedData = groupedData.Where(r => r.RequestDate <= inclusiveToDate).ToList();
            }

            return View(groupedData);
        }


        public ActionResult HODRequests(DateTime? fromDate, DateTime? toDate, string requestType = "Employee", int page = 1, int pageSize = 10)
        {
            // Validate session
            string userID = Session["UserID"] as string;
            string userRole = Session["UserRole"] as string;

            System.Diagnostics.Debug.WriteLine($"Session ID: {userID}, Role: {userRole}");

            if (string.IsNullOrEmpty(userID) || userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                // Return to some safe page (avoid infinite loop)
                return RedirectToAction("StoreAdminDasBoard", "Home");
            }

            // Find the StoreAdmin
            var storeadmin = _db.StoreAdmins.FirstOrDefault(e => e.StoreAdminID.ToString() == userID);
            if (storeadmin == null)
            {
                System.Diagnostics.Debug.WriteLine("Error: StoreAdmin not found in the database for the given email.");
                TempData["ErrorMessage"] = "StoreAdmin details not found.";
                return RedirectToAction("StoreAdminDasBoard", "Home");
            }

            string storeadminID = storeadmin.StoreAdminID;
            System.Diagnostics.Debug.WriteLine($"StoreAdminID Retrieved: {storeadminID}");



            // Base query: Approved or Ongoing requests for this StoreAdmin
            var query = _db.HODRequests
                .Where(r => r.StoreAdminID == storeadminID
                         && (r.Status == "New" || r.Status == "Ongoing"))
                .OrderByDescending(r => r.RequestedDate)
                .ToList()
                .GroupBy(r => r.HODRequestID)
                .Select((group, index) => new HODRequestGroupedViewModel
                {
                    SNo = index + 1,
                    HODRequestID = group.Key ?? 0,
                    HODID = group.First().HODID,
                    RequestedDate = group.First().RequestedDate ?? DateTime.Now,
                    Status = group.First().Status ?? "Unknown",
                    AssetDetails = group.Select(r => new HODRequestViewModel
                    {
                        AssetType = r.AssetType ?? "N/A",
                        MaterialCategory = r.MaterialCategory ?? "N/A",
                        MSubCategory = r.MSubCategory ?? "N/A",
                        AvailableQuantity = r.AvailableQuantity ?? 0,
                        RequestingQuantity = r.RequestingQuantity,
                        //ApprovedQuantity = r.ApprovedQuantity ?? 0,
                        //IssuingQuantity = r.IssuingQuantity ?? 0,
                        //PendingQuantity = r.PendingQuantity ?? 0
                    }).ToList()
                })
                .ToList();

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestedDate >= fromDate.Value).ToList();
            }
            if (toDate.HasValue)
            {
                query = query.Where(r => r.RequestedDate <= toDate.Value).ToList();
            }

            // Pagination
            int totalRequests = query.Count();
            var pagedRequests = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Create the wrapper ViewModel
            var model = new HODDashboardViewModel
            {
                HODRequests = pagedRequests,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalRequests
            };

            return View(model);

        }

        public ActionResult HODIssueForm(int hodrequestId, string msubCategory)
        {
            Console.WriteLine($"Received IssueForm Call: {hodrequestId}, {msubCategory}");

            // ✅ 1. Ensure the request is an AJAX request
            if (!Request.IsAjaxRequest())
            {
                return Json(new { success = false, message = "Request is not AJAX." });
            }

            // ✅ 2. Identify the currently logged-in StoreAdmin
            string userID = Session["UserID"] as string;
            if (string.IsNullOrEmpty(userID))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] User session is empty. Cannot find StoreAdmin.");
                return Json(new { success = false, message = "User session expired. Please log in again." });
            }

            var storeAdmin = _db.StoreAdmins.FirstOrDefault(s => s.StoreAdminID.ToString() == userID);
            if (storeAdmin == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] StoreAdmin not found in database.");
                return Json(new { success = false, message = "StoreAdmin not found." });
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Found StoreAdmin => ID={storeAdmin.StoreAdminID}");

            // ✅ 3. Retrieve the request from the database
            var request = _db.HODRequests.FirstOrDefault(r => r.HODRequestID == hodrequestId && r.MSubCategory == msubCategory);
            if (request == null)
            {
                return Json(new { success = false, message = "Request not found." });
            }

            // Fetching IssuingQuantity from DB
            int issuingQuantity = request.IssuingQuantity ?? 0;
            int availableQuantity = request.AvailableQuantity ?? 0;

            var issuingModel = new HODIssueMaterial
            {
                RequestID = request.HODRequestID ?? 0,
                HODID = request.HODID,
                AssetType = request.AssetType,
                MaterialCategory = request.MaterialCategory,
                MaterialSubCategory = request.MSubCategory,
                RequestingQuantity = request.RequestingQuantity,
                AvailableQuantity = availableQuantity,
                IssuingQuantity = issuingQuantity, // Pre-fill existing IssuingQuantity
                PreviousIssuingQuantity = issuingQuantity, // Add this property to help in validation
                ClosingQuantity = 0,
                Issue = 0,
                IssuedBy = storeAdmin.StoreAdminID.ToString()
            };

            return PartialView("_HODIssueForm", issuingModel);

        }
        
        [HttpPost]
        public ActionResult HODIssueMaterial(HODIssueMaterial model)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IssueMaterial => RequestID={model.RequestID}, IssuingQty={model.IssuingQuantity}");

            try
            {
                var request = _db.HODRequests.FirstOrDefault(m => m.HODRequestID == model.RequestID);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToAction("HODRequests", "Home");
                }

                int approvedQty = request.RequestingQuantity ;
                int previousIssuedQty = request.IssuingQuantity ?? 0;

                // Add the new issue to previousIssuedQty
                int newIssueInput = model.Issue ?? 0;
                int totalIssuingQty = previousIssuedQty + newIssueInput;

                if (totalIssuingQty > approvedQty)
                {
                    TempData["ErrorMessage"] = $"Total issuing quantity ({totalIssuingQty}) exceeds approved quantity ({approvedQty}).";
                    return RedirectToAction("HODRequests", "Home");
                }

                if (newIssueInput > request.AvailableQuantity)
                {
                    TempData["ErrorMessage"] = $"New issuing quantity ({newIssueInput}) exceeds available stock ({request.AvailableQuantity}).";
                    return RedirectToAction("HODRequests", "Home");
                }

                int closingQty = (request.AvailableQuantity ?? 0) - newIssueInput;

                // 🔍 UPDATE existing record in EmployeeIssueMaterial table
                var existingIssue = _db.HODIssueMaterials.FirstOrDefault(e => e.RequestID == model.RequestID);
                if (existingIssue != null)
                {
                    existingIssue.IssuingQuantity = totalIssuingQty;
                    existingIssue.ClosingQuantity = closingQty;
                    existingIssue.IssuedDate = DateTime.Now;
                    existingIssue.Status = "Issued";
                    existingIssue.IssuedBy = model.IssuedBy;
                }
                else
                {
                    // If record not found, create new one (fallback)
                    model.IssuingQuantity = totalIssuingQty;
                    model.ClosingQuantity = closingQty;
                    model.IssuedDate = DateTime.Now;
                    model.Status = "Issued";
                    _db.HODIssueMaterials.Add(model);
                }

                // Update MaterialMasterList
                var masterItem = _db.MaterialMasterLists.FirstOrDefault(m =>
                    m.AssetType == model.AssetType &&
                    m.MaterialCategory == model.MaterialCategory &&
                    m.MaterialSubCategory == model.MaterialSubCategory
                );

                if (masterItem != null)
                {
                    masterItem.AvailableQuantity = closingQty;
                    masterItem.MaterialUpdatedDate = DateTime.Now;
                    masterItem.UpdatedBy = model.IssuedBy;
                }

                // Update Request table
                request.IssuingQuantity = totalIssuingQty;
                request.AvailableQuantity = closingQty;
                request.Status = (request.IssuingQuantity == request.RequestingQuantity) ? "Issued" : "Ongoing";

                _db.SaveChanges();

                TempData["SuccessMessage"] = "Material issued successfully!";
                return RedirectToAction("HODRequests", "Home");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine($"- Property: \"{ve.PropertyName}\", Error: \"{ve.ErrorMessage}\"");
                    }
                }

                TempData["ErrorMessage"] = "Validation failed.";
                throw;
            }
        }

        [HttpPost]
        public JsonResult RejectHODRequest(int hodrequestId)
        {
            try
            {
                var requests = _db.HODRequests.Where(r => r.HODRequestID == hodrequestId).ToList();

                if (requests.Count > 0)
                {
                    foreach (var req in requests)
                    {
                        req.Status = "Rejected";
                    }

                    _db.SaveChanges();

                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Request not found." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rejecting request: {ex.Message}");
                return Json(new { success = false, message = "Error occurred." });
            }
        }

        public ActionResult IssuedRequests(DateTime? fromDate, DateTime? toDate)
        {


            var userID = Session["UserID"] as string;
            var userRole = Session["UserRole"] as string;

            if (string.IsNullOrEmpty(userID) || userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("StoreAdminDasBoard");
            }

            var storeAdmin = _db.StoreAdmins.FirstOrDefault(e => e.StoreAdminID.ToString() == userID);
            if (storeAdmin == null)
            {
                TempData["ErrorMessage"] = "StoreAdmin details not found.";
                return RedirectToAction("StoreAdminDasBoard");
            }

            // Step 1: Fetch issued materials
            var issuedRequests = _db.HODIssueMaterials
                .Where(r => r.IssuedBy == storeAdmin.StoreAdminID.ToString() && r.Status == "Issued")
                .OrderByDescending(r => r.IssuedDate)
                .ToList();

            // Step 2: Group by RequestID
            var groupedData = issuedRequests
                .GroupBy(r => r.RequestID)
                .Select(g => new HODIssueGroupedViewModel
                {
                    RequestID = g.Key,
                    RequestDate = g.First().IssuedDate ?? DateTime.Now,

                    Materials = g.ToList() // All materials under this RequestID
                })
                .ToList();

            if (fromDate.HasValue)
            {
                groupedData = groupedData.Where(r => r.RequestDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                groupedData = groupedData.Where(r => r.RequestDate <= inclusiveToDate).ToList();
            }


            return View(groupedData);
        }



        public ActionResult MaterialMasterList()
        {
            // Fetch Asset Types from the database
            var assetTypes = _db.AssetTypes.Select(a => new
            {
                a.AssetTypeID,
                a.AssetType1
            }).ToList();

            ViewBag.AssetType = new SelectList(assetTypes, "AssetTypeID", "AssetType1");

            // Fetch materials to display on the page
            return View(_db.MaterialMasterLists.ToList());
            // Return an empty model to reset form

        }
        [HttpGet]
        public JsonResult GetMaterialCategories(string assetTypeName)
        {
            var categories = _db.MaterialCategories
                               .Where(m => m.AssetType.AssetType1 == assetTypeName) // Assuming AssetType navigation property
                               .Select(m => new
                               {
                                   m.MaterialCategory1
                               })
                               .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }


       [HttpGet]
public JsonResult GetMaterialSubCategories(string categoryName)
{
    var subcategories = _db.MaterialSubCategories
                           .Where(m => m.MaterialCategory.MaterialCategory1 == categoryName) // Assuming navigation
                           .Select(m => new
                           {
                               m.MaterialSubCategory1
                           })
                           .ToList();

    return Json(subcategories, JsonRequestBehavior.AllowGet);
}





        [HttpGet]
        public JsonResult CheckMaterialMaster(string assetTypes, string categories, string subcategories)
        {
            if (string.IsNullOrEmpty(assetTypes) || string.IsNullOrEmpty(categories) || string.IsNullOrEmpty(subcategories))
            {
                // If any field missing, return exists = false
                return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
            }

            // Directly checking by Names (NO IDs used here!)
            var item = _db.MaterialMasterLists
                .FirstOrDefault(m =>
                    m.AssetType.Equals(assetTypes, StringComparison.OrdinalIgnoreCase) &&
                    m.MaterialCategory.Equals(categories, StringComparison.OrdinalIgnoreCase) &&
                    m.MaterialSubCategory.Equals(subcategories, StringComparison.OrdinalIgnoreCase)
                );

            if (item != null)
            {
                // Entry found, return existing details
                return Json(new
                {
                    exists = true,
                    availableQuantity = item.AvailableQuantity,
                    //make = item.Make,
                    unit = item.Units,
                    minimumLimit = item.MinimumLimit,
                    //expiryDate = item.ExpiryDate?.ToString("yyyy-MM-dd")
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Entry doesn't exist
                return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult MaterialMasterForm(MaterialMasterList model)
        {
            // Check if combination exists
            var existing = _db.MaterialMasterLists
                .FirstOrDefault(m =>
                    m.AssetType == model.AssetType &&
                    m.MaterialCategory == model.MaterialCategory &&
                    m.MaterialSubCategory == model.MaterialSubCategory
                );

            if (existing != null)
            {
                // Update existing row
                existing.AvailableQuantity = model.AvailableQuantity;
                //existing.Make = model.Make;
                //existing.Units = model.Units;
                //existing.ExpiryDate = model.ExpiryDate;
                existing.MinimumLimit = model.MinimumLimit;
                existing.MaterialUpdatedDate = DateTime.Now;
                existing.UpdatedBy = "StoreAdmin"; // or from session

                try
                {
                    _db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }

                    throw; // rethrow if you want to preserve the exception
                }

            }
            else
            {
                // Create new row
                model.MaterialUpdatedDate = DateTime.Now;
                model.UpdatedBy = "StoreAdmin"; // or from session

                _db.MaterialMasterLists.Add(model);

                _db.SaveChanges();
            }

            TempData["SuccessMessage"] = "Material saved successfully!";
            return RedirectToAction("MaterialMasterList", "Home");
        }

        


//       

        //Fetch Material Categories based on selected Asset Type(AJAX)
        public JsonResult GetMaterialCategories1(int assetTypeID)
        {
            var categories = _db.MaterialCategories
                .Where(m => m.AssetTypeID == assetTypeID)
                .Select(m => new { MID = m.MID, MCategoryName = m.MaterialCategory1 })
                .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        // Fetch existing Material Subcategories (AJAX)
        public JsonResult GetMaterialSubCategories()
        {
            var subCategories = _db.MaterialSubCategories
                .Select(s => new { s.MID, s.MaterialSubCategory1 })
                .ToList();

            return Json(subCategories, JsonRequestBehavior.AllowGet);
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

        public ActionResult PODetails()
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            }

            return View(); // just the empty search form initially
        }

        [HttpPost]
        public ActionResult PODetails(string poNumber)
        {
            if (string.IsNullOrEmpty(poNumber))
            {
                ViewBag.Error = "Please enter a PO Number.";
                return View("PODetails");
            }

            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == poNumber);
            if (po == null)
            {
                ViewBag.Error = $"No Purchase Order found with PO Number {poNumber}.";
                return View("PODetails");
            }

            var items = _db.PurchaseOrderItems.Where(i => i.PONumber.ToString() == poNumber).ToList();

        
            var viewModel = new GeneratePOViewModel
            {
                PONumber = po.PONumber.ToString(),
                PODate  = (DateTime)po.PODate,
                UniversityName = po.UniversityName,
                UniversityAddress = po.UniversityAddress,
                UniversityPhone = po.UniversityPhone,
                UniversityEmail = po.UniversityEmail,
                RequisitionNo = po.RequisitionNo,
                VendorName = po.VendorName,
                ShipTo = po.ShipTo,
                RequisitionedBy = po.RequisitionedBy,
                WhenShip = po.WhenShip,
                ShipVia = po.ShipVia,
                FOBPoint = po.FOBPoint,
                Terms = po.Terms,
                CopiesOfInvoice = po.CopiesOfInvoice ??0,
                AuthorizedBy = po.AuthorizedBy,
                StoreUploads = po.StoreUploads,
                MRVDetails = po.MRVDetails,
                PurchaseOrderItems = items.Select(item => new PurchaseOrderItem
                
                {
                    POItemID = item.POItemID,
                    QtyOrdered = item.QtyOrdered ?? 0,
                    QtyReceived = item.QtyReceived,
                    Description = item.Description,
                    UnitPrice = item.UnitPrice ?? 0,
                    Total = item.Total,
                    Remarks = item.Remarks,
                    AcceptedQty = item.AcceptedQty,
                    RejectedQty = item.RejectedQty,
                    VendorEmail = item.VendorEmail,
                    Unit = item.Unit,
                    Make = item.Make,
                    ExpiryDate = item.ExpiryDate
                }).ToList()
            };
            TempData["PONumber"] = poNumber; // Store PO Number in TempData for later use
            return View(viewModel);
        }

        [HttpPost]
        public ActionResult UpdatePOItems(GeneratePOViewModel model)
        {
            if (model.PurchaseOrderItems != null && model.PurchaseOrderItems.Count > 0)
            {
                // Get PO once to check AuthorizedBy
                var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber.ToString() == model.PONumber);
                Debug.WriteLine($"[DEBUG] PO PhoneNumber Raw Value: '{po?.PhoneNumber}'");

                string auditorIdToAssign = null;

                if (po != null)
                {
                    //string auditorIdToAssign = null;

                    if (!po.UniversityName?.Trim().Equals(po.AuthorizedBy.Trim(), StringComparison.OrdinalIgnoreCase) == true)
                    {
                        
                        // Match university name in CentralAuditors table
                        var centralAuditor = _db.CentralAuditors
                            .FirstOrDefault(a => a.University.Trim().Equals(po.UniversityName.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (centralAuditor != null)
                        {
                            auditorIdToAssign = centralAuditor.AuditorID;
                        }
                    }
                    else
                    {
                        // Match using AuthorizedBy (which is a university name)
                        var university = _db.Universities
                            .FirstOrDefault(u => u.UniversityName.Trim().Equals(po.AuthorizedBy.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (university != null)
                        {
                            var accountant = _db.LocalAccountants
                                .FirstOrDefault(a => a.UniversityID == university.UniversityId);

                            if (accountant != null)
                            {
                                auditorIdToAssign = accountant.LocalAccountantID;
                            }
                        }
                    }

                    // ✅ Assign the AuditorID if found
                    //if (!string.IsNullOrEmpty(auditorIdToAssign))
                    //{
                    //    po.AuditorID = auditorIdToAssign;
                    //}

                    // ✅ Mark the PO as delivered
                    po.Status = "Delivered";
                }

                foreach (var item in model.PurchaseOrderItems)
                {
                    var existingItem = _db.PurchaseOrderItems.FirstOrDefault(p => p.POItemID == item.POItemID);

                    if (existingItem != null)
                    {
                        existingItem.QtyReceived = item.QtyReceived;
                        existingItem.Remarks = item.Remarks;
                       
                        existingItem.RejectedQty = item.RejectedQty;
                        existingItem.AcceptedQty = item.AcceptedQty;
                        existingItem.Make = item.Make;
                        existingItem.ExpiryDate = item.ExpiryDate;
                        existingItem.Unit = item.Unit;

                        existingItem.Total = (item.AcceptedQty ?? 0) * (existingItem.UnitPrice ?? 0);

                        // ✅ Assign AuditorID
                        existingItem.AuditorID = auditorIdToAssign;                                                                                                                        

                        // ✅ Update Available Quantity in Material Master
                        var material = _db.MaterialMasterLists
                            .FirstOrDefault(m => m.MaterialSubCategory == existingItem.Description);

                        if (material != null && item.AcceptedQty.HasValue)
                        {
                            material.AvailableQuantity += item.AcceptedQty.Value;
                            material.IsLowStockAlertSent = false;
                            material.Units = existingItem.Unit;
                            //material.Make = existingItem.Make;
                            //material.ExpiryDate = existingItem.ExpiryDate;

                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Updated Material: {material.MaterialSubCategory}, New AvailableQty: {material.AvailableQuantity}");
                        }

                        // ✅ NEW: Update MaterialAvailable table with Vendor and Material Details
                        if (item.AcceptedQty.HasValue && item.AcceptedQty.Value > 0)
                        {
                            // Get Vendor details from PO
                            // ✅ Convert PhoneNumber to int? safely
                            //int? phoneNumber = null;
                            //if (int.TryParse(po?.PhoneNumber, out int parsedPhone))
                            //{
                            //    phoneNumber = parsedPhone;
                            //}

                            Debug.WriteLine($"PhoneNumber: {po.PhoneNumber}");
                            // Get Vendor details from PO
                            string vendorName = po?.VendorName ?? "Unknown";
                            string vendorEmail = po?.VendorEmail ?? "";
                            string vendorPhone = po?.PhoneNumber ?? "";


                            // Check if material exists in MaterialAvailable table
                            var existingMaterialAvailable = _db.MaterialAvailables
                                .Where(m => m.MaterialName == existingItem.Description)
                                .ToList();

                            if (existingMaterialAvailable.Any())
                            {
                                // Material exists, check vendor
                                var vendorEntry = existingMaterialAvailable
                                    .FirstOrDefault(m => m.VendorName.Equals(vendorName, StringComparison.OrdinalIgnoreCase));

                                if (vendorEntry != null)
                                {
                                    // ✅ Vendor exists, update AvailableQuantity only
                                    vendorEntry.AvailableQty += item.AcceptedQty.Value;
                                    vendorEntry.ModifiedDate = DateTime.Now;
                                }
                                else
                                {
                                    // ✅ Vendor not found for this material, insert new record
                                    var newMaterial = new MaterialAvailable
                                    {
                                        MaterialName = existingItem.Description,
                                        VendorName = vendorName,
                                        VendorEmail = vendorEmail,
                                        VendorPhoneNumber = vendorPhone,
                                        AvailableQty = item.AcceptedQty.Value,
                                      
                                        Unit = existingItem.Unit,
                                        Make = item.Make,
                                       ExpiryDate = item.ExpiryDate,
                                      //Unit = item.Unit;
                                        //UnitPrice = existingItem.UnitPrice ?? 0,
                                        ReceivedDate = DateTime.Now
                                    };
                                    _db.MaterialAvailables.Add(newMaterial);
                                }
                            }
                            else
                            {
                                // ✅ Material not found, insert new record with all details
                                var newMaterial = new MaterialAvailable
                                {
                                    MaterialName = existingItem.Description,
                                    VendorName = vendorName,
                                    VendorEmail = vendorEmail,
                                    VendorPhoneNumber = vendorPhone,

                                    AvailableQty = item.AcceptedQty.Value,
                                    Unit = existingItem.Unit,
                                    Make = item.Make,
                                   
                                    ExpiryDate = item.ExpiryDate,
                                    //UnitPrice = existingItem.UnitPrice ?? 0,
                                    ReceivedDate = DateTime.Now
                                };
                                _db.MaterialAvailables.Add(newMaterial);
                            }
                        }

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"POItemID {item.POItemID} not found!");
                    }
                }

                // ✅ Save Certification File if provided
                if (model.CertificationFile != null && model.CertificationFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(model.CertificationFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/UploadedCertificates/"), fileName);
                    model.CertificationFile.SaveAs(path);

                    if (po != null)
                    {
                        po.StoreUploads = "/UploadedCertificates/" + fileName;
                    }
                }
               

                // ✅ Save MRV file
                if (model.MRVFile != null && model.MRVFile.ContentLength > 0)
                {
                    string mrvFileName = Path.GetFileName(model.MRVFile.FileName);
                    string mrvPath = Path.Combine(Server.MapPath("~/UploadedCertificates/"), mrvFileName);
                    model.MRVFile.SaveAs(mrvPath);

                    if (po != null)
                    {
                        po.MRVDetails = "/UploadedCertificates/" + mrvFileName;
                    }
                }

                try
                {
                    _db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        Debug.WriteLine($"Entity: {eve.Entry.Entity.GetType().Name}, State: {eve.Entry.State}");
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Debug.WriteLine($"Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    throw; // rethrow after logging
                }

                TempData["SuccessMessage"] = "Purchase Order items updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "No items received to update.";
            }

            return RedirectToAction("PODetails", new { poNumber = model.PONumber });
        }


      

        [HttpGet]
        public ActionResult Report(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null || endDate == null)
            {
                TempData["Error"] = "Please select both start and end dates.";
                return View(new ReportViewModel()); // empty model  
            }

            DateTime fromDate = startDate.Value.Date;
            DateTime toDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);

            // Fetch detailed data  
            var employeeData = _db.Requests
                .Where(e => e.IssuedDate >= fromDate && e.IssuedDate <= toDate)
                .Select(e => new MaterialIssueReportViewModel
                {
                    MaterialName = e.MSubCategory,
                    IssuedDate = e.IssuedDate ?? DateTime.MinValue, // Explicit conversion to handle nullable DateTime  
                    RequestedQuantity = e.RequestingQuantity,
                    IssuedQuantity = e.IssuingQuantity ?? 0,
                    ClosingQuantity = e.ClosingQuantity ?? 0,
                    IssuedTo = e.EmpID,
                    Role = "Employee"
                });

            var hodData = _db.HODIssueMaterials
                .Where(h => h.IssuedDate >= fromDate && h.IssuedDate <= toDate)
                .Select(h => new MaterialIssueReportViewModel
                {
                    MaterialName = h.MaterialSubCategory,
                    IssuedDate = h.IssuedDate ?? DateTime.MinValue, // Explicit conversion to handle nullable DateTime  
                    RequestedQuantity = h.RequestingQuantity ?? 0,
                    IssuedQuantity = h.IssuingQuantity ?? 0,
                    ClosingQuantity = h.ClosingQuantity ?? 0,
                    IssuedTo = h.HODID,
                    Role = "HOD"
                });

            var detailedList = employeeData
                .Concat(hodData)
                .OrderBy(x => x.IssuedDate)
                .ThenBy(x => x.MaterialName)
                .ToList();

            // Generate summary  
            var summaryList = detailedList
                .GroupBy(x => x.MaterialName)
                .Select(g => new MaterialSummaryViewModel
                {
                    MaterialName = g.Key,
                    TotalIssuedQuantity = g.Sum(x => x.IssuedQuantity),
                    ClosingQuantity = g.OrderByDescending(x => x.IssuedDate).First().ClosingQuantity
                })
                .OrderBy(x => x.MaterialName)
                .ToList();

            var model = new ReportViewModel
            {
                DetailedReports = detailedList,
                SummaryReports = summaryList
            };

            return View(model);
        }

        //public ActionResult MRV()
        //{
        //    return View();
        //}

        public ActionResult ExpiredMaterials()
        {
            using (var db = new ASPEntities2())
            {
                DateTime today = DateTime.Today;

                var expiredMaterials = db.MaterialMasterLists
                    //.Where(m => m.ExpiryDate != null && m.ExpiryDate < today)
                    .ToList();

                return View(expiredMaterials);
            }
        }
        public ActionResult Materials()
        {
            var materials = _db.MaterialMasterLists.ToList(); // Fetch all materials
            return View(materials);
        }
        // Add this helper method inside the HomeController class (or BaseController if you want it shared)
        private string GetLocalIPv4()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            return "127.0.0.1";
        }

        //private string GetPublicBaseUrl()
        //{
        //    // Use the server's LAN IP or a configured hostname instead of localhost
        //    string ip = "192.168.1.100"; // <-- Replace with your system's LAN IP or domain
        //    string port = "44374"; // your app's port
        //    return $"https://{ip}:{port}";
        //}

    }
}