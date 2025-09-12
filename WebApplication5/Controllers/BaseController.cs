using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication5.Controllers
{
    public class BaseController : Controller
    {
        // GET: Base

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            var actionName = filterContext.ActionDescriptor.ActionName;

            var allowAnonymousActions = new[]
            {
        new { Controller = "Login", Action = "LoginPage" },
        new { Controller = "Login", Action = "Authenticate" },
        new { Controller = "Login", Action = "ForgotPassword" },
        new { Controller = "Login", Action = "VerifyOTP" },
        new { Controller = "Login", Action = "ResetPasswordRequest" },
        new { Controller = "Login", Action = "ResetPassword" }
    };

            bool isAnonymous = allowAnonymousActions.Any(a =>
                string.Equals(a.Controller, controllerName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Action, actionName, StringComparison.OrdinalIgnoreCase)
            );

            if (!isAnonymous && Session["UserID"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {
                { "controller", "Login" },
                { "action", "LoginPage" }
                    });
                return;
            }

            // Prevent caching for all pages to avoid back-navigation after logout
            HttpCachePolicyBase cache = filterContext.HttpContext.Response.Cache;
            cache.SetCacheability(HttpCacheability.NoCache);
            cache.SetNoStore();
            cache.SetExpires(DateTime.UtcNow.AddSeconds(-1));
            cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            cache.SetValidUntilExpires(false);

            base.OnActionExecuting(filterContext);
        }

    }

}

