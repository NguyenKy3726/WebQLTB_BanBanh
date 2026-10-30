using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebQLTB.Data;
using WebQLTB.Filters;
using WebQLTB.Models;

namespace WebQLTB.Controllers;

[PermissionAuthorize("SALES")]
public class DonHangsController : Controller
{
    private const int PageSize = 15;
    private readonly AppDbContext _context;
    private readonly ILogger<DonHangsController> _logger;

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

    public DonHangsController(AppDbContext context, ILogger<DonHangsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index(string? trangThai, string? q, int page = 1)
    {
        var allOrders = _context.DonHang_DS()
            .OrderByDescending(order => order.NgayDat)
            .ThenByDescending(order => order.MaDonHang)
            .ToList();
        var allPayments = _context.ThanhToan_DS();
        var customerNames = _context.KhachHang_DS()
            .ToDictionary(customer => customer.MaKH, customer => $"{customer.HoKH} {customer.TenKH}");

        ViewBag.TenTrangThaiDonHang = TenTrangThai;
        ViewBag.TenKH = customerNames;
        ViewBag.SoDonTheoTrangThai = allOrders
            .GroupBy(order => order.TrangThaiDonHang)
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.DaThuTheoDon = allPayments
            .Where(payment => payment.TrangThaiThanhToan == "SUCCESS")
            .GroupBy(payment => payment.MaDonHang)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(payment => payment.LoaiGiaoDich == "REFUND" ? -payment.SoTien : payment.SoTien));

        if (!string.IsNullOrWhiteSpace(trangThai) && TenTrangThai.ContainsKey(trangThai))
            allOrders = allOrders.Where(order => order.TrangThaiDonHang == trangThai).ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            allOrders = allOrders.Where(order =>
            {
                var customerName = order.MaKH.HasValue && customerNames.TryGetValue(order.MaKH.Value, out var name)
                    ? name
                    : "Khách lẻ";
                return order.MaDonHang.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || order.TenNguoiNhan.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || order.DienThoaiNguoiNhan.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || customerName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(allOrders.Count / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.TrangThaiDangChon = trangThai;
        ViewBag.TuKhoa = q;
        ViewBag.TrangHienTai = page;
        ViewBag.TongTrang = totalPages;
        ViewBag.TongDonSauLoc = allOrders.Count;
        SetFeedback();
        return View(allOrders.Skip((page - 1) * PageSize).Take(PageSize).ToList());
    }

    public IActionResult Details(string? madonhang)
    {
        if (string.IsNullOrWhiteSpace(madonhang)) return NotFound();

        var donhang = _context.DonHang_CT(madonhang);
        if (donhang == null) return NotFound();

        SetDetailsData(donhang);
        SetFeedback();
        ViewBag.NgayGiaoMacDinh = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm");
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

        if (!TryGetEmployeeId(out var maNV))
            return RedirectToAction("Login", "Account", new { returnUrl = "/DonHangs/Index" });

        try
        {
            _context.DonHang_ChuyenTrangThai(model, maNV);
            TempData["ThongBaoXuLyDon"] = "Đã ghi nhận thao tác xử lý đơn hàng.";
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not change status for order {OrderId}.", model.MaDonHang);
            TempData["LoiXuLyDon"] = SqlErrorMessage.ForUser(ex,
                "Chưa thể cập nhật đơn hàng lúc này. Vui lòng tải lại trang và thử lại.");
        }

        return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GhiNhanThanhToan(GhiNhanThanhToanViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["LoiXuLyDon"] = "Thông tin giao dịch chưa hợp lệ. Hãy kiểm tra số tiền và phương thức.";
            return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
        }

        var order = _context.DonHang_CT(model.MaDonHang);
        if (order == null) return NotFound();

        var existingPayments = _context.ThanhToan_DS()
            .Where(payment => payment.MaDonHang == model.MaDonHang)
            .ToList();

        if (model.LoaiGiaoDich == "PAYMENT" && order.TrangThaiDonHang is "DA_HUY" or "HOAN_TAT")
        {
            TempData["LoiXuLyDon"] = "Không thể tạo khoản thu mới cho đơn đã hủy hoặc hoàn tất.";
            return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
        }

        if (model.LoaiGiaoDich == "REFUND")
        {
            if (order.TrangThaiDonHang != "DA_HUY")
            {
                TempData["LoiXuLyDon"] = "Chỉ tạo khoản hoàn tiền sau khi đơn đã hủy.";
                return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
            }

            var original = existingPayments.FirstOrDefault(payment =>
                payment.MaThanhToan == model.MaThanhToanGoc
                && payment.LoaiGiaoDich == "PAYMENT"
                && payment.TrangThaiThanhToan == "SUCCESS");
            if (original == null)
            {
                TempData["LoiXuLyDon"] = "Khoản hoàn phải tham chiếu một khoản thu thành công của chính đơn này.";
                return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
            }

            var refundsAlreadyCreated = existingPayments
                .Where(payment => payment.MaThanhToanGoc == original.MaThanhToan
                    && payment.LoaiGiaoDich == "REFUND"
                    && payment.TrangThaiThanhToan is "SUCCESS" or "PENDING")
                .Sum(payment => payment.SoTien);
            if (refundsAlreadyCreated + model.SoTien > original.SoTien)
            {
                TempData["LoiXuLyDon"] = "Tổng khoản hoàn đang chờ/thành công sẽ vượt số tiền thu gốc.";
                return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
            }
        }

        try
        {
            _context.ThanhToan_Them(new ThanhToan
            {
                MaDonHang = model.MaDonHang,
                LoaiGiaoDich = model.LoaiGiaoDich,
                SoTien = model.SoTien,
                PhuongThuc = model.PhuongThuc,
                MaThanhToanGoc = model.LoaiGiaoDich == "REFUND" ? model.MaThanhToanGoc : null,
                DuongDanChungTu = model.DuongDanChungTu,
                GhiChu = model.GhiChu
            });
            TempData["ThongBaoXuLyDon"] = "Đã tạo giao dịch ở trạng thái chờ xác nhận. Xác nhận giao dịch sau khi kiểm tra tiền/chứng từ.";
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not record a payment for order {OrderId}.", model.MaDonHang);
            TempData["LoiXuLyDon"] = SqlErrorMessage.ForUser(ex,
                "Chưa thể ghi nhận giao dịch lúc này. Vui lòng thử lại sau.");
        }

        return RedirectToAction(nameof(Details), new { madonhang = model.MaDonHang });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult XuLyThanhToan(string? maThanhToan, string? trangThaiMoi)
    {
        if (string.IsNullOrWhiteSpace(maThanhToan)) return NotFound();
        if (trangThaiMoi is not ("SUCCESS" or "REJECTED")) return BadRequest();

        var payment = _context.ThanhToan_CT(maThanhToan);
        if (payment == null) return NotFound();
        if (payment.TrangThaiThanhToan != "PENDING")
        {
            TempData["LoiXuLyDon"] = "Giao dịch này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { madonhang = payment.MaDonHang });
        }
        if (!TryGetEmployeeId(out var maNV))
            return RedirectToAction("Login", "Account", new { returnUrl = "/DonHangs/Index" });

        payment.TrangThaiThanhToan = trangThaiMoi;
        payment.MaNVXacNhan = maNV;
        try
        {
            // Stored procedure xác minh cọc/tổng thu/hoàn tiền và tự ghi thời điểm xác nhận.
            _context.ThanhToan_Sua(payment);
            TempData["ThongBaoXuLyDon"] = trangThaiMoi == "SUCCESS"
                ? "Đã xác nhận giao dịch thành công."
                : "Đã từ chối giao dịch.";
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not process payment {PaymentId}.", payment.MaThanhToan);
            TempData["LoiXuLyDon"] = SqlErrorMessage.ForUser(ex,
                "Chưa thể xử lý giao dịch lúc này. Vui lòng thử lại sau.");
        }

        return RedirectToAction(nameof(Details), new { madonhang = payment.MaDonHang });
    }

    private void SetDetailsData(DonHang order)
    {
        var products = _context.SanPham_DS().ToDictionary(product => product.MaSP, product => product.TenSP);
        var variants = _context.BienTheBanh_DS().ToDictionary(variant => variant.MaBienThe);
        var payments = _context.ThanhToan_DS()
            .Where(payment => payment.MaDonHang == order.MaDonHang)
            .OrderBy(payment => payment.ThoiGianTao)
            .ToList();

        ViewBag.TenKH = _context.KhachHang_DS()
            .ToDictionary(customer => customer.MaKH, customer => $"{customer.HoKH} {customer.TenKH}");
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(employee => employee.MaNV, employee => $"{employee.HoNV} {employee.TenNV}");
        ViewBag.TenTrangThaiDonHang = TenTrangThai;
        ViewBag.ChiTietDonHang = _context.ChiTietDonHang_DS()
            .Where(line => line.MaDonHang == order.MaDonHang)
            .OrderBy(line => line.STT)
            .ToList();
        ViewBag.TenBienThe = variants.ToDictionary(
            pair => pair.Key,
            pair => $"{products.GetValueOrDefault(pair.Value.MaSP, pair.Value.MaSP)} · {pair.Value.TenBienThe}");
        ViewBag.ThanhToans = payments;
        ViewBag.DaThu = payments
            .Where(payment => payment.TrangThaiThanhToan == "SUCCESS")
            .Sum(payment => payment.LoaiGiaoDich == "REFUND" ? -payment.SoTien : payment.SoTien);
        ViewBag.ThanhToanThanhCong = payments
            .Where(payment => payment.LoaiGiaoDich == "PAYMENT" && payment.TrangThaiThanhToan == "SUCCESS")
            .ToList();
        ViewBag.TenLoaiGiaoDich = new Dictionary<string, string>
        {
            ["PAYMENT"] = "Thu tiền",
            ["REFUND"] = "Hoàn tiền"
        };
        ViewBag.TenPhuongThuc = new Dictionary<string, string>
        {
            ["CASH"] = "Tiền mặt",
            ["BANK_TRANSFER"] = "Chuyển khoản"
        };
        ViewBag.TenTrangThaiThanhToan = new Dictionary<string, string>
        {
            ["PENDING"] = "Chờ xác nhận",
            ["SUCCESS"] = "Thành công",
            ["REJECTED"] = "Đã từ chối"
        };
        ViewBag.LichSu = _context.DonHang_LichSu(order.MaDonHang);
    }

    private void SetFeedback()
    {
        ViewBag.ThongBaoXuLyDon = TempData["ThongBaoXuLyDon"] as string;
        ViewBag.LoiXuLyDon = TempData["LoiXuLyDon"] as string;
    }

    private bool TryGetEmployeeId(out byte maNV)
    {
        var employeeId = HttpContext.Session.GetInt32("MaNV");
        if (employeeId is >= 1 and <= byte.MaxValue)
        {
            maNV = (byte)employeeId.Value;
            return true;
        }
        maNV = 0;
        return false;
    }
}
