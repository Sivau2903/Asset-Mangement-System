using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.IO;
using System.Web;
using System.Diagnostics;

namespace WebApplication5.Models
{
    public  class StockAlertService
    {
        public  void CheckLowStock()
        {
            try
            {
                using (ASPEntities2 db = new ASPEntities2())
                {               
                    
                    // Step 1: Reset the alert flags for all materials
                    var previouslyAlerted = db.MaterialMasterLists
                        .Where(m => m.IsLowStockAlertSent)
                        .ToList();

                    foreach (var item in previouslyAlerted)
                    {
                        item.IsLowStockAlertSent = false;
                    }
                    db.SaveChanges();

                    var lowStockMaterials = db.MaterialMasterLists
                        .Where(m => m.AvailableQuantity < m.MinimumLimit && !m.IsLowStockAlertSent)
                        .ToList();

                    foreach (var material in lowStockMaterials)
                    {
                        string subject = "⚠️ Low Stock Alert";
                        string body = $"Material '{material.MaterialSubCategory}' is low in stock.\nAvailable: {material.AvailableQuantity}, Limit: {material.MinimumLimit}";

                        // Example: Send to Stock Incharge and Local Purchase Dept
                        var storeAdmin = db.StoreAdmins.FirstOrDefault(sa => sa.StoreAdminID == material.UpdatedBy);

                        if (storeAdmin != null && !string.IsNullOrEmpty(storeAdmin.EmailID))
                        {
                            // Step 3: Send email to StoreAdmin
                            SendEmail(storeAdmin.EmailID, subject, body);
                        }
                        else
                        {
                            // Optional: Log or handle case where no StoreAdmin found
                            Console.WriteLine($"No email found for StoreAdminID: {material.UpdatedBy}");
                        }


                        // Mark as alert sent
                        material.IsLowStockAlertSent = true;
                    }

                    db.SaveChanges();
                }

                Debug.WriteLine("✅ Stock check completed and alerts sent if needed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("❌ Error in StockAlert: " + ex.Message);
            }
        }

        private  void SendEmail(string toEmail, string subject, string body)
        {
            MailMessage mail = new MailMessage("shivaupputuri5@gmail.com", toEmail);
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient client = new SmtpClient("smtp.gmail.com"); // Replace with your SMTP server
            client.Port = 587;
            client.Credentials = new System.Net.NetworkCredential("shivaupputuri5@gmail.com", "uxwvtphmvzhqqqpl");
            client.EnableSsl = true;

            client.Send(mail);
        }
    }

}