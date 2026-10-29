using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("SALES")]
public class DonHangsController : Controller
{
    private readonly AppDbContext _context;

    public DonHangsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: DONHANGS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenKH = _context.KhachHang_DS()
            .ToDictionary(kh => kh.MaKH, kh => kh.HoKH + " " + kh.TenKH);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenTrangThaiDonHang = new Dictionary<string, string>
            {
                { "CHO_NHAN_VIEN_XU_LY", "Chờ nhân viên xử lý" },
                { "CHO_KHACH_XAC_NHAN", "Chờ khách xác nhận" },
                { "CHO_THANH_TOAN_COC", "Chờ thanh toán cọc" },
                { "DA_XAC_NHAN", "Đã xác nhận" },
                { "DANG_CHUAN_BI", "Đang chuẩn bị" },
                { "SAN_SANG_GIAO", "Sẵn sàng giao" },
                { "DANG_GIAO", "Đang giao" },
                { "HOAN_TAT", "Hoàn tất" },
                { "DA_HUY", "Đã hủy" }
            };
        return View(_context.DonHang_DS());
    }

    // GET: DONHANGS/Details/5
    public async Task<IActionResult> Details(string? madonhang)
    {
        if (madonhang == null)
        {
            return NotFound();
        }

        var donhang = _context.DonHang_CT(madonhang);
        if (donhang == null)
        {
            return NotFound();
        }
        ViewBag.TenKH = _context.KhachHang_DS()
            .ToDictionary(kh => kh.MaKH, kh => kh.HoKH + " " + kh.TenKH);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenTrangThaiDonHang = new Dictionary<string, string>
            {
                { "CHO_NHAN_VIEN_XU_LY", "Chờ nhân viên xử lý" },
                { "CHO_KHACH_XAC_NHAN", "Chờ khách xác nhận" },
                { "CHO_THANH_TOAN_COC", "Chờ thanh toán cọc" },
                { "DA_XAC_NHAN", "Đã xác nhận" },
                { "DANG_CHUAN_BI", "Đang chuẩn bị" },
                { "SAN_SANG_GIAO", "Sẵn sàng giao" },
                { "DANG_GIAO", "Đang giao" },
                { "HOAN_TAT", "Hoàn tất" },
                { "DA_HUY", "Đã hủy" }
            };
        return View(donhang);
    }

    // GET: DONHANGS/Create
    public IActionResult Create()
    {
        ViewBag.MaKH = new SelectList(
            _context.KhachHang_DS().Select(kh => new
            {
                kh.MaKH,
                HienThi = $"{kh.MaKH} - {kh.HoKH} {kh.TenKH}"
            }),
            "MaKH",
            "HienThi"
        );

        ViewBag.MaNVXuLy = new SelectList(
            _context.NhanVien_DS().Select(nv => new
            {
                nv.MaNV,
                HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
            }),
            "MaNV",
            "HienThi"
        );
        return View();
    }

    // POST: DONHANGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaDonHang,MaKH,MaNVXuLy,MaNVXacNhan,MaNVHoanTat,NgayDat,NgayXacNhan,NgayHoanTat,NgayGiaoDuKien,TrangThaiDonHang,TenNguoiNhan,DienThoaiNguoiNhan,DiaChiGiaoHang,PhiGiaoHang,TienGiamGia,TienCocYeuCau,TongTienHang,TongTienPhaiTra,GhiChu")] DonHang donhang)
    {
        if (ModelState.IsValid)
        {
            _context.DonHang_Them(donhang);
            return RedirectToAction(nameof(Index));
        }
        return View(donhang);
    }

    // GET: DONHANGS/Edit/5
    public async Task<IActionResult> Edit(string? madonhang)
    {
        if (madonhang == null)
        {
            return NotFound();
        }

        var donhang = _context.DonHang_CT(madonhang);
        if (donhang == null)
        {
            return NotFound();
        }
        ViewBag.MaKH = new SelectList(
            _context.KhachHang_DS().Select(kh => new
            {
                kh.MaKH,
                HienThi = $"{kh.MaKH} - {kh.HoKH} {kh.TenKH}"
            }),
            "MaKH",
            "HienThi",
            donhang.MaKH
        );

        var nhanViens = _context.NhanVien_DS().Select(nv => new
        {
            nv.MaNV,
            HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
        }).ToList();

        ViewBag.MaNVXuLy = new SelectList(
            nhanViens,
            "MaNV",
            "HienThi",
            donhang.MaNVXuLy
        );

        ViewBag.MaNVXacNhan = new SelectList(
            nhanViens,
            "MaNV",
            "HienThi",
            donhang.MaNVXacNhan
        );

        ViewBag.MaNVHoanTat = new SelectList(
            nhanViens,
            "MaNV",
            "HienThi",
            donhang.MaNVHoanTat
        );

        ViewBag.TrangThaiDonHang = new SelectList(
            new[]
            {
                new { GiaTri = "CHO_NHAN_VIEN_XU_LY", HienThi = "Chờ nhân viên xử lý" },
                new { GiaTri = "CHO_KHACH_XAC_NHAN", HienThi = "Chờ khách xác nhận" },
                new { GiaTri = "CHO_THANH_TOAN_COC", HienThi = "Chờ thanh toán cọc" },
                new { GiaTri = "DA_XAC_NHAN", HienThi = "Đã xác nhận" },
                new { GiaTri = "DANG_CHUAN_BI", HienThi = "Đang chuẩn bị" },
                new { GiaTri = "SAN_SANG_GIAO", HienThi = "Sẵn sàng giao" },
                new { GiaTri = "DANG_GIAO", HienThi = "Đang giao" },
                new { GiaTri = "HOAN_TAT", HienThi = "Hoàn tất" },
                new { GiaTri = "DA_HUY", HienThi = "Đã hủy" }
            },
            "GiaTri",
            "HienThi",
            donhang.TrangThaiDonHang
        );
        return View(donhang);
    }

    // POST: DONHANGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? madonhang, [Bind("MaDonHang,MaKH,MaNVXuLy,MaNVXacNhan,MaNVHoanTat,NgayDat,NgayXacNhan,NgayHoanTat,NgayGiaoDuKien,TrangThaiDonHang,TenNguoiNhan,DienThoaiNguoiNhan,DiaChiGiaoHang,PhiGiaoHang,TienGiamGia,TienCocYeuCau,TongTienHang,TongTienPhaiTra,GhiChu")] DonHang donhang)
    {
        if (madonhang != donhang.MaDonHang)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.DonHang_Sua(donhang);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonHangExists(donhang.MaDonHang))
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
        return View(donhang);
    }

    // GET: DONHANGS/Delete/5
    public async Task<IActionResult> Delete(string? madonhang)
    {
        if (madonhang == null)
        {
            return NotFound();
        }

        var donhang = _context.DonHang_CT(madonhang);
        if (donhang == null)
        {
            return NotFound();
        }
        ViewBag.TenKH = _context.KhachHang_DS()
            .ToDictionary(kh => kh.MaKH, kh => kh.HoKH + " " + kh.TenKH);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenTrangThaiDonHang = new Dictionary<string, string>
            {
                { "CHO_NHAN_VIEN_XU_LY", "Chờ nhân viên xử lý" },
                { "CHO_KHACH_XAC_NHAN", "Chờ khách xác nhận" },
                { "CHO_THANH_TOAN_COC", "Chờ thanh toán cọc" },
                { "DA_XAC_NHAN", "Đã xác nhận" },
                { "DANG_CHUAN_BI", "Đang chuẩn bị" },
                { "SAN_SANG_GIAO", "Sẵn sàng giao" },
                { "DANG_GIAO", "Đang giao" },
                { "HOAN_TAT", "Hoàn tất" },
                { "DA_HUY", "Đã hủy" }
            };
        return View(donhang);
    }

    // POST: DONHANGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? madonhang)
    {
        if (madonhang == null) return NotFound();

        var donHang = _context.DonHang_CT(madonhang);
        if (donHang == null) return NotFound();

        try
        {
            _context.DonHang_Xoa(madonhang);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", donHang);
        }
    }

    private bool DonHangExists(string? madonhang)
    {
        return _context.DonHangs.Any(e => e.MaDonHang == madonhang);
    }
}
