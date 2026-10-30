using Microsoft.AspNetCore.Mvc;
using WebQLTB.Data;
using WebQLTB.Models;

namespace WebQLTB.Controllers;

public class QuanLyController : Controller
{
    private readonly AppDbContext _context;

    public QuanLyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("Role") != "NhanVien")
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(Index), "QuanLy")
            });
        }

        var maNV = HttpContext.Session.GetInt32("MaNV");
        if (maNV is null || maNV < 1 || maNV > byte.MaxValue)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(Index), "QuanLy")
            });
        }

        // Kiểm tra quyền trong database để menu phản ánh phân quyền hiện tại.
        var coQuyenAdmin = _context.NhanVien_CoQuyen((byte)maNV.Value, "ADMIN");
        var coQuyenSales = _context.NhanVien_CoQuyen((byte)maNV.Value, "SALES");
        if (!coQuyenAdmin && !coQuyenSales)
            return RedirectToAction("AccessDenied", "Account");

        return View(new QuanLyViewModel
        {
            TenNhanVien = HttpContext.Session.GetString("DisplayName") ?? "Nhân viên",
            CoQuyenAdmin = coQuyenAdmin,
            CoQuyenSales = coQuyenSales
        });
    }
}
