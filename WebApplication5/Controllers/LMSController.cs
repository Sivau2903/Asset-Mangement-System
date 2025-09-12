using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;

using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class LMSController : Controller
    {
        private readonly ASPEntities2 _db = new ASPEntities2();
        // GET: LMS
        public ActionResult Home()
        {
            return View();
        }

        public ActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadSingleBook(BookUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                var book = new Book
                {
                    SlNo = model.SlNo,
                    AccNo = model.AccNo,
                    CallNo = model.CallNo,
                    TitleoftheBook = model.Title,
                    Author = model.Author,
                    PlaceOfPublishers = model.PlaceOfPublisher,
                    Year = model.Year,
                    Edition = model.Edition,
                    pages = model.Pages,
                    Vol = model.Volume,
                    Source = model.Source,
                    Price = model.Price
                };

                _db.Books.Add(book);
                _db.SaveChanges();

                TempData["Message"] = "Book added successfully!";
            }

            return RedirectToAction("Upload");
        }



        [HttpPost]
        public ActionResult UploadBooksExcel(HttpPostedFileBase excelFile)
        {
            if (excelFile != null && excelFile.ContentLength > 0)
            {
                // Set EPPlus license for non-commercial personal use
                ExcelPackage.License.SetNonCommercialPersonal("Your Name");

                using (var package = new ExcelPackage(excelFile.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets.First();
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string slNoText = worksheet.Cells[row, 1].Text.Trim();
                        string accNo = worksheet.Cells[row, 2].Text.Trim();
                        string callNo = worksheet.Cells[row, 3].Text.Trim();
                        string title = worksheet.Cells[row, 4].Text.Trim();
                        string author = worksheet.Cells[row, 5].Text.Trim();
                        string place = worksheet.Cells[row, 6].Text.Trim();
                        string yearText = worksheet.Cells[row, 7].Text.Trim();
                        string edition = worksheet.Cells[row, 8].Text.Trim();
                        string pages = worksheet.Cells[row, 9].Text.Trim();
                        string vol = worksheet.Cells[row, 10].Text.Trim();
                        string source = worksheet.Cells[row, 11].Text.Trim();
                        string priceText = worksheet.Cells[row, 12].Text.Trim();

                        // Safe parsing
                        int slNo;
                        int.TryParse(slNoText, out slNo); // Defaults to 0 if invalid

                        int? year = null;
                        if (int.TryParse(yearText, out int parsedYear))
                            year = parsedYear;

                        decimal? price = null;
                        if (decimal.TryParse(priceText, out decimal parsedPrice))
                            price = parsedPrice;

                        var book = new Book
                        {
                            SlNo = slNo,
                            AccNo = accNo,
                            CallNo = callNo,
                            TitleoftheBook = title,
                            Author = author,
                            PlaceOfPublishers = place,
                            Year = year,
                            Edition = edition,
                            pages = pages,
                            Vol = vol,
                            Source = source,
                            Price = price
                        };

                        _db.Books.Add(book);
                    }
                    _db.SaveChanges();
                }

                TempData["Message"] = "Books uploaded successfully!";
            }

            return RedirectToAction("Upload");
        }





    }
}