using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("SALES")]
public class KhachHangsController : Controller
{
    private readonly AppDbContext _context;

    public KhachHangsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: KHACHHANGS
    public async Task<IActionResult> Index()    
    {
        return View(_context.KhachHang_DS());
    }

    // GET: KHACHHANGS/Details/5
    public async Task<IActionResult> Details(short? makh)
    {
        if (makh == null)
        {
            return NotFound();
        }

        var khachhang = _context.KhachHang_CT(makh.Value);
        if (khachhang == null)
        {
            return NotFound();
        }

        return View(khachhang);
    }

    // GET: KHACHHANGS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: KHACHHANGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaKH,HoKH,TenKH,DienThoai,Email,DiaChiThuongDung,TenDN")] KhachHang khachhang, string? matKhauMoi)
    {
        ModelState.Remove(nameof(KhachHang.MatKhau));

        if (string.IsNullOrWhiteSpace(matKhauMoi))
        {
            ModelState.AddModelError(
                "matKhauMoi",
                "Vui lòng nhập mật khẩu.");
        }
        else if (matKhauMoi.Length > 50)
        {
            ModelState.AddModelError(
                "matKhauMoi",
                "Mật khẩu không được vượt quá 50 ký tự.");
        }

        if (!ModelState.IsValid)
        {
            return View(khachhang);
        }

        try
        {
            _context.KhachHang_Them(khachhang, matKhauMoi);
            return RedirectToAction(nameof(Index));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(khachhang);
        }
    }

    // GET: KHACHHANGS/Edit/5
    public async Task<IActionResult> Edit(short? makh)
    {
        if (makh == null)
        {
            return NotFound();
        }

        var khachhang = _context.KhachHang_CT(makh.Value);
        if (khachhang == null)
        {
            return NotFound();
        }
        return View(khachhang);
    }

    // POST: KHACHHANGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(short? makh, [Bind("MaKH,HoKH,TenKH,DienThoai,Email,DiaChiThuongDung,TenDN")] KhachHang khachhang, string? matKhauMoi)
    {
        if (makh != khachhang.MaKH)
        {
            return NotFound();
        }

        // MatKhau lưu dạng varbinary trong DB, không nhận trường này từ form sửa.
        ModelState.Remove(nameof(KhachHang.MatKhau));

        if (matKhauMoi?.Length > 50)
        {
            ModelState.AddModelError(
                "matKhauMoi",
                "Mật khẩu không được vượt quá 50 ký tự.");
        }

        if (!ModelState.IsValid)
        {
            return View(khachhang);
        }

        try
        {
            _context.KhachHang_Sua(khachhang, matKhauMoi);
            return RedirectToAction(nameof(Index));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(khachhang);
        }
    }

    // GET: KHACHHANGS/Delete/5
    public async Task<IActionResult> Delete(short? makh)
    {
        if (makh == null)
        {
            return NotFound();
        }

        var khachhang = _context.KhachHang_CT(makh.Value);
        if (khachhang == null)
        {
            return NotFound();
        }

        return View(khachhang);
    }

    // POST: KHACHHANGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(short? makh)
    {
        if (makh == null) return NotFound();

        var khachHang = _context.KhachHang_CT(makh.Value);
        if (khachHang == null) return NotFound();

        try
        {
            _context.KhachHang_Xoa(makh.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", khachHang);
        }
    }


    private bool KhachHangExists(short? makh)
    {
        return _context.KhachHangs.Any(e => e.MaKH == makh);
    }
}
