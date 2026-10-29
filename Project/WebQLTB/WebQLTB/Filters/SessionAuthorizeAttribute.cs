namespace WebQLTB.Filters
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public string? RequiredRole { get; set; }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var user = http.Session.GetString("Username");
            if (string.IsNullOrEmpty(user))
            {
                // build returnUrl (đầy đủ path + query)
                var request = http.Request;
                var returnUrl = request.Path + request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl });
                return;
            }
            if (RequiredRole != null && http.Session.GetString("Role") != RequiredRole)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
