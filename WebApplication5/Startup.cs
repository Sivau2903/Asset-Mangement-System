using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hangfire;
using Microsoft.Owin;
using Owin;
using WebApplication5.Models;

[assembly: OwinStartup(typeof(WebApplication5.Startup))] // Use your actual namespace


namespace WebApplication5
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("HangfireConnection");

            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();

            // Time zone detection (cross-platform)
            var istTimeZone = GetISTTimeZone();

            // ✅ Updated usage
            //RecurringJob.AddOrUpdate<StockAlertService>(
            //    recurringJobId: "low-stock-check",
            //    methodCall: x => x.CheckLowStock(),
            //    cronExpression: "0 10 * * *", // 10 AM every day
            //    options: new RecurringJobOptions
            //    {
            //        TimeZone = istTimeZone
            //    }
            //);
        }

        // Helper to support both Windows and Linux
        private TimeZoneInfo GetISTTimeZone()
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                }
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local; // Fallback if timezone is unavailable
            }
        }

    }
}