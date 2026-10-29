using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("SALES")]
public class ThanhToansController : Controller
{
    private readonly AppDbContext _context;

    public ThanhToansController(AppDbContext context)
    {
        _context = context;
    }

    // GET: THANHTOANS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenLoaiGiaoDich = new Dictionary<string, string>
        {
            { "PAYMENT", "Thu tiền" },
            { "REFUND", "Hoàn tiền" }
        };

        ViewBag.TenPhuongThuc = new Dictionary<string, string>
        {
            { "CASH", "Tiền mặt" },
            { "BANK_TRANSFER", "Chuyển khoản" }
        };

        ViewBag.TenTrangThaiThanhToan = new Dictionary<string, string>
        {
            { "PENDING", "Chờ xác nhận" },
            { "SUCCESS", "Thành công" },
            { "REJECTED", "Đã từ chối" }
        };
        return View(_context.ThanhToan_DS());
    }

    // GET: THANHTOANS/Details/5
    public async Task<IActionResult> Details(string? mathanhtoan)
    {
        if (mathanhtoan == null)
        {
            return NotFound();
        }

        var thanhtoan = _context.ThanhToan_CT(mathanhtoan);
        if (thanhtoan == null)
        {
            return NotFound();
        }
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenLoaiGiaoDich = new Dictionary<string, string>
        {
            { "PAYMENT", "Thu tiền" },
            { "REFUND", "Hoàn tiền" }
        };

        ViewBag.TenPhuongThuc = new Dictionary<string, string>
        {
            { "CASH", "Tiền mặt" },
            { "BANK_TRANSFER", "Chuyển khoản" }
        };

        ViewBag.TenTrangThaiThanhToan = new Dictionary<string, string>
        {
            { "PENDING", "Chờ xác nhận" },
            { "SUCCESS", "Thành công" },
            { "REJECTED", "Đã từ chối" }
        };
        return View(thanhtoan);
    }

    // GET: THANHTOANS/Create
    public IActionResult Create()
    {
        ViewBag.MaDonHang = new SelectList(
            _context.DonHang_DS(),
            "MaDonHang",
            "MaDonHang"
        );

        ViewBag.LoaiGiaoDich = new SelectList(
            new[]
            {
                new { GiaTri = "PAYMENT", HienThi = "Thu tiền" },
                new { GiaTri = "REFUND", HienThi = "Hoàn tiền" }
            },
            "GiaTri",
            "HienThi",
            "PAYMENT"
        );

        ViewBag.PhuongThuc = new SelectList(
            new[]
            {
                new { GiaTri = "CASH", HienThi = "Tiền mặt" },
                new { GiaTri = "BANK_TRANSFER", HienThi = "Chuyển khoản" }
            },
            "GiaTri",
            "HienThi"
        );

        ViewBag.MaThanhToanGoc = new SelectList(
            _context.ThanhToan_DS()
                .Where(tt => tt.LoaiGiaoDich == "PAYMENT"
                          && tt.TrangThaiThanhToan == "SUCCESS")
                .Select(tt => new
                {
                    tt.MaThanhToan,
                    HienThi = $"{tt.MaDonHang} - {tt.MaThanhToan} - {tt.SoTien:N0} đ"
                }),
            "MaThanhToan",
            "HienThi"
        );
        return View();
    }

    // POST: THANHTOANS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaThanhToan,MaDonHang,MaThanhToanGoc,LoaiGiaoDich,SoTien,PhuongThuc,TrangThaiThanhToan,ThoiGianTao,ThoiGianXacNhan,MaNVXacNhan,DuongDanChungTu,GhiChu")] ThanhToan thanhtoan)
    {
        if (ModelState.IsValid)
        {
            _context.ThanhToan_Them(thanhtoan); 
            return RedirectToAction(nameof(Index));
        }
        return View(thanhtoan);
    }

    // GET: THANHTOANS/Edit/5
    public async Task<IActionResult> Edit(string? mathanhtoan)
    {
        if (mathanhtoan == null)
        {
            return NotFound();
        }

        var thanhtoan = _context.ThanhToan_CT(mathanhtoan);
        if (thanhtoan == null)
        {
            return NotFound();
        }
        ViewBag.PhuongThuc = new SelectList(
            new[]
            {
                new { GiaTri = "CASH", HienThi = "Tiền mặt" },
                new { GiaTri = "BANK_TRANSFER", HienThi = "Chuyển khoản" }
            },
            "GiaTri",
            "HienThi",
            thanhtoan.PhuongThuc
        );

        ViewBag.TrangThaiThanhToan = new SelectList(
            new[]
            {
                new { GiaTri = "PENDING", HienThi = "Chờ xác nhận" },
                new { GiaTri = "SUCCESS", HienThi = "Thành công" },
                new { GiaTri = "REJECTED", HienThi = "Đã từ chối" }
            },
            "GiaTri",
            "HienThi",
            thanhtoan.TrangThaiThanhToan
        );

        ViewBag.MaNVXacNhan = new SelectList(
            _context.NhanVien_DS().Select(nv => new
            {
                nv.MaNV,
                HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
            }),
            "MaNV",
            "HienThi",
            thanhtoan.MaNVXacNhan
        );
        return View(thanhtoan);
    }

    // POST: THANHTOANS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? mathanhtoan, [Bind("MaThanhToan,MaDonHang,MaThanhToanGoc,LoaiGiaoDich,SoTien,PhuongThuc,TrangThaiThanhToan,ThoiGianTao,ThoiGianXacNhan,MaNVXacNhan,DuongDanChungTu,GhiChu")] ThanhToan thanhtoan)
    {
        if (mathanhtoan != thanhtoan.MaThanhToan)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.ThanhToan_Sua(thanhtoan);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThanhToanExists(thanhtoan.MaThanhToan))
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
        return View(thanhtoan);
    }

    // GET: THANHTOANS/Delete/5
    public async Task<IActionResult> Delete(string? mathanhtoan)
    {
        if (mathanhtoan == null)
        {
            return NotFound();
        }

        var thanhtoan = _context.ThanhToan_CT(mathanhtoan);
        if (thanhtoan == null)
        {
            return NotFound();
        }
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);

        ViewBag.TenLoaiGiaoDich = new Dictionary<string, string>
        {
            { "PAYMENT", "Thu tiền" },
            { "REFUND", "Hoàn tiền" }
        };

        ViewBag.TenPhuongThuc = new Dictionary<string, string>
        {
            { "CASH", "Tiền mặt" },
            { "BANK_TRANSFER", "Chuyển khoản" }
        };

        ViewBag.TenTrangThaiThanhToan = new Dictionary<string, string>
        {
            { "PENDING", "Chờ xác nhận" },
            { "SUCCESS", "Thành công" },
            { "REJECTED", "Đã từ chối" }
        };
        return View(thanhtoan);
    }

    // POST: THANHTOANS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? mathanhtoan)
    {
        if (mathanhtoan == null) return NotFound();

        var thanhToan = _context.ThanhToan_CT(mathanhtoan);
        if (thanhToan == null) return NotFound();

        try
        {
            _context.ThanhToan_Xoa(mathanhtoan);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", thanhToan);
        }
    }

    private bool ThanhToanExists(string? mathanhtoan)
    {
        return _context.ThanhToans.Any(e => e.MaThanhToan == mathanhtoan);
    }
}
