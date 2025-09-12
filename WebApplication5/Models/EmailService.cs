using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;

namespace WebApplication4.Models
{
    public class EmailService
    {
        private readonly string _email = ConfigurationManager.AppSettings["Email"];
        private readonly string _password = ConfigurationManager.AppSettings["EmailPassword"];

        //private readonly string _email = "shivaupputuri5@gmail.com"; // Your Gmail
        //private readonly string _password = "uxwvtphmvzhqqqpl"; // Your App Password

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(_email, _password);
                smtp.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_email),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // Ensures the email is sent as HTML
                };

                mailMessage.To.Add(toEmail);

                try
                {
                    await smtp.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    // Log error (file, db, etc.) or rethrow
                    throw new Exception("Failed to send email: " + ex.Message, ex);
                }

            }
        }
    }

}