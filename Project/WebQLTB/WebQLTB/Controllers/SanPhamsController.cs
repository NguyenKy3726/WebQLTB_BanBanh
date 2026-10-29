using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class SanPhamsController : Controller
{
    private readonly AppDbContext _context;

    public SanPhamsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: SANPHAMS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenLoaiBanh = _context.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenTrangThaiHienThi = new Dictionary<string, string>
        {
            { "DRAFT", "Đang soạn" },
            { "PUBLISHED", "Đang hiển thị" },
            { "HIDDEN", "Tạm ẩn" }
        };
        return View(_context.SanPham_DS());
    }

    // GET: SANPHAMS/Details/5
    public async Task<IActionResult> Details(string? masp)
    {
        if (masp == null)
        {
            return NotFound();
        }

        var sanpham = _context.SanPham_CT(masp);
        if (sanpham == null)
        {
            return NotFound();
        }
        ViewBag.TenLoaiBanh = _context.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenTrangThaiHienThi = new Dictionary<string, string>
        {
            { "DRAFT", "Đang soạn" },
            { "PUBLISHED", "Đang hiển thị" },
            { "HIDDEN", "Tạm ẩn" }
        };
        return View(sanpham);
    }

    // GET: SANPHAMS/Create
    public IActionResult Create()
    {
        ViewBag.MaLoaiBanh = new SelectList(
            _context.LoaiBanh_DS(),
            "MaLoaiBanh",
            "TenLoaiBanh"
        );

        ViewBag.TrangThaiHienThi = new SelectList(
            new[]
            {
                new { GiaTri = "DRAFT", HienThi = "Đang soạn" },
                new { GiaTri = "PUBLISHED", HienThi = "Đang hiển thị" },
                new { GiaTri = "HIDDEN", HienThi = "Tạm ẩn" }
            },
            "GiaTri",
            "HienThi",
            "DRAFT"
        );
        return View();
    }

    // POST: SANPHAMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaSP,MaLoaiBanh,TenSP,MoTaNgan,MoTaChiTiet,TrangThaiHienThi,MaNVTao,NgayTao,MaNVCapNhat,NgayCapNhat")] SanPham sanpham)
    {
        if (ModelState.IsValid)
        {
            _context.SanPham_Them(sanpham); 
            return RedirectToAction(nameof(Index));
        }
        return View(sanpham);
    }

    // GET: SANPHAMS/Edit/5
    public async Task<IActionResult> Edit(string? masp)
    {
        if (masp == null)
        {
            return NotFound();
        }

        var sanpham = _context.SanPham_CT(masp);
        if (sanpham == null)
        {
            return NotFound();
        }
        ViewBag.MaLoaiBanh = new SelectList(
            _context.LoaiBanh_DS(),
            "MaLoaiBanh",
            "TenLoaiBanh"
        );

        ViewBag.TrangThaiHienThi = new SelectList(
            new[]
            {
                new { GiaTri = "DRAFT", HienThi = "Đang soạn" },
                new { GiaTri = "PUBLISHED", HienThi = "Đang hiển thị" },
                new { GiaTri = "HIDDEN", HienThi = "Tạm ẩn" }
            },
            "GiaTri",
            "HienThi",
            sanpham.TrangThaiHienThi
        );
        return View(sanpham);
    }

    // POST: SANPHAMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? masp, [Bind("MaSP,MaLoaiBanh,TenSP,MoTaNgan,MoTaChiTiet,TrangThaiHienThi,MaNVTao,NgayTao,MaNVCapNhat,NgayCapNhat")] SanPham sanpham)
    {
        if (masp != sanpham.MaSP)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.SanPham_Sua(sanpham);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SanPhamExists(sanpham.MaSP))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(sanpham);
    }

    // GET: SANPHAMS/Delete/5
    public async Task<IActionResult> Delete(string? masp)
    {
        if (masp == null)
        {
            return NotFound();
        }

        var sanpham = _context.SanPham_CT(masp);
        if (sanpham == null)
        {
            return NotFound();
        }

        return View(sanpham);
    }

    // POST: SANPHAMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? masp)
    {
        if (masp == null) return NotFound();

        var sanPham = _context.SanPham_CT(masp);
        if (sanPham == null) return NotFound();

        try
        {
            _context.SanPham_Xoa(masp);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", sanPham);
        }
    }

    private bool SanPhamExists(string? masp)
    {
        return _context.SanPhams.Any(e => e.MaSP == masp);
    }
}
