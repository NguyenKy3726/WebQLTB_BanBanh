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

    // GET: THANHTOANS — báo cáo doanh thu
    public IActionResult Index(int? thang, int? nam)
    {
        var giaoDich = _context.ThanhToan_DS();
        var homNay = DateTime.Today;
        var thangChon = thang is >= 1 and <= 12 ? thang.Value : homNay.Month;
        var cacNam = giaoDich
            .Select(x => (x.ThoiGianXacNhan ?? x.ThoiGianTao).Year)
            .Append(homNay.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();
        var namChon = nam.HasValue && cacNam.Contains(nam.Value) ? nam.Value : homNay.Year;
        var batDau = new DateTime(namChon, thangChon, 1);
        var ketThuc = batDau.AddMonths(1);
        var trongThang = giaoDich
            .Where(x =>
            {
                var ngayGiaoDich = x.ThoiGianXacNhan ?? x.ThoiGianTao;
                return ngayGiaoDich >= batDau && ngayGiaoDich < ketThuc;
            })
            .ToList();
        var thanhCong = trongThang.Where(x => x.TrangThaiThanhToan == "SUCCESS").ToList();
        var thuTien = thanhCong.Where(x => x.LoaiGiaoDich == "PAYMENT").ToList();
        var hoanTien = thanhCong.Where(x => x.LoaiGiaoDich == "REFUND").ToList();
        var soNgay = DateTime.DaysInMonth(namChon, thangChon);
        var theoNgay = Enumerable.Range(1, soNgay).Select(ngay =>
        {
            var ngayBaoCao = new DateTime(namChon, thangChon, ngay);
            var thuTrongNgay = thuTien
                .Where(x => x.ThoiGianXacNhan?.Date == ngayBaoCao.Date)
                .Sum(x => x.SoTien);
            var hoanTrongNgay = hoanTien
                .Where(x => x.ThoiGianXacNhan?.Date == ngayBaoCao.Date)
                .Sum(x => x.SoTien);
            return new DoanhThuTheoNgayViewModel
            {
                Ngay = ngayBaoCao,
                TienThu = thuTrongNgay,
                TienHoan = hoanTrongNgay,
                DoanhThuThuan = thuTrongNgay - hoanTrongNgay
            };
        }).ToList();
        var mucThuCaoNhat = theoNgay.Max(x => x.TienThu);
        foreach (var ngay in theoNgay)
        {
            ngay.ChieuCaoCot = mucThuCaoNhat <= 0 || ngay.TienThu <= 0
                ? 0
                : Math.Max(4, (int)Math.Round(ngay.TienThu / mucThuCaoNhat * 100));
        }

        var model = new BaoCaoDoanhThuViewModel
        {
            Thang = thangChon,
            Nam = namChon,
            CacNam = cacNam,
            TongTienThu = thuTien.Sum(x => x.SoTien),
            TongTienHoan = hoanTien.Sum(x => x.SoTien),
            DoanhThuThuan = thuTien.Sum(x => x.SoTien) - hoanTien.Sum(x => x.SoTien),
            SoGiaoDichThu = thuTien.Count,
            SoGiaoDichHoan = hoanTien.Count,
            SoDonHangCoThanhToan = thuTien.Select(x => x.MaDonHang).Distinct().Count(),
            TheoNgay = theoNgay,
            GiaoDich = trongThang.OrderByDescending(x => x.ThoiGianXacNhan ?? x.ThoiGianTao)
                .Take(40)
                .Select(x => new BaoCaoGiaoDichViewModel
                {
                    MaThanhToan = x.MaThanhToan,
                    MaDonHang = x.MaDonHang,
                    LoaiGiaoDich = x.LoaiGiaoDich,
                    SoTien = x.SoTien,
                    PhuongThuc = x.PhuongThuc,
                    TrangThai = x.TrangThaiThanhToan,
                    ThoiGian = x.ThoiGianXacNhan ?? x.ThoiGianTao
                })
                .ToList()
        };

        return View(model);
    }

    // Bảng nghiệp vụ vẫn sẵn dùng khi cần đối chiếu hoặc cập nhật giao dịch.
    public IActionResult GiaoDich()
    {
        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(nv => nv.MaNV, nv => nv.HoNV + " " + nv.TenNV);
        ViewBag.TenLoaiGiaoDich = new Dictionary<string, string>
        {
            { "PAYMENT", "Thu tiền" }, { "REFUND", "Hoàn tiền" }
        };
        ViewBag.TenPhuongThuc = new Dictionary<string, string>
        {
            { "CASH", "Tiền mặt" }, { "BANK_TRANSFER", "Chuyển khoản" }
        };
        ViewBag.TenTrangThaiThanhToan = new Dictionary<string, string>
        {
            { "PENDING", "Chờ xác nhận" }, { "SUCCESS", "Thành công" }, { "REJECTED", "Đã từ chối" }
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
            ViewBag.LoiXoa = SqlErrorMessage.ForUser(ex, "Không thể xóa giao dịch vì giao dịch có thể đang được tham chiếu.");
            return View("Delete", thanhToan);
        }
    }

    private bool ThanhToanExists(string? mathanhtoan)
    {
        return _context.ThanhToans.Any(e => e.MaThanhToan == mathanhtoan);
    }
}
