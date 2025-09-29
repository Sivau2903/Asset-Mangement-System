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
                    IsBodyHtml = true 
                };

                mailMessage.To.Add(toEmail);

                try
                {
                    await smtp.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to send email: " + ex.Message, ex);
                }
            }
        }
    }
}