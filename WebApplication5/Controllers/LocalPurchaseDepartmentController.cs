using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using WebApplication4.Models;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class LocalPurchaseDepartmentController : BaseController
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        private readonly EmailService _emailService = new EmailService();  // Injecting EmailService
        // GET: LocalPurchaseDepartment
        public ActionResult LPDDashBoard()
        {
            return View();
        }

        public ActionResult Materials()
        {
            var materials = _db.MaterialMasterLists.ToList();

            // Pass TempData back to view
            ViewBag.ExpandedCategory = TempData["ExpandedCategory"];
            return View(materials);
        }


        public ActionResult RequestsRecevied()
        {
            string purchaseDepartmentID = (string)Session["UserID"];

            var raisedRequests = (from request in _db.TempSelectedMaterials
                                  where request.UserID == purchaseDepartmentID && request.Status == "New"
                                  select request).ToList();



            return View(raisedRequests);
        }

        [HttpPost]
        public ActionResult AddToReceivedRequests(string materialSubCategory)
        {
            string userId = (string)Session["UserID"];
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Session UserID: {userId}");

            if (userId == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] No UserID in session.");
                return PartialView("_VendorDetailsPartial", new List<VendorDetail>());
            }

            // Step 2: Get UniversityID from LocalPurchaseDepartment table based on UserID
            var localPD = _db.LocalPurchaseDepartments.FirstOrDefault(x => x.LocalID == userId);
            if (localPD == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] No LocalPurchaseDepartment found for UserID: {userId}");
                return PartialView("_VendorDetailsPartial", new List<VendorDetail>());
            }

            int universityId = localPD.UniversityID;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Found UniversityID: {universityId} for UserID: {userId}");

            // Fix for CS0019: Ensure proper type conversion for comparison  
            var exists = _db.TempSelectedMaterials
                           .Any(m => m.UserID == userId &&
                                     m.MaterialSubCategory == materialSubCategory &&
                                     m.Status == "New");

            if (!exists)
            {
                _db.TempSelectedMaterials.Add(new TempSelectedMaterial
                {
                    UserID = (string)Session["UserID"],
                    MaterialSubCategory = materialSubCategory,
                    Status = "New",
                    UniversityID = universityId.ToString()
                });
                _db.SaveChanges();

                TempData["SuccessMessage"] = $"{materialSubCategory} added to Received Requests.";
            }
            else
            {
                TempData["InfoMessage"] = $"{materialSubCategory} is already added.";
            }
            var material = _db.MaterialMasterLists.FirstOrDefault(m => m.MaterialSubCategory == materialSubCategory);
            if (material != null)
            {
                TempData["ExpandedCategory"] = material.MaterialCategory;
                TempData["SuccessMessage"] = $"{materialSubCategory} added successfully!";
            }
            else
            {
                TempData["InfoMessage"] = "Material not found.";
            }
            return RedirectToAction("Materials"); 
        }


        public JsonResult GetAvailableBudget()
        {
            string sessionID = (string)Session["UserID"]; // Adjust based on your session handling
            var budget = _db.LocalPurchaseDepartments
                            .Where(lpd => lpd.LocalID == sessionID)
                            .Select(lpd => lpd.Budget)
                            .FirstOrDefault();

            return Json(budget, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SendToCentral(string materialName, int quantity, HttpPostedFileBase certificateUpload)
        {
            try
            {
                if (certificateUpload == null || certificateUpload.ContentLength == 0)
                {
                    return Json(new { success = false, message = "Certificate file is required." });
                }

                string localUserId = (string)Session["UserID"];

                var user = _db.LocalPurchaseDepartments.FirstOrDefault(e => e.LocalID == localUserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var fileName = Path.GetFileNameWithoutExtension(certificateUpload.FileName);
                var extension = Path.GetExtension(certificateUpload.FileName);
                fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                var filePath = Path.Combine(Server.MapPath("~/UploadedCertificates"), fileName);
                certificateUpload.SaveAs(filePath);

                // Save relative path in DB
                filePath = "~/UploadedCertificates/" + fileName;

                //request.PurchaseDepartmentUploads = "/Uploads/" + fileName; // Save only the path

                var request = new SavetoCentral
                {
                    LocalID = localUserId,
                    CentralID = "Nill",
                    IUCDID = "2025IUCD1",
                    UniversityID = user.UniversityID,
                    Material = materialName,
                    Order_Quantity = quantity,
                    RequestedDate = DateTime.Now,
                    PurchaseDepartmentUploads = filePath, // Store the certificate file here
                    Status = "Sent to IUCD"
                };

                _db.SavetoCentrals.Add(request);

                var tempMaterial = _db.TempSelectedMaterials
                                      .FirstOrDefault(t => t.UserID == localUserId && t.MaterialSubCategory == materialName && t.Status == "New");

                if (tempMaterial != null)
                {
                    tempMaterial.Status = "Sent";
                    _db.Entry(tempMaterial).State = EntityState.Modified;
                }

                _db.SaveChanges();

                return Json(new { success = true, message = "Sent to IUCD Department." });
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(eve => eve.ValidationErrors)
                    .Select(ve => $"{ve.PropertyName}: {ve.ErrorMessage}");

                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = $"Validation failed: {fullErrorMessage}";

                return Json(new { success = false, message = exceptionMessage });
            }

        }



        public ActionResult GetVendors(string materialsubCategory)
        {
            // Step 1: Get UserID from session                
            string userId = (string)Session["UserID"];
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Session UserID: {userId}");

            if (userId == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] No UserID in session.");
                return PartialView("_VendorDetailsPartial", new List<VendorDetail>());
            }

            // Step 2: Get UniversityID from LocalPurchaseDepartment table based on UserID
            var localPD = _db.LocalPurchaseDepartments.FirstOrDefault(x => x.LocalID == userId);
            if (localPD == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] No LocalPurchaseDepartment found for UserID: {userId}");
                return PartialView("_VendorDetailsPartial", new List<VendorDetail>());
            }

            int universityId = localPD.UniversityID;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Found UniversityID: {universityId} for UserID: {userId}");

            // Step 3: Fetch vendor details
            var vendorDetails = _db.VendorDetails
                .Where(v => v.UniversityID == universityId &&
                            v.Material.Trim().ToLower() == materialsubCategory.Trim().ToLower())
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] MaterialSubCategory: {materialsubCategory}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Vendor count fetched: {vendorDetails.Count}");

            return PartialView("_VendorDetailsPartial", vendorDetails);
        }


        //public ActionResult GeneratePO(string material, int qty, string vendorId, decimal price, decimal cost, string email)
        //{
        //    // Fetch user info from session
        //    int userId = Convert.ToInt32(Session["UserID"]);
        //    var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId);
        //    if (user == null) return HttpNotFound("User not found");

        //    // Fetch university info
        //    var university = _db.Universities.FirstOrDefault(u => u.UniversityId == user.UniversityID);
        //    if (university == null) return HttpNotFound("University not found");

        //    // Generate unique requisition number
        //    string requisitionNo = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

        //    var viewModel = new GeneratePOViewModel
        //    {
        //        //PONumber = "PO-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
        //        VendorEmail = email,

        //        UniversityName = university.UniversityName,
        //        UniversityPhone = university.UniversityPhone,
        //        UniversityEmail = university.UniversityEmail,
        //        UniversityAddress = university.Address,
        //        RequisitionedBy = university.UniversityName,
        //        RequisitionNo = requisitionNo,
        //        AuthorizedBy = university.UniversityName,

        //        PurchaseOrderItems = new List<PurchaseOrderItem>
        //{
        //    new PurchaseOrderItem
        //    {
        //        Description = material,
        //        QtyOrdered = qty,
        //        UnitPrice = price,
        //        Total = cost,
        //        VendorID = Convert.ToInt32(vendorId)
        //    }
        //}
        //    };

        //    ViewBag.University = university;
        //    return View(viewModel);
        //}

        //[HttpPost]
        //public ActionResult GeneratePO(string vendorId, string email, string materials)
        //{
        //    if (string.IsNullOrEmpty(materials))
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Materials data is missing.");
        //    }

        //    var materialList = JsonConvert.DeserializeObject<List<MaterialItem>>(materials);

        //    string userId = (string)Session["UserID"];
        //    var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId);
        //    if (user == null) return HttpNotFound("User not found");

        //    var university = _db.Universities.FirstOrDefault(u => u.UniversityId == user.UniversityID);
        //    if (university == null) return HttpNotFound("University not found");

        //    string requisitionNo = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

        //    var viewModel = new GeneratePOViewModel
        //    {
        //        VendorEmail = email,
        //        UniversityName = university.UniversityName,
        //        UniversityPhone = university.UniversityPhone,
        //        UniversityEmail = university.UniversityEmail,
        //        UniversityAddress = university.Address,
        //        RequisitionedBy = university.UniversityName,
        //        AuthorizedBy = university.UniversityName,
        //        RequisitionNo = requisitionNo,
        //        PurchaseOrderItems = materialList.Select(m => new PurchaseOrderItem
        //        {
        //            Description = m.MaterialName,
        //            QtyOrdered = m.QtyOrdered,
        //            UnitPrice = m.UnitPrice,
        //            Total = m.Total,
        //            VendorID = Convert.ToInt32(vendorId),
        //            VendorEmail = email
        //        }).ToList()
        //    }; 

        //    ViewBag.University = university;
        //    return View(viewModel);
        //}

        [HttpGet]
        public ActionResult GeneratePO()
        {
            if (TempData["POViewModel"] is CentralGeneratePOViewModel model &&
                TempData["University"] is University university
                )
            {
                // Keep TempData alive for potential future reload
                TempData.Keep("POViewModel");
                TempData.Keep("University");
                //TempData.Keep("Central");

                ViewBag.University = university;
                //ViewBag.Central = user;

                return View("GeneratePO", model);
            }

            return RedirectToAction("RequestsRecevied"); // Redirect to default if no data
        }

        [HttpPost]
        public ActionResult GeneratePO(
       string VendorName,
       string VendorEmail,
       string PhoneNumber,
       string VendorAddress,
       string VendorGSTNo,
       string GSTPercent,
       string materialJson)  // Note: name must match hidden input
        {
            Debug.WriteLine("==== GeneratePO (POST) called ====");
            Debug.WriteLine($"PhoneNumber: {PhoneNumber}");

            // 1. User Validation
            string userId = (string)Session["UserID"];
            var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId);
            if (user == null)
                return HttpNotFound("User not found");

            var university = _db.Universities.FirstOrDefault(u => u.UniversityId == user.UniversityID);
            if (university == null)
                return HttpNotFound("University not found");

            // 2. Decode and Deserialize JSON
            List<PurchaseOrderItem> materials;
            try
            {
                var decodedJson = HttpUtility.UrlDecode(materialJson);
                Debug.WriteLine("Decoded materialsJson: " + decodedJson);

                materials = JsonConvert.DeserializeObject<List<PurchaseOrderItem>>(decodedJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Deserialization error: " + ex.Message);
                return new HttpStatusCodeResult(400, "Invalid materials data");
            }

            if (materials == null || !materials.Any())
                return new HttpStatusCodeResult(400, "No material data found");

            foreach (var m in materials)
            {
                Debug.WriteLine($"Material: {m.Description}, Qty: {m.QtyOrdered}, Cost: {m.Total}");
            }


            // 3. Parse GST
            float.TryParse(GSTPercent, out float gstPercent);

            // 4. Total Calculations
            int totalQuantity = materials.Sum(m => m.QtyOrdered ?? 0);
            decimal totalCost = materials.Sum(m => m.Total ?? 0);
            string materialName = materials.FirstOrDefault()?.Description ?? "Unknown Material";


            // 5. Generate PO Number
            string requisitionNo = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" +
                                   Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

            // 6. Prepare ViewModel
            var viewModel = new CentralGeneratePOViewModel
            {
                UniversityName = university.UniversityName,
                UniversityEmail = university.UniversityEmail,
                UniversityPhone = university.UniversityPhone,
                UniversityAddress = university.Address,
                ShipTo = university.Address,
                RequisitionNo = requisitionNo,
               

                VendorName = VendorName,
                VendorEmail = VendorEmail,
                PhoneNumber = PhoneNumber,
                VendorAddress = VendorAddress,
                VendorGSTNo = VendorGSTNo,
                VendorGSTPercent = GSTPercent,

                RequisitionedBy = university.UniversityName,
                TotalQuantity = totalQuantity,
                TotalCost = totalCost,
                PurchaseOrderItems = materials

            };

            ViewBag.University = university;
            ViewBag.Central = user;

            // 7. Save and Redirect
            TempData["POViewModel"] = viewModel;
            TempData["University"] = university;

            return RedirectToAction("GeneratePO");
        }




        [HttpGet]
        public ActionResult PreviewPO()
        {
            if (TempData["POModel"] is CentralGeneratePOViewModel model)
            {
                TempData.Keep("POModel"); // Keep it for next reload if needed
                return View(model);
            }
            return RedirectToAction("GeneratePO"); // fallback if no model found
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
                return View("GeneratePO", model); // Fixed: Return the correct view
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
                    PhoneNumber = model.PhoneNumber,
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
                            Total = item.Total,
                          
                        };

                        _db.PurchaseOrderItems.Add(poItem);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "No purchase order items were submitted.");
                    return View("GeneratePO", model); // Fixed fallback
                }

                _db.SaveChanges();
                TempData["Success"] = "PO Created Successfully!";
                TempData["VendorEmail"] = model.VendorEmail;

                return RedirectToAction("InitiatePOEmail", new { PONumber = po.PONumber });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View("GeneratePO", model); // Fixed fallback
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
                PhoneNumber = po.PhoneNumber,
                VendorAddress = po.VendorAddress,
                VendorGSTNo = po.VendorGSTNo,
                TotalQuantity = po.TotalQuantity ?? 0,
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
                //foreach (var item in items)
                //{
                //    var matchingRequests = _db.SavetoCentrals
                //        .Where(r => r.Material == item.Description && r.Status != "Sent for Purchase")
                //        .ToList();

                //    foreach (var request in matchingRequests)
                //    {
                //        request.Status = "Sent for Purchase";
                //    }
                //}

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

      



        public ActionResult InitiatePOEmail(int PONumber)
        {
            string userId = (string)Session["UserID"];
            var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId.ToString());

            if (user == null)
                return HttpNotFound("Purchase department user not found.");

            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;
            Session["PONumberToSend"] = PONumber;
            Session["OTPAttempts"] = 0;

            //SendEmail(user.EmailID, "OTP for PO Email Confirmation", $"Your OTP is: {otp}");

            string toEmail = user.EmailID;
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






        //[HttpPost]
        //public ActionResult GeneratePO123(GeneratePOViewModel viewModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(viewModel);

        //    }

        //    try
        //    {
        //        // Get current logged-in User ID
        //        string userId = (string)Session["UserID"];

        //        // Get UniversityID by current User (Assuming mapping exists)
        //        var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId);
        //        if (user == null)
        //        {
        //            ModelState.AddModelError("", "User not found.");
        //            return View(viewModel);
        //        }

        //        var university = _db.Universities.FirstOrDefault(u => u.UniversityId == user.UniversityID);
        //        if (university == null)
        //        {
        //            ModelState.AddModelError("", "University not found.");
        //            return View(viewModel);
        //        }

        //        string filePath = null;

        //        if (viewModel.CertificationFile != null && viewModel.CertificationFile.ContentLength > 0)
        //        {
        //            string uploadsFolder = Server.MapPath("~/UploadedCertificates");
        //            if (!Directory.Exists(uploadsFolder))
        //                Directory.CreateDirectory(uploadsFolder);

        //            // Use the original filename as uploaded by the user
        //            string originalFileName = Path.GetFileName(viewModel.CertificationFile.FileName);
        //            string fullPath = Path.Combine(uploadsFolder, originalFileName);
        //            viewModel.CertificationFile.SaveAs(fullPath);

        //            // Save relative path in DB
        //            filePath = "~/UploadedCertificates/" + originalFileName;
        //        }


        //        // Generate unique requisition number
        //        string requisitionNo = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        //        // Save Master Table
        //        var purchaseOrder = new PurchaseOrder
        //        {
        //            PODate = DateTime.Now,
        //            //UniversityID = university.UniversityID,
        //            UniversityName = university.UniversityName,
        //            UniversityAddress = university.Address,
        //            UniversityPhone = university.UniversityPhone,
        //            UniversityEmail = university.UniversityEmail,
        //            RequisitionNo = requisitionNo,
        //            ShipTo = viewModel.ShipTo,
        //            RequisitionedBy = university.UniversityName,
        //            WhenShip = viewModel.WhenShip,
        //            ShipVia = viewModel.ShipVia,
        //            FOBPoint = viewModel.FOBPoint,
        //            Terms = viewModel.Terms,
        //            //CopiesOfInvoice = model.CopiesOfInvoice,
        //            CopiesOfInvoice = viewModel.CopiesOfInvoice,
        //            AuthorizedBy = viewModel.AuthorizedBy,
        //            CreatedBy = userId.ToString(),
        //            CreatedDateTime = DateTime.Now,
        //            PurchaseDepartmentUploads = filePath,
        //            Status = "Draft"
        //        };

        //        _db.PurchaseOrders.Add(purchaseOrder);
        //        _db.SaveChanges(); // This generates PONumber (Identity)

        //        // Save Item Rows
        //        foreach (var item in viewModel.PurchaseOrderItems)
        //        {
        //            var poItem = new PurchaseOrderItem
        //            {
        //                PONumber = purchaseOrder.PONumber,
        //                QtyOrdered = item.QtyOrdered,
        //                QtyReceived = item.QtyReceived,
        //                Description = item.Description,
        //                UnitPrice = item.UnitPrice,
        //                Total = item.Total,
        //                VendorEmail = item.VendorEmail
        //            };
        //            _db.PurchaseOrderItems.Add(poItem);
        //            // 🔁 Update TempSelectedMaterials status for this item
        //            var tempMaterial = _db.TempSelectedMaterials
        //                                  .FirstOrDefault(t => t.UserID == userId &&
        //                                                       t.MaterialSubCategory == item.Description &&
        //                                                       t.Status == "New");

        //            if (tempMaterial != null)
        //            {
        //                tempMaterial.Status = "Sent";
        //                _db.Entry(tempMaterial).State = EntityState.Modified;
        //            }
        //        }

        //        _db.SaveChanges();
        //        TempData["Success"] = "PO Created Successfully!";
        //        TempData["VendorEmail"] = viewModel.VendorEmail;

        //        return RedirectToAction("InitiatePOEmail", new { PONumber = purchaseOrder.PONumber });
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "Error: " + ex.Message);
        //        return View(viewModel); // fallback
        //    }
        //}

        //public ActionResult InitiatePOEmail(int PONumber)
        //{
        //    string userId = (string)Session["UserID"];
        //    var user = _db.LocalPurchaseDepartments.FirstOrDefault(u => u.LocalID == userId);

        //    if (user == null)
        //        return HttpNotFound("Purchase department user not found.");

        //    string otp = new Random().Next(100000, 999999).ToString();
        //    Session["OTP"] = otp;
        //    Session["PONumberToSend"] = PONumber;
        //    Session["OTPAttempts"] = 0;

        //    //SendEmail(user.EmailID, "OTP for PO Email Confirmation", $"Your OTP is: {otp}");

        //    string toEmail = user.EmailID;
        //    string subject = $"Verification OTP for PO Email Confirmation – OTP:{otp}, PO No: {PONumber}";
        //    string body = $"Dear Purchase Department,\n\nPlease find attached the OTP  {otp}.\n\nRegards,\nICFAI ";

        //    Debug.WriteLine($"[DEBUG] Sending email to: {toEmail}");

        //    try
        //    {
        //        using (var message = new MailMessage())
        //        {
        //            message.From = new MailAddress("shivaupputuri5@gmail.com");
        //            message.To.Add(toEmail);
        //            message.Subject = subject;
        //            message.Body = body;
        //            //message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"PO_{PONumber}.pdf"));

        //            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
        //            {
        //                smtp.Credentials = new NetworkCredential("shivaupputuri5@gmail.com", "uxwvtphmvzhqqqpl");
        //                smtp.EnableSsl = true;

        //                smtp.Send(message);
        //                Debug.WriteLine("[DEBUG] Email sent successfully.");
        //            }
        //        }

        //        //TempData["OTP"] = true;
        //        TempData["PONumber"] = PONumber;

        //        return View("EnterOTP");
        //    }
        //    catch (Exception)
        //    {
        //        TempData["Error"] = "Failed to send PO.";
        //        return RedirectToAction("Materials"); // fallback
        //    }


        //}


        //[HttpPost]
        //public ActionResult VerifyOTPAndSendPO(string enteredOTP)
        //{
        //    string storedOTP = Session["OTP"]?.ToString();
        //    int attempts = (int)(Session["OTPAttempts"] ?? 0);

        //    if (storedOTP == enteredOTP)
        //    {
        //        int poNumber = (int)Session["PONumberToSend"];
        //        Session.Remove("OTP");
        //        Session.Remove("OTPAttempts");
        //        return RedirectToAction("SendMail", new { PONumber = poNumber });
        //    }

        //    attempts++;
        //    Session["OTPAttempts"] = attempts;

        //    if (attempts >= 3)
        //    {
        //        TempData["Error"] = "Failed to send PO: Too many incorrect OTP attempts.";
        //        return RedirectToAction("Materials");
        //    }

        //    ViewBag.Error = $"Invalid OTP. Attempt {attempts}/3.";
        //    return PartialView("_EnterOTPPartial");
        //}



        //public ActionResult SendMail(int PONumber)
        //{
        //    Debug.WriteLine($"[DEBUG] Starting SendMail for PO: {PONumber}");

        //    var po = _db.PurchaseOrders.FirstOrDefault(p => p.PONumber == PONumber);
        //    if (po == null)
        //    {
        //        Debug.WriteLine("[ERROR] Purchase Order not found.");
        //        return HttpNotFound("PO not found");
        //    }

        //    Debug.WriteLine("[DEBUG] Purchase Order found.");

        //    var items = _db.PurchaseOrderItems.Where(i => i.PONumber == PONumber).ToList();
        //    Debug.WriteLine($"[DEBUG] Retrieved {items.Count} purchase order items.");

        //    // Prepare ViewModel for PDF
        //    var model = new GeneratePOViewModel
        //    {
        //        PONumber = po.PONumber.ToString(),
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
        //        CopiesOfInvoice = (int)po.CopiesOfInvoice,
        //        AuthorizedBy = po.AuthorizedBy,
        //        //PurchaseDepartmentUploads = po.PurchaseDepartmentUploads,
        //        PurchaseOrderItems = items.Select(item => new PurchaseOrderItem
        //        {
        //            QtyOrdered = item.QtyOrdered ?? 0,
        //            QtyReceived = 0,
        //            Description = item.Description,
        //            UnitPrice = item.UnitPrice ?? 0,
        //            Total = (item.QtyOrdered ?? 0) * (item.UnitPrice ?? 0),
        //            VendorEmail = item.VendorEmail
        //        }).ToList()
        //    };
        //    Debug.WriteLine("[DEBUG] ViewModel for PDF created.");
        //    Debug.WriteLine($"[DEBUG] CopiesOfInvoice in ViewModel = {model.CopiesOfInvoice}");

        //    Debug.WriteLine("[DEBUG] ViewModel for PDF created.");

        //    // Generate PDF from View`````````````````````````````
        //    var pdf = new Rotativa.ViewAsPdf("POPDFView", model)
        //    {
        //        FileName = $"PurchaseOrder_{PONumber}.pdf"
        //    };

        //    Debug.WriteLine("[DEBUG] Generating PDF...");
        //    var pdfBytes = pdf.BuildFile(ControllerContext);
        //    Debug.WriteLine($"[DEBUG] PDF generated. Size: {pdfBytes.Length} bytes");

        //    // Email Details
        //    string toEmail = TempData["VendorEmail"]?.ToString() ?? "default@vendor.com";
        //    string subject = $"Purchase Order – PO No: {PONumber}";
        //    string body = $"Dear Vendor,\n\nPlease find attached the Purchase Order #{PONumber}.\n\nRegards,\nICFAI Procurement Team";

        //    Debug.WriteLine($"[DEBUG] Sending email to: {toEmail}");

        //    try
        //    {
        //        using (var message = new MailMessage())
        //        {
        //            message.From = new MailAddress("shivaupputuri5@gmail.com");
        //            message.To.Add(toEmail);
        //            message.Subject = subject;
        //            message.Body = body;
        //            message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"PO_{PONumber}.pdf"));

        //            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
        //            {
        //                smtp.Credentials = new NetworkCredential("shivaupputuri5@gmail.com", "uxwvtphmvzhqqqpl");
        //                smtp.EnableSsl = true;

        //                smtp.Send(message);
        //                Debug.WriteLine("[DEBUG] Email sent successfully.");
        //            }
        //        }

        //        TempData["ShowSuccessPopup"] = true;
        //        TempData["PONumber"] = PONumber;

        //        return View("SendMailConfirmation");
        //    }
        //    catch (Exception )
        //    {
        //        TempData["Error"] = "Failed to send PO.";
        //        return RedirectToAction("Materials"); // fallback
        //    }
        //}

        //public ActionResult VendorMaster()
        //{
        //    //ViewBag.AssetTypes = _db.AssetTypes.Select(a => a.AssetType1).Distinct().ToList();

        //    var assetTypes = _db.AssetTypes
        //         .Select(a => new SelectListItem
        //         {
        //             Value = a.AssetTypeID.ToString(),
        //             Text = a.AssetType1
        //         })
        //         .ToList();

        //    ViewBag.AssetType = new SelectList(assetTypes, "Value", "Text");
        //    ViewBag.MaterialCategories = new SelectList(new List<SelectListItem>());
        //    ViewBag.MaterialSubCategories = new SelectList(new List<SelectListItem>());

        //    return View();
        //}

        //public JsonResult GetCategoriesByAssetType(int assetTypeId)
        //{
        //    var categories = _db.MaterialCategories
        //        .Where(c => c.AssetTypeID == assetTypeId)
        //        .Select(c => new { MID = c.MID, MCategoryName = c.MaterialCategory1 })
        //        .ToList();

        //    return Json(categories, JsonRequestBehavior.AllowGet);
        //}

        //public JsonResult GetSubCategoriesByCategory(int categoryId)
        //{
        //    var subCategories = _db.MaterialSubCategories
        //        .Where(sc => sc.MID == categoryId)
        //        .Select(sc => new { MSubCategoryID = sc.MSubCategoryID, MSubCategoryName = sc.MaterialSubCategory1 })
        //        .ToList();

        //    return Json(subCategories, JsonRequestBehavior.AllowGet);
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(VendorDetailViewModel vm)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Step 1: Get UniversityID from Session
        //        string userId = Session["UserID"]?.ToString();
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            System.Diagnostics.Debug.WriteLine("[DEBUG] No UserID in session.");
        //            return View("VendorMaster");
        //        }

        //        var localPD = _db.LocalPurchaseDepartments.FirstOrDefault(x => x.LocalID == userId);
        //        if (localPD == null)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"[DEBUG] No LocalPurchaseDepartment found for UserID: {userId}");
        //            return View("VendorMaster");
        //        }

        //        int universityId = localPD.UniversityID;

        //        // Step 2: Parse MaterialsString: "Chair||18,Table||12.5"
        //        if (string.IsNullOrWhiteSpace(vm.MaterialsString))
        //        {
        //            TempData["ErrorMessage"] = "Please add at least one material with GST percentage.";
        //            return View("VendorMaster", vm);
        //        }

        //        string[] materialsArray = vm.MaterialsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        //        int newVendorId = 1;
        //        var lastVendor = _db.VendorDetails.OrderByDescending(v => v.VendorID).FirstOrDefault();
        //        if (lastVendor != null)
        //        {
        //            newVendorId = lastVendor.VendorID + 1 ??0;
        //        }

        //        foreach (var entry in materialsArray)
        //        {
        //            var parts = entry.Split(new[] { "||" }, StringSplitOptions.None);
        //            if (parts.Length != 2)
        //                continue;

        //            string material = parts[0].Trim();
        //            string gstPercent = parts[1].Trim();

        //            var vendor = new VendorDetail
        //            {
        //                VendorID = newVendorId++,
        //                VendorName = vm.VendorName,
        //                EmailID = vm.EmailID,
        //                PhoneNumber = vm.PhoneNumber,
        //                Material = material,               // Store only MaterialSubCategory
        //                GSTNO = vm.GSTNO,
        //                PanNumber = vm.PanNumber,
        //                Address = vm.Address,
        //                UniversityID = universityId,
        //                GSTPercentage = gstPercent
        //            };

        //            _db.VendorDetails.Add(vendor);
        //        }

        //        _db.SaveChanges();
        //        TempData["SuccessMessage"] = "Vendor details saved successfully.";
        //        return RedirectToAction("VendorMaster");
        //    }

        //    return View(vm);
        //}


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
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No UserID in session.");
                    return View("VendorMaster");
                }

                var localPD = _db.LocalPurchaseDepartments.FirstOrDefault(x => x.LocalID == userId);
                if (localPD == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No LocalPurchaseDepartment found for UserID: {userId}");
                    return View("VendorMaster");
                }

                int universityId = localPD.UniversityID;

                int newVendorId = 1;
                var lastVendor = _db.VendorDetails.OrderByDescending(v => v.VendorID).FirstOrDefault();
                if (lastVendor != null)
                {
                    newVendorId = lastVendor.VendorID + 1 ?? 0;
                }

                foreach (var material in model.Materials)
                {
                    var subCategory = _db.MaterialSubCategories.Find(material.MaterialSubCategoryID);

                    VendorDetail vendor = new VendorDetail
                    {
                        VendorID = newVendorId,
                        VendorName = model.VendorName,
                        EmailID = model.EmailID,
                        PhoneNumber = model.PhoneNumber,
                        GSTNO = model.GSTNO,
                        PanNumber = model.PanNumber,
                        UniversityID = universityId,
                        PricePerUnit = material.PricePerUnit,
                        Address = model.Address,
                        Material = subCategory?.MaterialSubCategory1, // Only MaterialSubCategory saved
                        GSTPercentage = material.GSTPercentage
                    };
                    _db.VendorDetails.Add(vendor);
                }

                _db.SaveChanges();
                TempData["Success"] = "Vendor saved successfully.";
                return RedirectToAction("VendorMaster");
            }

            // Reload dropdowns
            ViewBag.AssetTypes = _db.AssetTypes.ToList();
            ViewBag.Universities =  _db.Universities.ToList();
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




        //public ActionResult MyRequests()
        //{
        //    string userId = (string)Session["UserID"];

        //    // Get all POs created by this user
        //    var purchaseOrders = _db.PurchaseOrders
        //                            .Where(po => po.CreatedBy == userId.ToString())
        //                            .OrderByDescending(po => po.PODate)
        //                            .ToList();

        //    // Create a ViewModel list for each PO with its items
        //    var viewModel = purchaseOrders.Select(po => new PurchaseOrderGroupedViewModel
        //    {
        //        PONumber = po.PONumber,
        //        PODate = (DateTime)po.PODate,
        //        PurchaseOrderItems = _db.PurchaseOrderItems
        //                                .Where(item => item.PONumber == po.PONumber)
        //                                .ToList()
        //    }).ToList();

        //    return View(viewModel);
        //}
    }

}

