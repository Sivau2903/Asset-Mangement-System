using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class NodelOfficerController : Controller
    {
        private readonly ASPEntities2 db = new ASPEntities2();

        // ================================
        // Dashboard
        // ================================
        public ActionResult Dashboard()
        {
            var model = new NodeOfficerDashboardViewModel
            {
                TotalAssets = db.AssetQuantities.Count(),
                AssetsWithQRCode = db.AssetQuantities.Count(a => (bool)a.IsQRIssued),
                ExpiredAssets = db.AssetQuantities.Count(a => (bool)a.IsExpired)
            };

            return View(model);
        }

        // ================================
        // GET: Add Asset
        // ================================
        public ActionResult AddAsset()
        {
            return View();
        }

        // ================================
        // POST: Add Asset
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAsset(AssetMaster model, AssetQuantity qtyModel)
        {
            if (ModelState.IsValid)
            {
                // Check if AssetName already exists in Master
                var existingAsset = db.AssetMasters.FirstOrDefault(a => a.AssetName == model.AssetName);

                if (existingAsset == null)
                {
                    // 1️⃣ Create a new Master record
                    model.CreatedDate = DateTime.Now;
                    model.Quantity = 1; // since we are adding first quantity
                    db.AssetMasters.Add(model);
                    db.SaveChanges();

                    // 2️⃣ Add into AssetQuantities
                    qtyModel.AssetID = model.AssetID;
                    qtyModel.IsQRIssued = false;
                    qtyModel.IsExpired = false;
                    db.AssetQuantities.Add(qtyModel);
                    db.SaveChanges();

                    TempData["Success"] = "New Asset added successfully!";
                }
                else
                {
                    // 1️⃣ Update Quantity in Master
                    existingAsset.Quantity += 1;
                    existingAsset.UpdatedBy = 1; // replace with Session UserID
                    db.Entry(existingAsset).State = EntityState.Modified;

                    // 2️⃣ Add new unit in Quantities
                    qtyModel.AssetID = existingAsset.AssetID;
                    qtyModel.IsQRIssued = false;
                    qtyModel.IsExpired = false;
                    qtyModel.ModifiedDate = DateTime.Now;
                    db.AssetQuantities.Add(qtyModel);

                    db.SaveChanges();
                    TempData["Success"] = "Existing Asset updated with new unit!";
                }

                return RedirectToAction("Dashboard");
            }
            return View(model);
        }

        // ================================
        // GET: Upload Assets (Bulk)
        // ================================
        public ActionResult UploadAssets()
        {
            return View();
        }

        // ================================
        // POST: Upload Assets (Bulk)
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAssets(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                using (var package = new ExcelPackage(file.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string assetName = worksheet.Cells[row, 1].Value?.ToString();
                        string serial = worksheet.Cells[row, 2].Value?.ToString();
                        DateTime purchaseDate = DateTime.Parse(worksheet.Cells[row, 3].Value?.ToString());
                        DateTime warrantyExpiry = DateTime.Parse(worksheet.Cells[row, 4].Value?.ToString());

                        if (!string.IsNullOrEmpty(assetName) && !string.IsNullOrEmpty(serial))
                        {
                            var existingAsset = db.AssetMasters.FirstOrDefault(a => a.AssetName == assetName);

                            if (existingAsset == null)
                            {
                                // Create new Master
                                var master = new AssetMaster
                                {
                                    AssetName = assetName,
                                    Quantity = 1,
                                    CreatedDate = DateTime.Now
                                };
                                db.AssetMasters.Add(master);
                                db.SaveChanges();

                                // Add to Quantities
                                var qty = new AssetQuantity
                                {
                                    AssetID = master.AssetID,
                                    SerialNumber = serial,
                                    PurchaseDate = purchaseDate,
                                    WarrantyExpiryDate = warrantyExpiry,
                                    IsQRIssued = false,
                                    IsExpired = false
                                };
                                db.AssetQuantities.Add(qty);
                            }
                            else
                            {
                                // Update master quantity
                                existingAsset.Quantity += 1;
                                db.Entry(existingAsset).State = EntityState.Modified;

                                // Add new quantity row
                                var qty = new AssetQuantity
                                {
                                    AssetID = existingAsset.AssetID,
                                    SerialNumber = serial,
                                    PurchaseDate = purchaseDate,
                                    WarrantyExpiryDate = warrantyExpiry,
                                    IsQRIssued = false,
                                    IsExpired = false,
                                    ModifiedDate = DateTime.Now
                                };
                                db.AssetQuantities.Add(qty);
                            }
                        }
                    }
                    db.SaveChanges();
                }
                TempData["Success"] = "Assets uploaded successfully!";
            }
            return RedirectToAction("Dashboard");
        }

        // ================================
        // GET: Issue QR Codes
        // ================================
        public ActionResult IssueQRCode()
        {
            var assets = db.AssetQuantities
                .Where(a => a.IsQRIssued == false && a.IsExpired == false)
                .ToList();

            return View(assets);
        }

        // ================================
        // POST: Issue QR Codes
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IssueQRCode(List<int> selectedAssetIds)
        {
            foreach (var id in selectedAssetIds)
            {
                var asset = db.AssetQuantities.Find(id);
                if (asset != null && asset.IsQRIssued == false)
                {
                    asset.IsQRIssued = true;
                    asset.ModifiedDate = DateTime.Now;
                }

            }
            db.SaveChanges();
            TempData["Success"] = "QR Codes issued successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}