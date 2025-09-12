using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using PagedList;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class EmployeeController : BaseController
    {
        private readonly ASPEntities2 db = new ASPEntities2();

        public ActionResult EmployeeDashBoard()
        {
            return View();
        }

        [HttpGet]
        public ActionResult RaiseRequest()
        {
            Debug.WriteLine($"RaiseRequest method hit! Time: {DateTime.Now}");

            string userID = Session["UserID"] as string;
            string userRole = Session["UserRole"] as string;

            Debug.WriteLine($"Session UserID: {userID}, Role: {userRole}");

            if (string.IsNullOrEmpty(userID) || userRole != "Employee")
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("EmployeeDashboard");
            }

            var employee = db.Employees.FirstOrDefault(e => e.EmpID.ToString() == userID);
            if (employee == null)
            {
                Debug.WriteLine("Error: Employee not found in the database.");
                TempData["ErrorMessage"] = "Employee details not found.";
                return RedirectToAction("EmployeeDashboard");
            }

            ViewBag.EmpID = employee.EmpID;

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
                return RedirectToAction("EmployeeDashboard");
            }

            var employee = db.Employees.FirstOrDefault(e => e.EmpID == userID);
            if (employee == null)
            {
                Debug.WriteLine("❌ Error: Employee not found.");
                TempData["ErrorMessage"] = "Employee details not found.";
                return RedirectToAction("EmployeeDashboard");
            }

            var hod = db.HODs.FirstOrDefault(h => h.DepName == employee.DepName);
            if (hod == null)
            {
                Debug.WriteLine("❌ Error: No HOD found for department - " + employee.DepName);
                TempData["ErrorMessage"] = "No HOD assigned to your department.";
                return RedirectToAction("EmployeeDashboard");
            }

            try
            {
                List<Request> requestList = new List<Request>();

                foreach (var item in model)
                {
                    int requestID = GenerateUniqueRequestID(); // Move inside loop

                    Debug.WriteLine($"✅ Generated Request ID: {requestID} for {item.AssetType}, {item.MaterialCategory}, {item.MSubCategory}");

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

                    Request newRequest = new Request
                    {
                        RequestID = requestID,  // Each item gets a unique ID
                        AssetType = assetTypeName,
                        MaterialCategory = categoryName,
                        MSubCategory = subCatName,
                        EmpID = employee.EmpID,
                        AvailableQuantity = availableQuantity,
                        RequestDate = DateTime.Now,
                        Status = "New",
                        ApprovedQuantity = 0,
                        IssuingQuantity = 0,
                        PendingQuantity = 0,
                        HODID = hod.HODID,
                        StoreAdminID = "",
                        RequestingQuantity = item.RequestingQuantity
                    };

                    requestList.Add(newRequest);
                    Debug.WriteLine($"🔹 Added Request ID {newRequest.RequestID} for Asset: {newRequest.AssetType}");
                }

                if (requestList.Count == 0)
                {
                    Debug.WriteLine("❌ No valid requests were created.");
                    TempData["ErrorMessage"] = "Error processing request data.";
                    return RedirectToAction("RaiseRequest");
                }

                db.Requests.AddRange(requestList);
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
                return db.Requests.Any() ? db.Requests.Max(r => (int?)r.RequestID).GetValueOrDefault() + 1 : 1;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error generating RequestID: " + ex.Message);
                return 1;
            }
        }

        public ActionResult MyRequests(int? page, DateTime? fromDate, DateTime? toDate)
        {
            string userID = Session["UserID"] as string;

            if (string.IsNullOrEmpty(userID))
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("EmployeeDashboard");
            }

            var employee = db.Employees.FirstOrDefault(h => h.EmpID.ToString() == userID);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee details not found.";
                return RedirectToAction("EmployeeDashboard");
            }

            string empID = employee.EmpID;
            var myrequests = db.Requests
                .Where(r => r.EmpID == empID)
                .OrderByDescending(r => r.RequestDate)
                .ToList()
                .GroupBy(r => r.RequestID)
                .Select((group, index) => new RequestGroupedViewModel
                {
                    SNo = index + 1,
                    RequestID = group.Key ?? 0,
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
                        IssuingQuantity = r.IssuingQuantity ?? 0,
                        PendingQuantity = r.PendingQuantity ?? 0,
                        Remarks = r.Remarks
                    }).ToList()
                }).ToList();

            int pageSize = 9;
            int pageNumber = page ?? 1;
            if (fromDate.HasValue)
            {
                myrequests = myrequests.Where(r => r.RequestDate >= fromDate.Value.Date).ToList();
            }

            if (toDate.HasValue)
            {
                DateTime inclusiveToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                myrequests = myrequests.Where(r => r.RequestDate <= inclusiveToDate).ToList();
            }

            return View(myrequests.ToPagedList(pageNumber, pageSize));
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
