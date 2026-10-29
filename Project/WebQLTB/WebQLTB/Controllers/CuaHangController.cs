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
        var categories = _db.LoaiBanh_DS().ToDictionary(x => x.MaLoaiBanh, x => x.TenLoaiBanh);
        ViewBag.Categories = categories;
        return View(_db.SanPham_DS().Where(x => x.TrangThaiHienThi == "PUBLISHED").ToList());
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
        if (soLuong < 1 || soLuong > 50) return BadRequest("Số lượng phải từ 1 đến 50.");
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

    public IActionResult GioHang() => View(ReadCart());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult XoaKhoiGio(short maBienThe)
    {
        var cart=ReadCart(); cart.RemoveAll(x=>x.MaBienThe==maBienThe); SaveCart(cart);
        return RedirectToAction(nameof(GioHang));
    }

    [HttpGet]
    public IActionResult DatHang()
    {
        if (HttpContext.Session.GetString("Role") != "KhachHang") return RedirectToAction("Login", "Account", new { returnUrl="/CuaHang/DatHang" });
        if (!ReadCart().Any()) return RedirectToAction(nameof(GioHang));
        return View(new DatHangViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DatHang(DatHangViewModel model)
    {
        var maKH=HttpContext.Session.GetInt32("MaKH");
        if (HttpContext.Session.GetString("Role")!="KhachHang" || maKH is null) return RedirectToAction("Login", "Account");
        var cart=ReadCart();
        if (!cart.Any()) ModelState.AddModelError("", "Giỏ hàng đang trống.");
        if (!ModelState.IsValid) return View(model);
        try
        {
            var rows=cart.Select(x=>new { maBienThe=x.MaBienThe, soLuong=x.SoLuong });
            var id=_db.DatHangWeb((short)maKH.Value,model.TenNguoiNhan,model.DienThoaiNguoiNhan,model.DiaChiGiaoHang,JsonSerializer.Serialize(rows),model.GhiChu);
            SaveCart(new());
            TempData["ThongBao"]=$"Đã gửi đơn {id}. Nhân viên sẽ kiểm tra và phản hồi.";
            return RedirectToAction(nameof(DonDatHang));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) { ModelState.AddModelError("",ex.Message); return View(model); }
    }

    public IActionResult DonDatHang()
    {
        if (HttpContext.Session.GetString("Role")!="KhachHang") return RedirectToAction("Login","Account");
        var id=HttpContext.Session.GetInt32("MaKH");
        return View(_db.DonHang_DS().Where(x=>x.MaKH==id).ToList());
    }

    private List<GioHangItem> ReadCart() => JsonSerializer.Deserialize<List<GioHangItem>>(HttpContext.Session.GetString(CartKey) ?? "[]") ?? new();
    private void SaveCart(List<GioHangItem> cart) => HttpContext.Session.SetString(CartKey,JsonSerializer.Serialize(cart));
}
