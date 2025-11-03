using OfficeOpenXml;
using ClosedXML.Excel;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class NodelOfficerController : Controller
    {
        private readonly ASPEntities2 db = new ASPEntities2();

        public ActionResult Dashboard()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Login");
            }

            int currentUserID = Convert.ToInt32(Session["UserID"]);
            DateTime Today = DateTime.Today;
            var userassets = db.BDSAssets.Where(a => a.AddedBy == currentUserID);
            var model = new NodeOfficerDashboardViewModel
            {
                TotalAssets = db.BDSAssets.Count(),
                AssetsWithQRCode = db.BDSAssets.Count(a => a.IsQRIssued == true),
                ExpiredAssets = db.BDSAssets.Count(a => a.ExpiryDate != null && a.ExpiryDate < Today)
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult AddAsset()
        {
            ViewBag.AssetTypes = new SelectList(db.AssetTypes.ToList(), "AssetTypeID", "AssetType1");
            ViewBag.MaterialCategories = new SelectList(db.MaterialCategories.ToList(), "MID", "MaterialCategory1");
            ViewBag.MaterialSubCategories = new SelectList(db.MaterialSubCategories.ToList(), "MSubCategoryID", "MaterialSubCategory1");

            return View();
        }
        [HttpGet]
        public JsonResult GetCategories(int assetTypeId)
        {
            var categories = db.MaterialCategories
                .Where(c => c.AssetTypeID == assetTypeId)
                .Select(c => new
                {
                    c.MID,
                    c.MaterialCategory1
                })
                .ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSubCategories(int categoryId)
        {
            var subCategories = db.MaterialSubCategories
                .Where(sc => sc.MID == categoryId)
                .Select(sc => new
                {
                    sc.MSubCategoryID,
                    sc.MaterialSubCategory1
                })
                .ToList();

            return Json(subCategories, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAsset(BDSAsset model)
        {
                if (ModelState.IsValid)
                {
                    int addedBy = 0;
                    if (Session["UserID"] != null)
                        addedBy = Convert.ToInt32(Session["UserID"]);

                    int assetTypeId = Convert.ToInt32(model.AssetType);
                    int categoryId = Convert.ToInt32(model.MaterialCategory);
                    int subCategoryId = Convert.ToInt32(model.MaterialSubCategory);

                    string assetTypeName = db.AssetTypes
                        .Where(a => a.AssetTypeID == assetTypeId)
                        .Select(a => a.AssetType1)
                        .FirstOrDefault();

                    string categoryName = db.MaterialCategories
                        .Where(c => c.MID == categoryId)
                        .Select(c => c.MaterialCategory1)
                        .FirstOrDefault();

                    string subCategoryName = db.MaterialSubCategories
                        .Where(s => s.MSubCategoryID == subCategoryId)
                        .Select(s => s.MaterialSubCategory1)
                        .FirstOrDefault();

                    var asset = new BDSAsset
                    {
                        AssetType = assetTypeName,
                        MaterialCategory = categoryName,
                        MaterialSubCategory = subCategoryName,
                        PurchaseDate = model.PurchaseDate,
                        MfgDate = model.MfgDate,
                        WarrantyDate = model.WarrantyDate,
                        ExpiryDate = model.ExpiryDate,
                        UserName = model.UserName,
                        Designation = model.Designation,
                        Location = model.Location,
                        AddedBy = addedBy,
                        AddedDate = DateTime.Now,
                        IsQRIssued = model.IsQRIssued  
                    };


                    db.BDSAssets.Add(asset);
                    db.SaveChanges();

                    TempData["Success"] = "Asset added successfully!";
                    return RedirectToAction("AddAsset");
                }

                TempData["Error"] = "Please check your input fields.";
                ViewBag.AssetTypes = new SelectList(db.AssetTypes.ToList(), "AssetTypeID", "AssetType1");
                ViewBag.MaterialCategories = new SelectList(db.MaterialCategories.ToList(), "MID", "MaterialCategory1");
                ViewBag.MaterialSubCategories = new SelectList(db.MaterialSubCategories.ToList(), "MSubCategoryID", "MaterialSubCategory1");

                return View(model);
            }
        

        [HttpPost]
        public JsonResult AddAssetMetaDataAjax(string AssetType, string MaterialCategory, string MaterialSubCategory)
        {

                int assetTypeId = 0, categoryId = 0;

                // 1️⃣ Add Asset Type if not exists
                if (!string.IsNullOrEmpty(AssetType))
                {
                    var existingType = db.AssetTypes.FirstOrDefault(a => a.AssetType1 == AssetType);
                    if (existingType == null)
                    {
                        var newType = new AssetType { AssetType1 = AssetType };
                        db.AssetTypes.Add(newType);
                        db.SaveChanges();
                        assetTypeId = newType.AssetTypeID;
                    }
                    else assetTypeId = existingType.AssetTypeID;
                }

                // 2️⃣ Add Material Category if not exists
                if (!string.IsNullOrEmpty(MaterialCategory))
                {
                    var existingCat = db.MaterialCategories
                        .FirstOrDefault(c => c.MaterialCategory1 == MaterialCategory && c.AssetTypeID == assetTypeId);
                    if (existingCat == null)
                    {
                        var newCat = new MaterialCategory { MaterialCategory1 = MaterialCategory, AssetTypeID = assetTypeId };
                        db.MaterialCategories.Add(newCat);
                        db.SaveChanges();
                        categoryId = newCat.MID;
                    }
                    else categoryId = existingCat.MID;
                }

                // 3️⃣ Add Material SubCategory if not exists
                if (!string.IsNullOrEmpty(MaterialSubCategory))
                {
                    var existingSub = db.MaterialSubCategories
                        .FirstOrDefault(s => s.MaterialSubCategory1 == MaterialSubCategory && s.MID == categoryId);
                    if (existingSub == null)
                    {
                        var newSub = new MaterialSubCategory { MaterialSubCategory1 = MaterialSubCategory, MID = categoryId };
                        db.MaterialSubCategories.Add(newSub);
                        db.SaveChanges();
                    }
                }

                return Json(new { success = true, message = "Metadata saved successfully!" });
            }
       


        public ActionResult UploadAssets()
        {
            return View();
        }


        [HttpGet]
        public ActionResult DownloadAssetTemplate()
        {
            string filePath = Server.MapPath("~/Templets/Asset QR Issue Template.xlsx");
            System.Diagnostics.Debug.WriteLine("Looking for file at: " + filePath);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "Template file not found on the server.";
                return RedirectToAction("UploadAssets");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string downloadFileName = "Asset QR Issue Template.xlsx";

            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        downloadFileName);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAssets(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rowCount = worksheet.LastRowUsed().RowNumber();

                    int addedBy = 0;
                    if (Session["UserID"] != null)
                        addedBy = Convert.ToInt32(Session["UserID"]);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string assetType = worksheet.Cell(row, 1).GetString();
                        string materialCategory = worksheet.Cell(row, 2).GetString();
                        string materialSubCategory = worksheet.Cell(row, 3).GetString();

                        DateTime? purchaseDate = TryParseDate(worksheet.Cell(row, 4).Value);
                        DateTime? mfgDate = TryParseDate(worksheet.Cell(row, 5).Value);
                        DateTime? warrantyDate = TryParseDate(worksheet.Cell(row, 6).Value);
                        DateTime? expiryDate = TryParseDate(worksheet.Cell(row, 7).Value);

                        string userName = worksheet.Cell(row, 8).GetString();
                        string designation = worksheet.Cell(row, 9).GetString();
                        string location = worksheet.Cell(row, 10).GetString();
                        string isQrIssuedStr = worksheet.Cell(row, 11).GetString()?.Trim();

                        bool isQrIssued = false;
                        if (!string.IsNullOrEmpty(isQrIssuedStr))
                        {
                            string normalized = isQrIssuedStr.ToLower();
                            if (normalized.Contains("issued"))
                                isQrIssued = true;
                            else if (normalized.Contains("not"))
                                isQrIssued = false;
                        }

                        var newAsset = new BDSAsset
                        {
                            AssetType = assetType,
                            MaterialCategory = materialCategory,
                            MaterialSubCategory = materialSubCategory,
                            PurchaseDate = purchaseDate,
                            MfgDate = mfgDate,
                            WarrantyDate = warrantyDate,
                            ExpiryDate = expiryDate,
                            UserName = userName,
                            Designation = designation,
                            Location = location,
                            AddedBy = addedBy,
                            AddedDate = DateTime.Now,
                            IsQRIssued = isQrIssued
                        };

                        db.BDSAssets.Add(newAsset);
                    }

                    db.SaveChanges();
                }

                TempData["Success"] = "Assets uploaded successfully!";
            }
            else
            {
                TempData["Error"] = "Please upload a valid Excel file.";
            }

            return RedirectToAction("UploadAssets");
        }


        private DateTime? TryParseDate(object cellValue)
        {
            if (cellValue == null)
                return null;

            if (DateTime.TryParse(cellValue.ToString(), out DateTime parsed))
                return parsed;

            return null;
        }



        public ActionResult IssueQRCode()
        {
            var assets = db.BDSAssets.ToList();
            return View(assets);
        }

        [HttpPost]
        public ActionResult IssueQRCode(int assetId)
        {
            var asset = db.BDSAssets.Find(assetId);
            if (asset == null)
                return HttpNotFound();

            asset.IsQRIssued = true;
            asset.AddedDate = DateTime.Now;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("QRSticker", "NodelOfficer", new { id = asset.AssetID })
            });
        }

        public ActionResult GenerateQRCode(int id)
        {
            var asset = db.BDSAssets.Find(id);
            if (asset == null)
                return HttpNotFound();

            string qrData = $"AssetID:{asset.AssetID}\nUser:{asset.UserName}\nDesignation:{asset.Designation}";
            using (var qrGen = new QRCodeGenerator())
            {
                var qrDataObj = qrGen.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCode(qrDataObj);
                using (var bitmap = qrCode.GetGraphic(20))
                {
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return File(stream.ToArray(), "image/png");
                    }
                }
            }
        }

        public ActionResult QRSticker(int id)
        {
            var asset = db.BDSAssets.Find(id);
            if (asset == null)
                return HttpNotFound();

            return PartialView("_QRSticker", asset);
        }
    }
}