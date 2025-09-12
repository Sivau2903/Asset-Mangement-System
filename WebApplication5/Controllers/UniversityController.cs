//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Diagnostics;
//using System.Linq;
//using System.Net;
//using System.Web;
//using System.Web.Mvc;
//using System.Web.Routing;
//using WebApplication5.Models;

//namespace WebApplication5.Controllers
//{
//    public class UniversityController : Controller
//    {
//        private readonly ASPEntities _db = new ASPEntities();
//        // GET: University
//        public ActionResult Home()
//        {
//            return View();
//        }

//        public ActionResult ReceviedRequests()
//        {
//            int universityID = Convert.ToInt32(Session["UserID"]);
//            var raisedRequests = _db.RequestingMaterials
//                .Where(r => r.UniversityID == universityID && r.Status == "Raised")
//                .OrderByDescending(r => r.RequestedDate) // Sort by Requested Date (Latest First)
//                .ToList();
//            return View(raisedRequests);
//        }

//        [HttpPost]
//        public ActionResult UpdateReceviedRequests(int requestId, string msubCategory, string actionType, string remarks)
//        {
//            Console.WriteLine($"[DEBUG] Received UpdateReceviedRequests Call: RequestID={requestId}, MSubCategory={msubCategory}, ActionType={actionType}, Remarks={remarks}");

//            if (!Request.IsAjaxRequest())
//            {
//                Console.WriteLine("[ERROR] Request is not AJAX.");
//                return Json(new { success = false, message = "Request is not AJAX." });
//            }

//            try
//            {
//                var userID = Session["UserID"] as string;
//                var userRole = Session["UserRole"] as string;
//                //var universityID = Session["UniversityID"] as string; // Get UniversityID from session

//                Console.WriteLine($"[DEBUG] UserID: {userID}, UserRole: {userRole}");

//                if (string.IsNullOrEmpty(userID) || userRole != "SuperAdmin")
//                {
//                    Console.WriteLine("[ERROR] Unauthorized access detected.");
//                    return Json(new { success = false, message = "Unauthorized access." });
//                }

//                var existingRequest = _db.RequestingMaterials
//                    .FirstOrDefault(r => r.RequestID == requestId && r.MaterialSubCategory.ToLower() == msubCategory.ToLower());

//                if (existingRequest == null)
//                {
//                    Console.WriteLine("[ERROR] Request not found.");
//                    return Json(new { success = false, message = "Request not found." });
//                }

//                Console.WriteLine($"[DEBUG] Found request: RequestID={existingRequest.RequestID}, Current Status={existingRequest.Status}");

//                if (actionType != null)
//                {
//                    switch (actionType.ToLower())
//                    {
//                        case "approve":
//                            Console.WriteLine("[DEBUG] Approving request...");
//                            existingRequest.Status = "Approve";
//                            existingRequest.Remarks = null; // Clear remarks if approved

//                            // Fetch PDNo from MaterialSubCategory table
//                            var materialSubCategory = _db.MaterialSubCategories
//                                .FirstOrDefault(m => m.MaterialSubCategory1.ToLower() == msubCategory.ToLower());

//                            if (materialSubCategory == null)
//                            {
//                                Console.WriteLine("[ERROR] MaterialSubCategory not found.");
//                                return Json(new { success = false, message = "MaterialSubCategory not found." });
//                            }

//                            //int pdNo = materialSubCategory.PDNo??0;
//                            Console.WriteLine($"[DEBUG] Found PDNo: {pdNo}");

//                            if (pdNo == 1)
//                            {
//                                Console.WriteLine("[DEBUG] Sending to LocalPurchaseDepartment...");

//                                // Fetch LocalPurchaseDepartmentID using UniversityID
//                                int userUniversityID = Convert.ToInt32(Session["UserID"]);

//                                var localPurchaseDepartment = _db.LocalPurchaseDepartments
//                                    .FirstOrDefault(lpd => lpd.UniversityID == userUniversityID);

//                                if (localPurchaseDepartment == null)
//                                {
//                                    Console.WriteLine("[ERROR] No LocalPurchaseDepartment found for this UniversityID.");
//                                    return Json(new { success = false, message = "LocalPurchaseDepartment not found for this university." });
//                                }

//                                int localPurchaseDepartmentID = localPurchaseDepartment.LocalID;
//                                Console.WriteLine($"[DEBUG] Found LocalPurchaseDepartmentID: {localPurchaseDepartmentID}");

//                                // ✅ Fetch University Address from Universities table
//                                var university = _db.Universities.FirstOrDefault(u => u.UniversityId == userUniversityID);
//                                string universityAddress = university != null ? university.Address : "-";
//                                Console.WriteLine($"[DEBUG] Fetched University Address: {universityAddress}");

//                                var localPurchaseEntry = new InsertLocalPurchaseDepartment
//                                {
//                                    RequestID = existingRequest.RequestID,
//                                    MaterialSubCategory = existingRequest.MaterialSubCategory,
//                                    RequestingQuantity = existingRequest.RequestingQuantity,
//                                    Status = "Pending",
//                                    LocalPurchaseDepartmentID = localPurchaseDepartmentID, // Assign the fetched ID
//                                    UniversityID = userUniversityID,
//                                    RequestedDate = DateTime.Now,
//                                    Address = universityAddress  // ✅ Save address to the table
//                                };

//                                _db.InsertLocalPurchaseDepartments.Add(localPurchaseEntry);
//                                Console.WriteLine("[SUCCESS] Request added to LocalPurchaseDepartment.");
//                            }

//                            //else if (pdNo == 2)
//                            {
//                                Console.WriteLine("[DEBUG] Sending to CentralPurchaseDepartment...");

//                                var centralPurchaseEntry = new InsertCentralPurchaseDepartment
//                                {
//                                    RequestID = existingRequest.RequestID,
//                                    MaterialSubCategory = existingRequest.MaterialSubCategory,
//                                    RequestingQuantity = existingRequest.RequestingQuantity,
//                                    Status = "Pending",
//                                    CPDID = 1, // CentralPurchaseDepartment ID is always 1
//                                    Address = "ICFAI Group,CIT,Punjagutta,Hyderabad",
//                                    RequestedDate = DateTime.Now
//                                };

//                                _db.InsertCentralPurchaseDepartments.Add(centralPurchaseEntry);
//                                Console.WriteLine("[SUCCESS] Request added to CentralPurchaseDepartment.");
//                            }
//                            else
//                            {
//                                Console.WriteLine("[ERROR] Invalid PDNo detected.");
//                                return Json(new { success = false, message = "Invalid PDNo value." });
//                            }

//                            break;

//                        case "reject":
//                            Console.WriteLine("[DEBUG] Rejecting request with remarks...");
//                            existingRequest.Status = "Rejected";
//                            existingRequest.Remarks = string.IsNullOrEmpty(remarks) ? "Rejected" : remarks;
//                            break;

//                        default:
//                            Console.WriteLine("[ERROR] Invalid action type received.");
//                            return Json(new { success = false, message = "Invalid action type received." });
//                    }
//                }
//                else
//                {
//                    Console.WriteLine("[ERROR] Action type is null.");
//                    return Json(new { success = false, message = "Action type is null." });
//                }

//                Console.WriteLine($"[DEBUG] Saving changes to database... New Status={existingRequest.Status}");

//                _db.Entry(existingRequest).State = EntityState.Modified;
//                _db.SaveChanges();

//                Console.WriteLine($"[SUCCESS] Request {actionType}d successfully.");
//                return Json(new { success = true, message = $"Request {actionType}d successfully." });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[ERROR] Exception occurred: {ex.Message}");
//                return Json(new { success = false, message = "Error updating request.", error = ex.Message });
//            }
//        }


//        protected override void OnActionExecuting(ActionExecutingContext filterContext)
//        {
//            if (Session["UserID"] == null) // Check if session exists
//            {
//                filterContext.Result = new RedirectToRouteResult(
//                    new RouteValueDictionary(new { controller = "Login", action = "Loginpage" })
//                );
//            }
//            base.OnActionExecuting(filterContext);
//        }


//        public ActionResult MyRequests()
//        {
//            int universityID = Convert.ToInt32(Session["UserID"]);
//            var raisedRequests = _db.RequestingMaterials
//                .Where(r => r.UniversityID == universityID && r.Status == "Approve")
//                .OrderByDescending(r => r.RequestedDate) // Sort by Requested Date (Latest First)
//                .ToList();
//            return View(raisedRequests);
//        }

//        public ActionResult GetPastRequests(int storeadminId)
//        {
//            var pastRequests = _db.RequestingMaterials.Where(r => r.StoreAdminID == storeadminId).ToList();

//            if (pastRequests == null || pastRequests.Count == 0)
//            {
//                TempData["ErrorMessage"] = "No past requests found for this employee.";
//                return new EmptyResult();
//            }

//            Debug.WriteLine($"Total Requests Found: {pastRequests.Count}");
//            var last10Requests = pastRequests.Take(7).ToList();
//            return PartialView("_PastRequests", last10Requests);
//        }
//    }
//}