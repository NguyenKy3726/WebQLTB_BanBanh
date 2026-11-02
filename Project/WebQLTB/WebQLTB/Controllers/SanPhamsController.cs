using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;
using WebQLTB.Services;

[PermissionAuthorize("ADMIN")]
public class SanPhamsController : Controller
{
    private readonly AppDbContext _context;
    private readonly InventoryThresholdStore _inventoryThresholds;

    public SanPhamsController(AppDbContext context, InventoryThresholdStore inventoryThresholds)
    {
        _context = context;
        _inventoryThresholds = inventoryThresholds;
    }

    // GET: SANPHAMS
    public IActionResult Index(byte? maloai, string? trangthai, string? q)
    {
        var sanPhams = _context.SanPham_DS();
        if (maloai.HasValue)
            sanPhams = sanPhams.Where(sp => sp.MaLoaiBanh == maloai.Value).ToList();
        if (!string.IsNullOrWhiteSpace(trangthai))
            sanPhams = sanPhams.Where(sp => sp.TrangThaiHienThi == trangthai).ToList();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            sanPhams = sanPhams.Where(sp =>
                sp.TenSP.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                sp.MaSP.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        ViewBag.NguongTonThap = _inventoryThresholds.GetThreshold();
        ViewBag.MaLoaiBanhDangChon = maloai;
        ViewBag.TrangThaiDangChon = trangthai;
        ViewBag.TuKhoa = q;
        ViewBag.LoaiBanhLoc = new SelectList(_context.LoaiBanh_DS(), "MaLoaiBanh", "TenLoaiBanh", maloai);
        PrepareListData(sanPhams);
        if (TempData["ThongBaoSanPham"] is string message)
            ViewBag.ThongBao = message;
        return View(sanPhams);
    }

    // GET: SANPHAMS/Details/5
    public async Task<IActionResult> Details(string? masp)
    {
        if (string.IsNullOrWhiteSpace(masp))
        {
            return NotFound();
        }

        var sanpham = _context.SanPham_CT(masp);
        if (sanpham == null)
        {
            return NotFound();
        }
        PrepareDetailsData(masp);
        if (TempData["ThongBaoSanPham"] is string message)
            ViewBag.ThongBao = message;
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
                TempData["ThongBaoSanPham"] = "Đã tạo sản phẩm. Mở sản phẩm để quản lý biến thể và hình ảnh.";
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

    private void PrepareDetailsData(string masp)
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
        ViewBag.Variants = _context.BienTheBanh_DS()
            .Where(variant => variant.MaSP == masp)
            .OrderBy(variant => variant.GiaBan)
            .ThenBy(variant => variant.MaBienThe)
            .ToList();
        ViewBag.Images = _context.HinhAnhBanh_DS()
            .Where(image => image.MaSP == masp)
            .OrderByDescending(image => image.LaAnhDaiDien)
            .ThenBy(image => image.ThuTuHienThi)
            .ThenBy(image => image.MaAnh)
            .ToList();
        ViewBag.NguongTonThap = _inventoryThresholds.GetThreshold();
        var images = (List<HinhAnhBanh>)ViewBag.Images;
        ViewBag.ThuTuAnhTiepTheo = images.Count == 0 ? 1 : images.Max(image => (int)image.ThuTuHienThi) + 1;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveStockThreshold(int? nguongTonThap)
    {
        if (!nguongTonThap.HasValue || nguongTonThap.Value is < 0 or > 100000)
        {
            TempData["ThongBaoSanPham"] = "Ngưỡng tồn phải là số nguyên từ 0 đến 100.000.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _inventoryThresholds.SaveThreshold(nguongTonThap.Value);
            TempData["ThongBaoSanPham"] = nguongTonThap.Value == 0
                ? "Đã lưu ngưỡng 0: cảnh báo tồn thấp đang tắt."
                : $"Đã lưu ngưỡng cảnh báo: báo khi từng biến thể còn dưới {nguongTonThap.Value} bánh khả dụng.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TempData["ThongBaoSanPham"] = "Không thể lưu ngưỡng. Hãy kiểm tra quyền ghi thư mục ứng dụng.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddVariant(string masp, [Bind("TenBienThe,GiaBan,SoLuongThucTe,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh variant)
    {
        if (string.IsNullOrWhiteSpace(masp) || _context.SanPham_CT(masp) == null) return NotFound();

        if (!ModelState.IsValid || !ValidateVariant(variant))
            return RedirectToDetailsWithMessage(masp, "Kiểm tra tên biến thể, giá nguyên không âm, tồn kho và thời gian chuẩn bị.");

        variant.MaSP = masp;
        variant.SoLuongDaGiu = 0;
        try
        {
            _context.BienTheBanh_Them(variant);
            return RedirectToDetailsWithMessage(masp, "Đã thêm biến thể bánh.");
        }
        catch (SqlException ex)
        {
            return RedirectToDetailsWithMessage(masp, SqlErrorMessage.ForUser(ex, "Không thể thêm biến thể lúc này."));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveVariant([Bind("MaBienThe,TenBienThe,GiaBan,SoLuongThucTe,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh variant)
    {
        var current = _context.BienTheBanh_CT(variant.MaBienThe);
        if (current == null) return NotFound();

        if (!ModelState.IsValid || !ValidateVariant(variant))
            return RedirectToDetailsWithMessage(current.MaSP, "Kiểm tra tên biến thể, giá nguyên không âm, tồn kho và thời gian chuẩn bị.");

        current.TenBienThe = variant.TenBienThe;
        current.GiaBan = variant.GiaBan;
        current.SoLuongThucTe = variant.SoLuongThucTe;
        current.SoNgayChuanBiDuKien = variant.SoNgayChuanBiDuKien;
        current.TrangThaiBan = variant.TrangThaiBan;
        try
        {
            _context.BienTheBanh_Sua(current);
            return RedirectToDetailsWithMessage(current.MaSP, "Đã lưu giá và tồn kho của biến thể.");
        }
        catch (SqlException ex)
        {
            return RedirectToDetailsWithMessage(current.MaSP, SqlErrorMessage.ForUser(ex, "Không thể cập nhật biến thể lúc này."));
        }
    }

    private static bool ValidateVariant(BienTheBanh variant) =>
        !string.IsNullOrWhiteSpace(variant.TenBienThe) &&
        variant.GiaBan >= 0 &&
        decimal.Truncate(variant.GiaBan) == variant.GiaBan &&
        variant.SoLuongThucTe >= 0 &&
        variant.SoNgayChuanBiDuKien >= 0;

    private IActionResult RedirectToDetailsWithMessage(string masp, string message)
    {
        TempData["ThongBaoSanPham"] = message;
        return RedirectToAction(nameof(Details), new { masp });
    }
}
