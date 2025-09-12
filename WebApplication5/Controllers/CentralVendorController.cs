//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;
//using System.Web.Routing;
//using WebApplication5.Models;

//namespace WebApplication5.Controllers
//{
//    public class CentralVendorController : Controller
//    {
//        private readonly ASPEntities2 _db = new ASPEntities2();
//        // GET: CentralVendor
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

//        public ActionResult CVDashBoard()
//        {
//            return View();
//        }

//        public ActionResult RequestRecevied()
//        {
//            int vendorID = Convert.ToInt32(Session["UserID"]);
//            var raisedRequests = _db.CentralSentRequests
//                .Where(r => r.VendorID == vendorID && r.Status == "Sent to Vendor" || r.Status == "Pending")
//                .OrderByDescending(r => r.OrderedDate) // Sort by Requested Date (Latest First)
//                .ToList();

//            return View(raisedRequests);
//        }

//        [HttpPost]
//        public JsonResult UpdateIssuingQuantity(int requestId, int issuingQuantity)
//        {
//            try
//            {
//                var request = _db.CentralSentRequests.FirstOrDefault(r => r.RequestID == requestId);
//                if (request == null)
//                {
//                    return Json(new { success = false, message = "Request not found." });
//                }

//                // Validations
//                if (issuingQuantity < 1)
//                {
//                    return Json(new { success = false, message = "Issuing Quantity must be at least 1." });
//                }

//                if (issuingQuantity > request.OrderedQuantity)
//                {
//                    return Json(new { success = false, message = "Issuing Quantity cannot exceed Ordered Quantity." });
//                }

//                // Update quantity
//                request.IssuingQuantity = issuingQuantity;
//                request.IssuedDate = DateTime.Now;

//                // Determine status
//                if (issuingQuantity == request.OrderedQuantity)
//                    request.Status = "Issued";
//                else
//                    request.Status = "Pending";

//                // Update Status in related tables
//                var requestingMaterial = _db.RequestingMaterials.FirstOrDefault(r => r.RequestID == requestId);
//                if (requestingMaterial != null)
//                {
//                    requestingMaterial.Status = request.Status;
//                }

//                var requiredMaterial = _db.RequiredMaterials.FirstOrDefault(r => r.RequiredMaterialID == requestId);
//                if (requiredMaterial != null)
//                {
//                    requiredMaterial.Status = request.Status;
//                }

//                _db.SaveChanges();

//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult RejectRequest(int requestId, string remarks)
//        {
//            try
//            {
//                var request = _db.CentralSentRequests.FirstOrDefault(r => r.RequestID == requestId);
//                if (request == null)
//                {
//                    return Json(new { success = false, message = "Request not found." });
//                }

//                request.Status = "Rejected";
//                request.Remarks = remarks;
//                request.IssuedDate = DateTime.Now;

//                // Update related tables
//                var requestingMaterial = _db.RequestingMaterials.FirstOrDefault(r => r.RequestID == requestId);
//                if (requestingMaterial != null)
//                {
//                    requestingMaterial.Status = request.Status;
//                }

//                var requiredMaterial = _db.RequiredMaterials.FirstOrDefault(r => r.RequiredMaterialID == requestId);
//                if (requiredMaterial != null)
//                {
//                    requiredMaterial.Status = request.Status;
//                }

//                _db.SaveChanges();

//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }
//        public ActionResult SentMaterials()
//        {
//            int purchaseDepartmentID = Convert.ToInt32(Session["UserID"]);

//            var raisedRequests = (from request in _db.CentralSentRequests
//                                  where request.VendorID == purchaseDepartmentID
//                                  orderby request.OrderedDate descending
//                                  select request).ToList();

//            return View(raisedRequests);
//        }

//        public ActionResult SubmittoAccountant()
//        {
//            return View();
//        }

//        public ActionResult Update()
//        {
//            return View();
//        }

//    }
//}