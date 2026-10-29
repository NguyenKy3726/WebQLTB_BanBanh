using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class NhanViensController : Controller
{
    private readonly AppDbContext _context;

    public NhanViensController(AppDbContext context)
    {
        _context = context;
    }

    // GET: NHANVIENS
    public async Task<IActionResult> Index()    
    {
        return View(_context.NhanVien_DS());
    }

    // GET: NHANVIENS/Details/5
    public async Task<IActionResult> Details(byte? manv)
    {
        if (manv == null)
        {
            return NotFound();
        }

        var nhanvien = _context.NhanVien_CT(manv.Value);
        if (nhanvien == null)
        {
            return NotFound();
        }

        return View(nhanvien);
    }

    // GET: NHANVIENS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NHANVIENS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaNV,HoNV,TenNV,DienThoai,Email,TenDN,AnhNV")] NhanVien nhanvien, string? matKhauMoi)
    {
        ModelState.Remove(nameof(NhanVien.MatKhau));

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
            return View(nhanvien);
        }

        try
        {
            _context.NhanVien_Them(nhanvien, matKhauMoi);
            return RedirectToAction(nameof(Index));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nhanvien);
        }
    }

    // GET: NHANVIENS/Edit/5
    public async Task<IActionResult> Edit(byte? manv)
    {
        if (manv == null)
        {
            return NotFound();
        }

        var nhanvien = _context.NhanVien_CT(manv.Value);
        if (nhanvien == null)
        {
            return NotFound();
        }
        return View(nhanvien);
    }

    // POST: NHANVIENS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(byte? manv, [Bind("MaNV,HoNV,TenNV,DienThoai,Email,TenDN,MatKhau,AnhNV")] NhanVien nhanvien, string? matKhauMoi)
    {
        if (manv != nhanvien.MaNV)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(NhanVien.MatKhau));

        if (matKhauMoi?.Length > 50)
        {
            ModelState.AddModelError(
                "matKhauMoi",
                "Mật khẩu không được vượt quá 50 ký tự.");
        }

        if (!ModelState.IsValid)
        {
            return View(nhanvien);
        }

        try
        {
            _context.NhanVien_Sua(nhanvien, matKhauMoi);
            return RedirectToAction(nameof(Index));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nhanvien);
        }
    }

    // GET: NHANVIENS/Delete/5
    public async Task<IActionResult> Delete(byte? manv)
    {
        if (manv == null)
        {
            return NotFound();
        }

        var nhanvien = _context.NhanVien_CT(manv.Value);
        if (nhanvien == null)
        {
            return NotFound();
        }

        return View(nhanvien);
    }

    // POST: NHANVIENS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(byte? manv)
    {
        if (manv == null) return NotFound();

        var nhanVien = _context.NhanVien_CT(manv.Value);
        if (nhanVien == null) return NotFound();

        try
        {
            _context.NhanVien_Xoa(manv.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", nhanVien);
        }
    }

    private bool NhanVienExists(byte? manv)
    {
        return _context.NhanViens.Any(e => e.MaNV == manv);
    }
}
