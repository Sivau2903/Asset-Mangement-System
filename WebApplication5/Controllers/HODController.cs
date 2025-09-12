using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class HODController : BaseController
    {
        private readonly ASPEntities2 db = new ASPEntities2();

        public ActionResult Home()
        {
            return View();
        }

        private List<Request> pagedRequest;

        public ActionResult HODDashBoard(DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var userID = Session["UserID"] as string;
            var userRole = Session["UserRole"] as string;

            if (string.IsNullOrEmpty(userID) || userRole != "HOD")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("HODDashBoard");
            }

            var hod = db.HODs.FirstOrDefault(e => e.HODID.ToString() == userID);
            if (hod == null)
            {
                TempData["ErrorMessage"] = "HOD details not found.";
                return RedirectToAction("HODDashBoard");
            }

            // Fetching and grouping requests
            var query = db.Requests
                .Where(r => r.HODID == hod.HODID && r.Status == "New")
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
                        PendingQuantity = r.PendingQuantity ?? 0,
                    }).ToList()
                })
                .ToList();

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.RequestDate <= inclusiveToDate).ToList();
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
                Requests = pagedRequests,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalRequests
            };

            return View(model);
        }


        public ActionResult GetPastRequests(string empId)
        {
            var pastRequests = db.Requests.Where(r => r.EmpID == empId).ToList();

            if (pastRequests == null || pastRequests.Count == 0)
            {
                TempData["ErrorMessage"] = "No past requests found for this employee.";
                return new EmptyResult();
            }

            Debug.WriteLine($"Total Requests Found: {pastRequests.Count}");
            var last10Requests = pastRequests.Take(7).ToList();
            return PartialView("_PastRequests", last10Requests);
        }

        [HttpPost]
        public ActionResult UpdateRequest(int requestId, string msubCategory, int? approvingQuantity, string actionType, string remarks)
        {
            Console.WriteLine($"Received UpdateRequest Call: {requestId}, {msubCategory}, {approvingQuantity}, {actionType}, Remarks: {remarks}");

            if (!Request.IsAjaxRequest())
            {
                return Json(new { success = false, message = "Request is not AJAX." });
            }

            try
            {
                var userID = Session["UserID"] as string;
                var userRole = Session["UserRole"] as string;

                if (string.IsNullOrEmpty(userID) || userRole != "HOD")
                {
                    return Json(new { success = false, message = "Unauthorized access." });
                }

                // ✅ Fetch the HOD's UniversityID
                var hod = db.HODs.FirstOrDefault(e => e.HODID.ToString() == userID);
                if (hod == null)
                {
                    return Json(new { success = false, message = "HOD details not found." });
                }

                var universityID = hod.UniversityID;

                // ✅ Find the corresponding Store Admin with the same UniversityID
                var storeAdmin = db.StoreAdmins.FirstOrDefault(e => e.UniversityID == universityID );

                if (storeAdmin == null)
                {
                    return Json(new { success = false, message = "No Store Admin found for this University." });
                }

                var storeAdminID = storeAdmin.StoreAdminID; // ✅ Store Admin ID fetched

                var existingRequest = db.Requests.FirstOrDefault(r => r.RequestID == requestId && r.MSubCategory == msubCategory);
                if (existingRequest == null)
                {
                    return Json(new { success = false, message = "Request not found." });
                }

                // Perform updates based on action type
                if (actionType != null)
                {
                    switch (actionType.ToLower())
                    {
                        case "approve":
                            if (approvingQuantity == null || approvingQuantity < 1)
                            {
                                return Json(new { success = false, message = "Approving quantity is required and must be at least 1." });
                            }

                            if (approvingQuantity > existingRequest.RequestingQuantity)
                            {
                                return Json(new { success = false, message = "Approving quantity cannot exceed requesting quantity." });
                            }

                            existingRequest.ApprovedQuantity = approvingQuantity.Value;
                            existingRequest.StoreAdminID = storeAdminID; // ✅ Save StoreAdminID in Request table

                            // Set status & PendingQuantity
                            if (approvingQuantity == existingRequest.RequestingQuantity)
                            {
                                existingRequest.Status = "Approved"; // Fully approved
                                existingRequest.PendingQuantity = 0;
                            }
                            else
                            {
                                existingRequest.Status = "Ongoing"; // Partial approval
                                existingRequest.PendingQuantity = existingRequest.RequestingQuantity - approvingQuantity.Value;
                            }
                            break;

                        case "reject":
                            existingRequest.Status = "Rejected";
                            existingRequest.Remarks = remarks ?? "Rejected"; // Default remark if null
                            break;

                        default:
                            return Json(new { success = false, message = "Invalid action type received." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Action type is null." });
                }

                db.SaveChanges();

                return Json(new { success = true, message = $"Request {actionType}d successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating request.", error = ex.Message });
            }
        }



        // Add this at the top

        [HttpGet]
        public ActionResult EmployeeRequests(DateTime? fromDate, DateTime? toDate)
        {
            // Validate HOD
            string userID = Session["UserID"] as string;
            string userRole = Session["UserRole"] as string;
            if (string.IsNullOrEmpty(userID) || userRole != "HOD")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("HODDashboard");
            }

            // Find HOD
            var hod = db.HODs.FirstOrDefault(h => h.HODID.ToString() == userID);
            if (hod == null)
            {
                TempData["ErrorMessage"] = "HOD details not found.";
                return RedirectToAction("HODDashboard");
            }

            // Base query: requests for this HOD excluding "New" status
            var allRequests = db.Requests
                .Where(r => r.HODID == hod.HODID && r.Status != "New");

            if (fromDate.HasValue)
            {
                allRequests = allRequests.Where(r => r.RequestDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                allRequests = allRequests.Where(r => r.RequestDate <= inclusiveToDate);
            }

            // Sort each status group descending by date
            var ongoing = allRequests
                .Where(r => r.Status == "Ongoing")
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            var approved = allRequests
                .Where(r => r.Status == "Approved")
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            var rejected = allRequests
                .Where(r => r.Status == "Rejected")
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            var model = new HODViewRequestsViewModel
            {
                OngoingRequests = ongoing,
                ApprovedRequests = approved,
                RejectedRequests = rejected
            };

            return View(model);
        }



        // GET: HOD
        [HttpGet]
        public ActionResult RaiseRequest()
        {
            Debug.WriteLine($"RaiseRequest method hit! Time: {DateTime.Now}");

            string userID = Session["UserID"] as string;
            string userRole = Session["UserRole"] as string;

            Debug.WriteLine($"Session UserID: {userID}, Role: {userRole}");

            if (string.IsNullOrEmpty(userID) || userRole != "HOD")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("HODDashBoard");
            }

            var hod = db.HODs.FirstOrDefault(e => e.HODID.ToString() == userID);
            if (hod == null)
            {
                Debug.WriteLine("Error: HOD not found in the database.");
                TempData["ErrorMessage"] = "HOD details not found.";
                return RedirectToAction("HODDashBoard");
            }

            ViewBag.HODID = hod.HODID;

            var assetTypes = db.AssetTypes
                .Select(a => new SelectListItem
                {
                    Value = a.AssetTypeID.ToString(),
                    Text = a.AssetType1
                })
                .ToList();

            ViewBag.AssetType = new SelectList(assetTypes, "Value", "Text");
            ViewBag.MaterialCategories = new SelectList(new List<SelectListItem>());
            ViewBag.MaterialSubCategories = new SelectList(new List<SelectListItem>());

            return View();
        }

        public JsonResult GetCategoriesByAssetType(int assetTypeId)
        {
            var categories = db.MaterialCategories
                .Where(c => c.AssetTypeID == assetTypeId)
                .Select(c => new { MID = c.MID, MCategoryName = c.MaterialCategory1 })
                .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSubCategoriesByCategory(int categoryId)
        {
            var subCategories = db.MaterialSubCategories
                .Where(sc => sc.MID == categoryId)
                .Select(sc => new { MSubCategoryID = sc.MSubCategoryID, MSubCategoryName = sc.MaterialSubCategory1 })
                .ToList();

            return Json(subCategories, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAvailableQuantity(int assetTypeId, int categoryId, int subCategoryId)
        {
            try
            {
                string assetTypeName = db.AssetTypes.Where(a => a.AssetTypeID == assetTypeId).Select(a => a.AssetType1).FirstOrDefault();
                string categoryName = db.MaterialCategories.Where(c => c.MID == categoryId).Select(c => c.MaterialCategory1).FirstOrDefault();
                string subCategoryName = db.MaterialSubCategories.Where(s => s.MSubCategoryID == subCategoryId).Select(s => s.MaterialSubCategory1).FirstOrDefault();

                var masterItem = db.MaterialMasterLists.FirstOrDefault(m =>
                    m.MaterialCategory == categoryName &&
                    m.AssetType == assetTypeName &&
                    m.MaterialSubCategory == subCategoryName
                );

                return Json(new { success = true, AvailableQuantity = masterItem?.AvailableQuantity ?? 0 }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {ex.Message}");
                return Json(new { success = false, message = "Error retrieving data." }, JsonRequestBehavior.AllowGet);
            }
        }

        //public PartialViewResult RequestMaterial()
        //{
        //    return PartialView("_hodRequestMaterial");
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public JsonResult SubmitRequestMaterial(string assetType, string materialCategory, string materialSubCategory, int requestingQuantity)
        //{
        //    if (string.IsNullOrEmpty(assetType) || string.IsNullOrEmpty(materialCategory) || string.IsNullOrEmpty(materialSubCategory) || requestingQuantity <= 0)
        //    {
        //        return Json(new { success = false, message = "Invalid input data!" });
        //    }

        //    string userID = Session["UserID"] as string;
        //    string userRole = Session["UserRole"] as string;

        //    Debug.WriteLine($"Session UserID: {userID}, Role: {userRole}");

        //    if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userRole))
        //    {
        //        return Json(new { success = false, message = "Unauthorized access!" });
        //    }

        //    int storeAdminID = 0;

        //    if (userRole == "Employee")
        //    {
        //        var employee = db.Employees.FirstOrDefault(e => e.EmpID.ToString() == userID);
        //        if (employee == null)
        //        {
        //            return Json(new { success = false, message = "Employee not found!" });
        //        }

        //        // Fetch StoreAdminID by matching UniversityID
        //        storeAdminID = db.StoreAdmins
        //                        .Where(sa => sa.UniversityID == employee.UniversityID)
        //                        .Select(sa => int.Parse(sa.StoreAdminID))
        //                        .FirstOrDefault();
        //    }
        //    else if (userRole == "HOD")
        //    {
        //        var hod = db.HODs.FirstOrDefault(h => h.HODID.ToString() == userID);
        //        if (hod == null)
        //        {
        //            return Json(new { success = false, message = "HOD not found!" });
        //        }

        //        // Fetch StoreAdminID by matching UniversityID
        //        storeAdminID = db.StoreAdmins
        //                        .Where(sa => sa.UniversityID == hod.UniversityID)
        //                        .Select(sa => int.Parse(sa.StoreAdminID))
        //                        .FirstOrDefault();
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = "Invalid role!" });
        //    }

        //    RequiredMaterial newRequest = new RequiredMaterial
        //    {
        //        AssetType = assetType,
        //        MaterialCategory = materialCategory,
        //        MaterialSubCategory = materialSubCategory,
        //        RequiredQuantity = requestingQuantity,
        //        UserID = int.Parse(userID),
        //        Role = userRole,
        //        StoreAdminID = storeAdminID,
        //        RequestedDate = DateTime.Now,
        //        Status = "New"
        //    };

        //    db.RequiredMaterials.Add(newRequest);
        //    db.SaveChanges();


        //    return Json(new { success = true, message = "Material request submitted successfully!" });
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RaiseRequest(List<Request> model)
        {
            Debug.WriteLine($"🔹 Model Count: {(model?.Count ?? 0)}");

            if (model == null || !model.Any())
            {
                Debug.WriteLine("❌ No request items provided.");
                TempData["ErrorMessage"] = "No request items provided.";
                return RedirectToAction("RaiseRequest");
            }

            string userID = Session["UserID"] as string;
            if (string.IsNullOrEmpty(userID))
            {
                Debug.WriteLine("❌ Unauthorized access - User ID not found.");
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("HODDashBoard");
            }

            var hod = db.HODs.FirstOrDefault(e => e.HODID.ToString() == userID);
            if (hod == null)
            {
                Debug.WriteLine("❌ Error: Hod not found.");
                TempData["ErrorMessage"] = "hod details not found.";
                return RedirectToAction("HODDashBoard");
            }

            var storeadmin = db.StoreAdmins.FirstOrDefault(sa => sa.UniversityID == hod.UniversityID);
            if (hod == null)
            {
                Debug.WriteLine("❌ Error: No HOD found for department - " + hod.DepName);
                TempData["ErrorMessage"] = "No HOD assigned to your department.";
                return RedirectToAction("HODDashBoard");
            }

            try
            {
                List<HODRequest> hodrequestList = new List<HODRequest>();

                foreach (var item in model)
                {
                    int hodrequestID = GenerateUniqueRequestID(); // Move inside loop

                    Debug.WriteLine($"✅ Generated Request ID: {hodrequestID} for {item.AssetType}, {item.MaterialCategory}, {item.MSubCategory}");

                    if (!int.TryParse(item.AssetType, out int assetTypeId) ||
                        !int.TryParse(item.MaterialCategory, out int categoryId) ||
                        !int.TryParse(item.MSubCategory, out int subCatId))
                    {
                        Debug.WriteLine("❌ Error parsing AssetType, MaterialCategory, or MSubCategory IDs.");
                        continue;
                    }

                    var assetTypeName = db.AssetTypes?.Where(a => a.AssetTypeID == assetTypeId)?.Select(a => a.AssetType1)?.FirstOrDefault();
                    var categoryName = db.MaterialCategories?.Where(c => c.MID == categoryId)?.Select(c => c.MaterialCategory1)?.FirstOrDefault();
                    var subCatName = db.MaterialSubCategories?.Where(sc => sc.MSubCategoryID == subCatId)?.Select(sc => sc.MaterialSubCategory1)?.FirstOrDefault();

                    if (string.IsNullOrEmpty(assetTypeName) || string.IsNullOrEmpty(categoryName) || string.IsNullOrEmpty(subCatName))
                    {
                        Debug.WriteLine("❌ Error: Could not fetch names for AssetType, MaterialCategory, or MSubCategory.");
                        continue;
                    }

                    var masterItem = db.MaterialMasterLists?.FirstOrDefault(m =>
                        m.MaterialCategory == categoryName &&
                        m.AssetType == assetTypeName &&
                        m.MaterialSubCategory == subCatName);

                    int availableQuantity = masterItem?.AvailableQuantity ?? 0;
                    Debug.WriteLine($"🔹 Available Quantity: {availableQuantity}");

                    HODRequest newhodRequest = new HODRequest
                    {
                        HODRequestID = hodrequestID,  // Each item gets a unique ID
                        AssetType = assetTypeName,
                        MaterialCategory = categoryName,
                        MSubCategory = subCatName,
                        HODID = hod.HODID,
                        AvailableQuantity = availableQuantity,
                        RequestedDate = DateTime.Now,
                        Status = "New",
                       
                        IssuingQuantity = 0,
                        PendingQuantity = 0.ToString(),
                        StoreAdminID = storeadmin.StoreAdminID,
                       
                        RequestingQuantity = item.RequestingQuantity
                    };

                    hodrequestList.Add(newhodRequest);
                    Debug.WriteLine($"🔹 Added Request ID {newhodRequest.HODRequestID} for Asset: {newhodRequest.AssetType}");
                }

                if (hodrequestList.Count == 0)
                {
                    Debug.WriteLine("❌ No valid requests were created.");
                    TempData["ErrorMessage"] = "Error processing request data.";
                    return RedirectToAction("RaiseRequest");
                }

                db.HODRequests.AddRange(hodrequestList);
                int recordsSaved = db.SaveChanges();
                Debug.WriteLine($"✅ Requests saved successfully. Records inserted: {recordsSaved}");

                TempData["SuccessMessage"] = "Requests submitted successfully!";
                ModelState.Clear();
                return RedirectToAction("RaiseRequest");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("❌ Error saving request: " + ex.Message);
                TempData["ErrorMessage"] = "An error occurred while saving the requests.";
                return RedirectToAction("RaiseRequest");
            }

        }


        private int GenerateUniqueRequestID()
        {
            try
            {
                return db.HODRequests.Any() ? db.HODRequests.Max(r => (int?)r.HODRequestID).GetValueOrDefault() + 1 : 1;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error generating HODRequestID: " + ex.Message);
                return 1;
            }
        }
        public ActionResult MyRequests(DateTime? fromDate, DateTime? toDate)
        {
            string userID = Session["UserID"] as string;

            if (string.IsNullOrEmpty(userID))
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("HODDashBoard");
            }

            var hod = db.HODs.FirstOrDefault(e => e.HODID.ToString() == userID);
            if (hod == null)
            {
                TempData["ErrorMessage"] = "HOD details not found.";
                return RedirectToAction("HODDashBoard");
            }

            // Grouping requests by HODRequestID
            var myrequests = db.HODRequests
                .Where(r => r.HODID == hod.HODID)
                .OrderByDescending(r => r.RequestedDate)
                .ToList()
                .GroupBy(r => r.HODRequestID) // Group by RequestID
                .Select((group, index) => new HODRequestGroupedViewModel  // Use Grouped ViewModel
                { 
                SNo = index + 1,
                    HODRequestID = group.Key ?? 0,
                    RequestedDate = group.First().RequestedDate ?? DateTime.Now,
                    Status = group.First().Status ?? "Unknown",
                    AssetDetails = group.Select(r => new HODRequestViewModel
                    {
                        AssetType = r.AssetType ?? "N/A",
                        MaterialCategory = r.MaterialCategory ?? "N/A",
                        MSubCategory = r.MSubCategory ?? "N/A",
                        AvailableQuantity = r.AvailableQuantity ?? 0,
                        RequestingQuantity = r.RequestingQuantity,
                        IssuingQuantity = r.IssuingQuantity ?? 0,
                        PendingQuantity = r.PendingQuantity != null ? Convert.ToInt32(r.PendingQuantity) : 0,
                    }).ToList()
                })
                .ToList();

            if (fromDate.HasValue)
            {
                myrequests = myrequests.Where(r => r.RequestedDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                myrequests = myrequests.Where(r => r.RequestedDate <= inclusiveToDate).ToList();
            }

            return View(myrequests);
        }




        public ActionResult EmployeeRequests()
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

    }
}