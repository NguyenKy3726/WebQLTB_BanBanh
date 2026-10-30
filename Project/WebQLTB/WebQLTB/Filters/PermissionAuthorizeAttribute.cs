using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebQLTB.Data;

namespace WebQLTB.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : TypeFilterAttribute
{
    public PermissionAuthorizeAttribute(string permission) : base(typeof(PermissionAuthorizeFilter))
    {
        Arguments = new object[] { permission };
    }
}

public sealed class PermissionAuthorizeFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;
    private readonly string _permission;
    public PermissionAuthorizeFilter(AppDbContext db, string permission)
    {
        _db = db;
        _permission = permission;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.Session.GetString("Role") != "NhanVien")
        {
            var returnUrl = http.Request.Path + http.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }
        var maNV = http.Session.GetInt32("MaNV");
        if (maNV is null || maNV < 1 || maNV > byte.MaxValue
            || !_db.NhanVien_CoQuyen((byte)maNV.Value, _permission))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
