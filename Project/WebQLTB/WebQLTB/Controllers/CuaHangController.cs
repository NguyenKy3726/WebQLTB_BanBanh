using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebQLTB.Data;
using WebQLTB.Models;

namespace WebQLTB.Controllers;

public class CuaHangController : Controller
{
    private readonly AppDbContext _db;
    private const string CartKey = "GioHang";
    public CuaHangController(AppDbContext db) => _db = db;

    public IActionResult Index()
    {
        // Lấy dữ liệu một lần cho mỗi danh sách.
        var sanPhams = _db.SanPham_DS()
            .Where(sp => sp.TrangThaiHienThi == "PUBLISHED")
            .ToList();

        var loaiBanhs = _db.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);

        var anhTheoSanPham = _db.HinhAnhBanh_DS()
            .ToLookup(anh => anh.MaSP);

        var bienTheTheoSanPham = _db.BienTheBanh_DS()
            .Where(bt => bt.TrangThaiBan)
            .ToLookup(bt => bt.MaSP);

        var danhSach = sanPhams.Select(sp =>
        {
            // Ưu tiên ảnh đại diện, sau đó theo thứ tự hiển thị.
            var anhDaiDien = anhTheoSanPham[sp.MaSP]
                .OrderByDescending(anh => anh.LaAnhDaiDien)
                .ThenBy(anh => anh.ThuTuHienThi)
                .ThenBy(anh => anh.MaAnh)
                .FirstOrDefault();

            var bienThes = bienTheTheoSanPham[sp.MaSP].ToList();

            return new ProductCardViewModel
            {
                MaSP = sp.MaSP,
                TenSP = sp.TenSP,
                MoTaNgan = sp.MoTaNgan,

                TenLoaiBanh = loaiBanhs.TryGetValue(
                    sp.MaLoaiBanh, out var tenLoai)
                        ? tenLoai
                        : "Bánh Whiskful",

                DuongDanAnh = anhDaiDien?.DuongDanAnh,

                GiaThapNhat = bienThes.Count > 0
                    ? bienThes.Min(bt => bt.GiaBan)
                    : (decimal?)null,

                SoBienTheDangBan = bienThes.Count
            };
        }).ToList();
        var categories = _db.LoaiBanh_DS()
            .ToDictionary(x => x.MaLoaiBanh, x => x.TenLoaiBanh);

        ViewBag.Categories = categories;
        return View(danhSach);
    }

    public IActionResult Details(string id)
    {
        var product = _db.SanPham_CT(id);
        if (product == null || product.TrangThaiHienThi != "PUBLISHED") return NotFound();
        ViewBag.Variants = _db.BienTheBanh_DS().Where(x => x.MaSP == id && x.TrangThaiBan).ToList();
        ViewBag.Images = _db.HinhAnhBanh_DS().Where(x => x.MaSP == id).OrderByDescending(x => x.LaAnhDaiDien).ThenBy(x => x.ThuTuHienThi).ToList();
        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ThemVaoGio(short maBienThe, int soLuong = 1)
    {
        if (!ModelState.IsValid || soLuong < 1 || soLuong > 50) return BadRequest("Số lượng phải từ 1 đến 50.");
        var variant = _db.BienTheBanh_DS().FirstOrDefault(x => x.MaBienThe == maBienThe && x.TrangThaiBan);
        if (variant == null) return NotFound();
        var product = _db.SanPham_CT(variant.MaSP);
        if (product == null || product.TrangThaiHienThi != "PUBLISHED") return NotFound();
        var cart = ReadCart();
        var row = cart.FirstOrDefault(x => x.MaBienThe == maBienThe);
        if (row == null) cart.Add(new GioHangItem { MaBienThe=maBienThe, TenSP=product.TenSP, TenBienThe=variant.TenBienThe, DonGia=variant.GiaBan, SoLuong=soLuong });
        else row.SoLuong = Math.Min(50, row.SoLuong + soLuong);
        SaveCart(cart);
        return RedirectToAction(nameof(GioHang));
    }

    public IActionResult GioHang()
    {
        return View(RefreshCart());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CapNhatGio(List<GioHangCapNhatItem>? items)
    {
        var cart = RefreshCart();
        if (items == null || !ModelState.IsValid || items.Count != cart.Count
            || items.Select(x => x.MaBienThe).Distinct().Count() != items.Count
            || items.Any(x => !cart.Any(row => row.MaBienThe == x.MaBienThe)))
        {
            TempData["GioHangThongBao"] = "Số lượng không hợp lệ. Mỗi phiên bản nhận từ 1 đến 50 bánh. Vui lòng kiểm tra lại giỏ.";
            return RedirectToAction(nameof(GioHang));
        }

        foreach (var item in items)
        {
            var row = cart.First(x => x.MaBienThe == item.MaBienThe);
            row.SoLuong = item.SoLuong;
        }
        SaveCart(cart);
        var notice = TempData["GioHangThongBao"] as string;
        TempData["GioHangThongBao"] = "Đã cập nhật số lượng trong giỏ hàng."
            + (string.IsNullOrWhiteSpace(notice) ? "" : " " + notice);
        return RedirectToAction(nameof(GioHang));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult XoaToanBoGio()
    {
        SaveCart(new List<GioHangItem>());
        TempData["GioHangThongBao"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction(nameof(GioHang));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult XoaKhoiGio(short maBienThe)
    {
        var cart=ReadCart(); cart.RemoveAll(x=>x.MaBienThe==maBienThe); SaveCart(cart);
        return RedirectToAction(nameof(GioHang));
    }

    [HttpGet]
    public IActionResult DatHang()
    {
        var maKH = HttpContext.Session.GetInt32("MaKH");
        if (HttpContext.Session.GetString("Role") != "KhachHang" || maKH is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/CuaHang/DatHang" });
        }

        var customer = _db.KhachHang_CT((short)maKH.Value);
        if (customer == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account", new { returnUrl = "/CuaHang/DatHang" });
        }

        var cart = RefreshCart();
        if (!cart.Any()) return RedirectToAction(nameof(GioHang));
        if (cart.Any(x => !x.DangBan))
        {
            TempData["GioHangThongBao"] = "Có bánh không còn được bán. Vui lòng xóa các dòng đó trước khi đặt hàng.";
            return RedirectToAction(nameof(GioHang));
        }

        PrepareCheckoutView(cart);
        return View(new DatHangViewModel
        {
            TenNguoiNhan = $"{customer.HoKH} {customer.TenKH}".Trim(),
            DienThoaiNguoiNhan = customer.DienThoai ?? string.Empty,
            DiaChiGiaoHang = customer.DiaChiThuongDung ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DatHang(DatHangViewModel model)
    {
        var maKH = HttpContext.Session.GetInt32("MaKH");
        if (HttpContext.Session.GetString("Role") != "KhachHang" || maKH is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/CuaHang/DatHang" });
        }

        var customer = _db.KhachHang_CT((short)maKH.Value);
        if (customer == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account", new { returnUrl = "/CuaHang/DatHang" });
        }

        var cart = RefreshCart();
        if (!cart.Any()) ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống.");
        if (cart.Any(x => !x.DangBan))
        {
            TempData["GioHangThongBao"] = "Có bánh không còn được bán. Vui lòng kiểm tra lại giỏ hàng.";
            return RedirectToAction(nameof(GioHang));
        }

        if (!ModelState.IsValid)
        {
            PrepareCheckoutView(cart);
            return View(model);
        }

        model.TenNguoiNhan = model.TenNguoiNhan.Trim();
        model.DienThoaiNguoiNhan = model.DienThoaiNguoiNhan.Trim();
        model.DiaChiGiaoHang = model.DiaChiGiaoHang.Trim();
        model.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim();

        var addressChanged = !string.Equals(
            model.DiaChiGiaoHang,
            customer.DiaChiThuongDung?.Trim(),
            StringComparison.Ordinal);

        try
        {
            var rows = cart.Select(x => new { maBienThe = x.MaBienThe, soLuong = x.SoLuong });
            var id = _db.DatHangWeb(
                (short)maKH.Value,
                model.TenNguoiNhan,
                model.DienThoaiNguoiNhan,
                model.DiaChiGiaoHang,
                JsonSerializer.Serialize(rows),
                model.GhiChu);

            // Thủ tục SQL tạo đơn và cập nhật địa chỉ mặc định trong cùng transaction.
            SaveCart(new List<GioHangItem>());
            TempData["DatHangDiaChiCapNhat"] = addressChanged;
            return RedirectToAction(nameof(DatHangThanhCong), new { maDonHang = id });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepareCheckoutView(cart);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult DatHangThanhCong(string? maDonHang)
    {
        var maKH = HttpContext.Session.GetInt32("MaKH");
        if (HttpContext.Session.GetString("Role") != "KhachHang" || maKH is null)
        {
            var returnUrl = Url.Action(nameof(DatHangThanhCong), "CuaHang", new { maDonHang });
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(maDonHang) || maDonHang.Length != 11)
            return NotFound();

        var order = _db.DonHang_CT(maDonHang);
        if (order == null || order.MaKH != (short)maKH.Value)
            return NotFound();

        ViewBag.DiaChiMacDinhDaCapNhat =
            TempData["DatHangDiaChiCapNhat"] is bool updated && updated;

        return View(order);
    }

    private void PrepareCheckoutView(List<GioHangItem> cart)
    {
        ViewBag.CheckoutItems = cart;
        ViewBag.CheckoutTotal = cart
            .Where(item => item.DangBan)
            .Sum(item => item.DonGia * item.SoLuong);
    }

    public IActionResult DonDatHang()
    {
        if (HttpContext.Session.GetString("Role")!="KhachHang") return RedirectToAction("Login","Account");
        var id=HttpContext.Session.GetInt32("MaKH");
        return View(_db.DonHang_DS().Where(x=>x.MaKH==id).ToList());
    }

    // Đối chiếu session với database để giỏ hiển thị giá và trạng thái hiện tại.
    private List<GioHangItem> RefreshCart()
    {
        var cart = ReadCart();
        if (cart.Count == 0) return cart;

        var variants = _db.BienTheBanh_DS().ToDictionary(x => x.MaBienThe);
        var products = _db.SanPham_DS().ToDictionary(x => x.MaSP);
        var images = _db.HinhAnhBanh_DS()
            .Where(x => !string.IsNullOrWhiteSpace(x.DuongDanAnh))
            .OrderByDescending(x => x.LaAnhDaiDien)
            .ThenBy(x => x.ThuTuHienThi)
            .ThenBy(x => x.MaAnh)
            .ToLookup(x => x.MaSP);
        var priceChanged = false;

        foreach (var row in cart)
        {
            row.DangBan = false;
            if (!variants.TryGetValue(row.MaBienThe, out var variant)) continue;
            row.MaSP = variant.MaSP;
            row.TenBienThe = variant.TenBienThe;
            row.DuongDanAnh = images[variant.MaSP].FirstOrDefault()?.DuongDanAnh;
            if (!products.TryGetValue(variant.MaSP, out var product)) continue;
            row.TenSP = product.TenSP;
            row.DangBan = variant.TrangThaiBan && product.TrangThaiHienThi == "PUBLISHED";
            if (row.DangBan)
            {
                if (row.DonGia != variant.GiaBan) priceChanged = true;
                row.DonGia = variant.GiaBan;
            }
        }

        SaveCart(cart);
        if (priceChanged)
        {
            var previous = TempData["GioHangThongBao"] as string;
            TempData["GioHangThongBao"] = (string.IsNullOrWhiteSpace(previous) ? "" : previous + " ")
                + "Giá của một số bánh đã thay đổi; giỏ đã cập nhật theo giá bán hiện tại.";
        }
        return cart;
    }

    private List<GioHangItem> ReadCart() => JsonSerializer.Deserialize<List<GioHangItem>>(HttpContext.Session.GetString(CartKey) ?? "[]") ?? new();
    private void SaveCart(List<GioHangItem> cart) => HttpContext.Session.SetString(CartKey,JsonSerializer.Serialize(cart));
}

