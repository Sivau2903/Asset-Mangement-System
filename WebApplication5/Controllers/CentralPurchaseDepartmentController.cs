using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication5.Models;
using System.Data.Entity;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace WebApplication5.Controllers
{
    public class CentralPurchaseDepartmentController : BaseController
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        // GET: CentralPurchaseDepartment
        public ActionResult CPDDashBoard()
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
                        po.CommityApprovedDoc = virtualPath;
                    }
                    else if (fileKey == "InvoiceDetails")
                    {
                        po.InvoiceDetails = virtualPath;
                    }

                    po.Status = "Uploaded";
                }
            }

            _db.SaveChanges();
            TempData["UploadSuccess"] = "Documents uploaded successfully.";
            return RedirectToAction("CPDDashBoard");
        }

        public ActionResult RequestsRecevied()
        {
            string purchaseDepartmentID = (string)Session["UserID"];

            var raisedRequests = (from request in _db.SavetoCentrals
                                  where request.CentralID == purchaseDepartmentID &&
                                        (request.Status == "Sent to Central" || request.Status == "Some in Processing")
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
                                      IUCDApprovedQty = request.IUCDApprovedQty ?? 0, // Explicitly handle nullable int  
                                      PurchaseDepartmentUploads = request.PurchaseDepartmentUploads,
                                      CentralID = request.CentralID,
                                      Status = request.Status
                                  }).ToList();

            return View(raisedRequests);
        }



        public JsonResult GetAvailableBudget()
        {
            string sessionID = (string)Session["UserID"]; // Adjust based on your session handling
            var budget = _db.CentralPurchaseDepartments
                            .Where(lpd => lpd.CentralID == sessionID)
                            .Select(lpd => lpd.Budget)
                            .FirstOrDefault();

            return Json(budget, JsonRequestBehavior.AllowGet);
        }

      

        public ActionResult GetVendors(string material)
        {
            Session["Material"] = material;
            var vendorDetails = _db.CentralVendorDetails
                .Where(v => v.Material == material)
                .ToList();

            return PartialView("_CentralVendorDetailsPartial", vendorDetails);
        }


        [HttpGet]
        public ActionResult CentralGeneratePO()
        {
            if (TempData["POViewModel"] is CentralGeneratePOViewModel model &&
                TempData["University"] is University university &&
                TempData["Central"] is CentralPurchaseDepartment user)
            {
                // Keep TempData alive for potential future reload
                TempData.Keep("POViewModel");
                TempData.Keep("University");
                TempData.Keep("Central");

                ViewBag.University = university;
                ViewBag.Central = user;

                return View("CentralGeneratePO", model);
            }

            return RedirectToAction("RequestsRecevied"); // Redirect to default if no data
        }


        [HttpPost]
        public ActionResult CentralGeneratePO(
        string UniversityName,
        string VendorName,
        string VendorEmail,
        string VendorAddress,
        string VendorGSTNo,
        string GSTPercent,
        string materialsJson)
        {
            Debug.WriteLine("==== CentralGeneratePO (POST) called ====");
            Debug.WriteLine($"University: {UniversityName}, Vendor: {VendorName}");

            // Fetch university
            var university = _db.Universities.FirstOrDefault(u => u.UniversityName == UniversityName);
            if (university == null)
            {
                Debug.WriteLine("University not found.");
                return HttpNotFound("University not found");
            }

            // Fetch central department user
            string userId = (string)Session["UserID"];
            var user = _db.CentralPurchaseDepartments.FirstOrDefault(u => u.CentralID == userId);
            if (user == null)
            {
                Debug.WriteLine("Central user not found.");
                return HttpNotFound("Central user not found");
            }
            var decodedJson = HttpUtility.UrlDecode(materialsJson);
            Debug.WriteLine("materialsJson: " + materialsJson);

            // Deserialize materials list
            List<PurchaseOrderItem> materials;
            try
            {
               
                materials = JsonConvert.DeserializeObject<List<PurchaseOrderItem>>(decodedJson);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Deserialization error: " + ex.Message);
                return new HttpStatusCodeResult(400, "Invalid materials data");
            }

            // Generate requisition number
            string requisitionNo = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" +
                                   Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

            // Calculate total quantity and total cost
            int totalQuantity = materials.Sum(m => m.QtyOrdered ?? 0);
            decimal totalCost = materials.Sum(m => m.Total ?? 0);

           


            // Prepare view model
            var viewModel = new CentralGeneratePOViewModel
            {
                UniversityName = university.UniversityName,
                UniversityEmail = university.UniversityEmail,
                UniversityPhone = university.UniversityPhone,
                UniversityAddress = university.Address,
                ShipTo = university.Address,
                RequisitionNo = requisitionNo,


                CentralDepartmentName = user.CentralDepartmentName,
                CentralDepartmentPhone = user.CentralDepartmentPhone,
                CentralDepartmentEmail = user.CentralDepartmentEmail,
                CentralDepartmentAddress = user.CentralDepartmentAddress,

                VendorName = VendorName,
                VendorEmail = VendorEmail,
                VendorAddress = VendorAddress,
                VendorGSTNo = VendorGSTNo,
                VendorGSTPercent = GSTPercent,

                RequisitionedBy = user.CentralDepartmentName,
               

                TotalQuantity = totalQuantity,
                TotalCost = totalCost,

                PurchaseOrderItems = materials

            };

            ViewBag.University = university;
            ViewBag.Central = user;

            // Store model for GET reload
            TempData["POViewModel"] = viewModel;
            TempData["University"] = university;
            TempData["Central"] = user;

            // Redirect to GET method
            return RedirectToAction("CentralGeneratePO");
        }


        [HttpGet]
        public ActionResult PreviewPO()
        {
            if (TempData["POModel"] is CentralGeneratePOViewModel model)
            {
                TempData.Keep("POModel"); // Keep it for next reload if needed
                return View(model);
            }
            return RedirectToAction("CentralGeneratePO"); // fallback if no model found
        }

        [HttpPost]
        public ActionResult PreviewPO(CentralGeneratePOViewModel model)
        {
            TempData["POModel"] = model;
            return View("PreviewPO", model); // POPreview.cshtml is the preview page
        }



        [HttpPost]
        public ActionResult BulkPO(CentralGeneratePOViewModel model, string SerializedItems)
        {


            if (!ModelState.IsValid)
            {
                return View("CentralGeneratePO", model); // Fixed: Return the correct view
            }

            if (!string.IsNullOrEmpty(SerializedItems))
            {
                var decoded = HttpUtility.UrlDecode(SerializedItems);
                model.PurchaseOrderItems = JsonConvert.DeserializeObject<List<PurchaseOrderItem>>(decoded);
            }

            try
            {
                string userId = (string)Session["UserID"];

                var po = new PurchaseOrder
                {
                    UniversityName = model.UniversityName,
                    UniversityEmail = model.UniversityEmail,
                    UniversityPhone = model.UniversityPhone,
                    UniversityAddress = model.UniversityAddress,
                    PODate = DateTime.Now,
                    RequisitionNo = model.RequisitionNo,
                    RequisitionedBy = model.RequisitionedBy,
                    WhenShip = model.WhenShip,
                    ShipVia = model.ShipVia,
                    FOBPoint = model.FOBPoint,
                    Terms = model.Terms,
                    TermsConditions = model.TermsConditions,
                    VendorName = model.VendorName,
                    VendorEmail = model.VendorEmail,
                    VendorAddress = model.VendorAddress,
                    VendorGSTNo = model.VendorGSTNo,
                    TotalQuantity = model.TotalQuantity,
                    TotalCost = model.TotalCost,
                    AuthorizedBy = model.RequisitionedBy,
                    CreatedBy = userId,
                    Status = "Draft"
                };

                _db.PurchaseOrders.Add(po);
                _db.SaveChanges(); // Generates PONumber

                if (model.PurchaseOrderItems != null)
                {
                    foreach (var item in model.PurchaseOrderItems)
                    {
                        var poItem = new PurchaseOrderItem
                        {
                            PONumber = po.PONumber,
                            Description = item.Description,
                            QtyOrdered = item.QtyOrdered,
                            UnitPrice = item.UnitPrice,
                            Total = item.Total
                        };

                        _db.PurchaseOrderItems.Add(poItem);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "No purchase order items were submitted.");
                    return View("CentralGeneratePO", model); // Fixed fallback
                }

                _db.SaveChanges();
                TempData["Success"] = "PO Created Successfully!";
                TempData["VendorEmail"] = model.VendorEmail;

                return RedirectToAction("InitiatePOEmail", new { PONumber = po.PONumber });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View("CentralGeneratePO", model); // Fixed fallback
            }
        }



     


        public ActionResult SendMail(int PONumber)
        {
            Debug.WriteLine($"[DEBUG] Starting SendMail for PO: {PONumber}");

            var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber == PONumber);
            if (po == null)
            {
                Debug.WriteLine("[ERROR] Purchase Order not found.");
                return HttpNotFound("PO not found");
            }

            Debug.WriteLine("[DEBUG] Purchase Order found.");

            var items = _db.PurchaseOrderItems.Where(i => i.PONumber == PONumber).ToList();
            Debug.WriteLine($"[DEBUG] Retrieved {items.Count} purchase order items.");



            // Prepare ViewModel for PDF
            var model = new CentralGeneratePOViewModel
            {
                PONumber = po.PONumber.ToString(),
                UniversityName = po.UniversityName,
                UniversityEmail = po.UniversityEmail,
                UniversityPhone = po.UniversityPhone,
                UniversityAddress = po.UniversityAddress,

                PODate = DateTime.Now,
                RequisitionNo = po.RequisitionNo,
                RequisitionedBy = po.RequisitionedBy,
                WhenShip = po.WhenShip,
                ShipVia = po.ShipVia,
                FOBPoint = po.FOBPoint,
                Terms = po.Terms,
                TermsConditions = po.TermsConditions,
                VendorName = po.VendorName,
                VendorEmail = po.VendorEmail,
                VendorAddress = po.VendorAddress,
                VendorGSTNo = po.VendorGSTNo,
                TotalQuantity = po.TotalQuantity??0,
                TotalCost = po.TotalCost ?? 0,
                AuthorizedBy = po.RequisitionedBy,
                //PurchaseDepartmentUploads = po.PurchaseDepartmentUploads,
                PurchaseOrderItems = items.Select(item => new PurchaseOrderItem
                {
                    Description = item.Description,
                    QtyOrdered = item.QtyOrdered,
                    UnitPrice = item.UnitPrice,
                    Total = item.Total
                    //VendorEmail = item.VendorEmail
                }).ToList()
            };
            Debug.WriteLine("[DEBUG] ViewModel for PDF created.");
            //Debug.WriteLine($"[DEBUG] CopiesOfInvoice in ViewModel = {model.CopiesOfInvoice}");

            Debug.WriteLine("[DEBUG] ViewModel for PDF created.");

            // Generate PDF from View
            var pdf = new Rotativa.ViewAsPdf("POPDFView", model)
            {
                FileName = $"PurchaseOrder_{PONumber}.pdf"
            };

            Debug.WriteLine("[DEBUG] Generating PDF...");
            var pdfBytes = pdf.BuildFile(ControllerContext);
            Debug.WriteLine($"[DEBUG] PDF generated. Size: {pdfBytes.Length} bytes");

            po.PODetails = pdfBytes;

            // Email Details
            string toEmail = TempData["VendorEmail"]?.ToString() ?? "default@vendor.com";
            string subject = $"Purchase Order – PO No: {PONumber}";
            string body = $"Dear Vendor,\n\nPlease find attached the Purchase Order #{PONumber}.\n\nRegards,\nICFAI Procurement Team";

            Debug.WriteLine($"[DEBUG] Sending email to: {toEmail}");

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress("shivaupputuri5@gmail.com");
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"PO_{PONumber}.pdf"));

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("shivaupputuri5@gmail.com", "uxwvtphmvzhqqqpl");
                        smtp.EnableSsl = true;

                        smtp.Send(message);
                        Debug.WriteLine("[DEBUG] Email sent successfully.");
                    }
                }

                // Update status of relevant materials in SavetoCentral table
                // Update status of all relevant materials in SavetoCentral table
                foreach (var item in items)
                {
                    var matchingRequests = _db.SavetoCentrals
                        .Where(r => r.Material == item.Description && r.Status != "Sent for Purchase")
                        .ToList();

                    foreach (var request in matchingRequests)
                    {
                        request.Status = "Sent for Purchase";
                    }
                }

                _db.SaveChanges();
                Debug.WriteLine("[DEBUG] Updated all matching rows to 'Sent for Purchase' in SavetoCentral table.");



                TempData["ShowSuccessPopup"] = true;
                TempData["PONumber"] = PONumber;

                return View("SendMailConfirmation");
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to send PO.";
                return RedirectToAction("RequestsRecieved"); // fallback
            }
        }

        public ActionResult MyRequests()
        {
            string userId = (string)Session["UserID"];

            // Get all POs created by this user
            var purchaseOrders = _db.CentralPurchaseOrders
                                    .Where(po => po.CreatedBy == userId.ToString())
                                    .OrderByDescending(po => po.PODate)
                                    .ToList();

            // Create a ViewModel list for each PO with its items
            var viewModel = purchaseOrders.Select(po => new CentralPurchaseOrderGroupedViewModel
            {
                PONumber = po.PONumber,
                PODate = (DateTime)po.PODate,
                CentralPurchaseOrderItems = _db.CentralPurchaseOrderItems
                                        .Where(item => item.PONumber == po.PONumber)
                                        .ToList()
            }).ToList();

            return View(viewModel);
        }

       

        public ActionResult InitiatePOEmail(int PONumber)
        {
            string userId = (string)Session["UserID"];
            var user = _db.CentralPurchaseDepartments.FirstOrDefault(u => u.CentralID == userId.ToString());

            if (user == null)
                return HttpNotFound("Purchase department user not found.");

            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;
            Session["PONumberToSend"] = PONumber;
            Session["OTPAttempts"] = 0;

            //SendEmail(user.EmailID, "OTP for PO Email Confirmation", $"Your OTP is: {otp}");

            string toEmail = user.CentralDepartmentEmail;
            string subject = $"Verification OTP for PO Email Confirmation – OTP:{otp}, PO No: {PONumber}";
            string body = $"Dear Purchase Department,\n\nPlease find attached the OTP  {otp}.\n\nRegards,\nICFAI ";

            Debug.WriteLine($"[DEBUG] Sending email to: {toEmail}");

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress("shivaupputuri5@gmail.com");
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    //message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"PO_{PONumber}.pdf"));

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("shivaupputuri5@gmail.com", "uxwvtphmvzhqqqpl");
                        smtp.EnableSsl = true;

                        smtp.Send(message);
                        Debug.WriteLine("[DEBUG] Email sent successfully.");
                    }
                }

                //TempData["OTP"] = true;
                TempData["PONumber"] = PONumber;

                return View("EnterOTP");
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to send PO.";
                return RedirectToAction("RequestsReceived"); // fallback
            }


        }


        [HttpPost]
        public ActionResult VerifyOTPAndSendPO(string enteredOTP)
        {
            string storedOTP = Session["OTP"]?.ToString();
            int attempts = (int)(Session["OTPAttempts"] ?? 0);

            if (storedOTP == enteredOTP)
            {
                int poNumber = (int)Session["PONumberToSend"];
                Session.Remove("OTP");
                Session.Remove("OTPAttempts");
                return RedirectToAction("SendMail", new { PONumber = poNumber });
            }

            attempts++;
            Session["OTPAttempts"] = attempts;

            if (attempts >= 3)
            {
                TempData["Error"] = "Failed to send PO: Too many incorrect OTP attempts.";
                return RedirectToAction("RequestsReceived");
            }

            ViewBag.Error = $"Invalid OTP. Attempt {attempts}/3.";
            return View("EnterOTP");
        }

        public ActionResult VendorMaster()
        {
            ViewBag.AssetTypes = _db.AssetTypes.ToList();
            //ViewBag.Universities = _db.Universities.ToList();
            return View(new VendorDetailViewModel { Materials = new List<MaterialSelection> { new MaterialSelection() } });
        }

        [HttpPost]
        public ActionResult VendorMaster1(VendorDetailViewModel model)
        {
            if (ModelState.IsValid)
            {
                string userId = Session["UserID"]?.ToString();
                //if (string.IsNullOrEmpty(userId))
                //{
                //    System.Diagnostics.Debug.WriteLine("[DEBUG] No UserID in session.");
                //    return View("VendorMaster");
                //}

                //var localPD = _db.CentralPurchaseDepartments.FirstOrDefault(x => x.C == userId);
                //if (localPD == null)
                //{
                //    System.Diagnostics.Debug.WriteLine($"[DEBUG] No LocalPurchaseDepartment found for UserID: {userId}");
                //    return View("VendorMaster");
                //}

                //int universityId = localPD.UniversityID;

                int newVendorId = 1;
                var lastVendor = _db.CentralVendorDetails.OrderByDescending(v => v.VendorID).FirstOrDefault();
                if (lastVendor != null)
                {
                    newVendorId = lastVendor.VendorID + 1 ?? 0;
                }

                foreach (var material in model.Materials)
                {
                    var subCategory = _db.MaterialSubCategories.Find(material.MaterialSubCategoryID);

                    CentralVendorDetail vendor = new CentralVendorDetail
                    {
                        VendorID = newVendorId,
                        VendorName = model.VendorName,
                        EmailID = model.EmailID,
                        PhoneNumber = model.PhoneNumber,
                        GSTNO = model.GSTNO,
                        PanNumber = model.PanNumber,
                        //UniversityID = universityId,
                        PricePerUnit = material.PricePerUnit,
                        Address = model.Address,
                        Material = subCategory?.MaterialSubCategory1, // Only MaterialSubCategory saved
                        GSTPercentage = material.GSTPercentage
                    };
                    _db.CentralVendorDetails.Add(vendor);
                }

                _db.SaveChanges();
                TempData["Success"] = "Vendor saved successfully.";
                return RedirectToAction("VendorMaster");
            }

            // Reload dropdowns
            ViewBag.AssetTypes = _db.AssetTypes.ToList();
            ViewBag.Universities = _db.Universities.ToList();
            return View(model);
        }

        public JsonResult GetCategoriesByAssetType(int assetTypeId)
        {
            var categories = _db.MaterialCategories
                .Where(c => c.AssetTypeID == assetTypeId)
                .Select(c => new {
                    id = c.MID,                    // ✅ return MID as id
                    name = c.MaterialCategory1     // ✅ return name
                })
                .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetSubCategoriesByCategory(int categoryId)
        {
            var subCategories = _db.MaterialSubCategories
                .Where(sc => sc.MID == categoryId)
                .Select(sc => new {
                    id = sc.MSubCategoryID,             // ✅ use proper ID
                    name = sc.MaterialSubCategory1      // ✅ use proper name
                })
                .ToList();

            return Json(subCategories, JsonRequestBehavior.AllowGet);
        }

    }
}