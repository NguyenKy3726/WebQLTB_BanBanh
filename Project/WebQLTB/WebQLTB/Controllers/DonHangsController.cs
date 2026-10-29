using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebQLTB.Data;
using WebQLTB.Filters;
using WebQLTB.Models;

namespace WebQLTB.Controllers;

[PermissionAuthorize("SALES")]
public class DonHangsController : Controller
{
    private readonly AppDbContext _context;

    private static readonly Dictionary<string, string> TenTrangThai = new()
    {
        ["CHO_NHAN_VIEN_XU_LY"] = "Chờ nhân viên xử lý",
        ["CHO_KHACH_XAC_NHAN"] = "Chờ khách xác nhận",
        ["CHO_THANH_TOAN_COC"] = "Chờ thanh toán cọc",
        ["DA_XAC_NHAN"] = "Đã xác nhận",
        ["DANG_CHUAN_BI"] = "Đang chuẩn bị",
        ["SAN_SANG_GIAO"] = "Sẵn sàng giao",
        ["DANG_GIAO"] = "Đang giao",
        ["HOAN_TAT"] = "Hoàn tất",
        ["DA_HUY"] = "Đã hủy"
    };

    public DonHangsController(AppDbContext context) => _context = context;

    public IActionResult Index()
    {
        SetDisplayData();
        return View(_context.DonHang_DS());
    }

    public IActionResult Details(string? madonhang)
    {
        if (string.IsNullOrWhiteSpace(madonhang)) return NotFound();

        var donhang = _context.DonHang_CT(madonhang);
        if (donhang == null) return NotFound();

        SetDisplayData();
        ViewBag.LichSu = _context.DonHang_LichSu(madonhang);
        return View(donhang);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChuyenTrangThai(DonHangChuyenTrangThaiViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["LoiXuLyDon"] = "Thông tin thao tác chưa hợp lệ. Hãy tải lại trang và thử lại.";
            return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
        }

        var maNV = HttpContext.Session.GetInt32("MaNV");
        if (maNV is null || maNV < 0 || maNV > byte.MaxValue)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/DonHangs/Index" });
        }

        try
        {
            // Mã nhân viên lấy từ session, không lấy từ dữ liệu gửi trong form.
            _context.DonHang_ChuyenTrangThai(model, (byte)maNV.Value);
            TempData["ThongBaoXuLyDon"] = "Đã ghi nhận thao tác xử lý đơn hàng.";
        }
        catch (SqlException ex)
        {
            TempData["LoiXuLyDon"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
    }

    private void SetDisplayData()
    {
        ViewBag.TenKH = _context.KhachHang_DS()
            .ToDictionary(kh => kh.MaKH, kh => kh.HoKH + " " + kh.TenKH);
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);
        ViewBag.TenTrangThaiDonHang = TenTrangThai;
    }
}
