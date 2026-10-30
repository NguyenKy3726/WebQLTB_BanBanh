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
        var sanPhams = _context.SanPham_DS();
        PrepareListData(sanPhams);
        if (TempData["ThongBaoSanPham"] is string message)
            ViewBag.ThongBao = message;
        return View(sanPhams);
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
        PrepareDetailsData();
        return View(sanpham);
    }

    // GET: SANPHAMS/Create
    public IActionResult Create()
    {
        PrepareFormData();
        return View();
    }

    // POST: SANPHAMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaLoaiBanh,TenSP,MoTaNgan,MoTaChiTiet,TrangThaiHienThi")] SanPham sanpham)
    {
        var maNV = CurrentEmployeeId();
        if (maNV is null)
        {
            return RedirectToAction("Login", "Account");
        }

        sanpham.MaNVTao = maNV.Value;
        if (ModelState.IsValid)
        {
            try
            {
                _context.SanPham_Them(sanpham);
                TempData["ThongBaoSanPham"] = "Đã tạo mẫu bánh. Tiếp theo, thêm phiên bản và hình ảnh cho bánh.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, SqlErrorMessage.ForUser(ex, "Không thể lưu sản phẩm lúc này. Vui lòng kiểm tra dữ liệu và thử lại."));
            }
        }

        PrepareFormData();
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
        PrepareFormData(sanpham.MaLoaiBanh, sanpham.TrangThaiHienThi);
        return View(sanpham);
    }

    // POST: SANPHAMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? masp, [Bind("MaSP,MaLoaiBanh,TenSP,MoTaNgan,MoTaChiTiet,TrangThaiHienThi")] SanPham sanpham)
    {
        if (masp != sanpham.MaSP)
        {
            return NotFound();
        }

        var current = _context.SanPham_CT(masp);
        if (current == null) return NotFound();

        var maNV = CurrentEmployeeId();
        if (maNV is null) return RedirectToAction("Login", "Account");

        // Giữ mã loại và thông tin tạo từ bản ghi hiện tại; không nhận các trường này từ form.
        sanpham.MaLoaiBanh = current.MaLoaiBanh;
        sanpham.MaNVTao = current.MaNVTao;
        sanpham.NgayTao = current.NgayTao;
        sanpham.MaNVCapNhat = maNV.Value;

        if (ModelState.IsValid)
        {
            try
            {
                _context.SanPham_Sua(sanpham);
                TempData["ThongBaoSanPham"] = "Đã cập nhật thông tin mẫu bánh.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, SqlErrorMessage.ForUser(ex, "Không thể cập nhật sản phẩm lúc này. Vui lòng thử lại."));
            }
        }

        PrepareFormData(current.MaLoaiBanh, sanpham.TrangThaiHienThi);
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
            ViewBag.LoiXoa = SqlErrorMessage.ForUser(ex, "Không thể xóa sản phẩm vì sản phẩm có thể đang được sử dụng.");
            return View("Delete", sanPham);
        }
    }

    private bool SanPhamExists(string? masp)
    {
        return _context.SanPhams.Any(e => e.MaSP == masp);
    }

    private byte? CurrentEmployeeId()
    {
        var maNV = HttpContext.Session.GetInt32("MaNV");
        return maNV is >= 1 and <= byte.MaxValue ? (byte)maNV.Value : null;
    }

    private void PrepareFormData(byte? selectedCategory = null, string? selectedStatus = null)
    {
        ViewBag.TenLoaiBanh = _context.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);
        ViewBag.MaLoaiBanh = new SelectList(
            _context.LoaiBanh_DS(), "MaLoaiBanh", "TenLoaiBanh", selectedCategory);

        ViewBag.TrangThaiHienThi = new SelectList(
            new[]
            {
                new { GiaTri = "DRAFT", HienThi = "Bản nháp" },
                new { GiaTri = "PUBLISHED", HienThi = "Đang hiển thị" },
                new { GiaTri = "HIDDEN", HienThi = "Tạm ẩn" }
            },
            "GiaTri", "HienThi", selectedStatus ?? "DRAFT");
    }

    private void PrepareListData(List<SanPham> sanPhams)
    {
        ViewBag.TenLoaiBanh = _context.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);
        ViewBag.TenTrangThaiHienThi = new Dictionary<string, string>
        {
            ["DRAFT"] = "Bản nháp",
            ["PUBLISHED"] = "Đang hiển thị",
            ["HIDDEN"] = "Tạm ẩn"
        };

        var maSPs = sanPhams.Select(sp => sp.MaSP).ToHashSet();
        ViewBag.AnhDaiDien = _context.HinhAnhBanh_DS()
            .Where(anh => anh.LaAnhDaiDien && maSPs.Contains(anh.MaSP))
            .ToDictionary(anh => anh.MaSP, anh => anh.DuongDanAnh);
        var variants = _context.BienTheBanh_DS()
            .Where(bt => maSPs.Contains(bt.MaSP))
            .ToList();
        ViewBag.SoBienThe = variants
            .GroupBy(bt => bt.MaSP)
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.GiaThapNhat = variants
            .Where(bt => bt.TrangThaiBan)
            .GroupBy(bt => bt.MaSP)
            .ToDictionary(group => group.Key, group => group.Min(bt => bt.GiaBan));
    }

    private void PrepareDetailsData()
    {
        ViewBag.TenLoaiBanh = _context.LoaiBanh_DS()
            .ToDictionary(lb => lb.MaLoaiBanh, lb => lb.TenLoaiBanh);
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);
        ViewBag.TenTrangThaiHienThi = new Dictionary<string, string>
        {
            ["DRAFT"] = "Bản nháp",
            ["PUBLISHED"] = "Đang hiển thị",
            ["HIDDEN"] = "Tạm ẩn"
        };
    }
}
