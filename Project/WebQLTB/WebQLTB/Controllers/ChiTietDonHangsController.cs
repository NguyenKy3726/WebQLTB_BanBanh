using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("SALES")]
public class ChiTietDonHangsController : Controller
{
    private readonly AppDbContext _context;

    public ChiTietDonHangsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: CHITIETDONHANGS
    public async Task<IActionResult> Index()    
    {
        var tenSanPhams = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);

        ViewBag.TenBienThe = _context.BienTheBanh_DS()
            .ToDictionary(
                bt => bt.MaBienThe,
                bt => $"{tenSanPhams[bt.MaSP]} - {bt.TenBienThe}"
            );
        return View(_context.ChiTietDonHang_DS());
    }

    // GET: CHITIETDONHANGS/Details/5
    public async Task<IActionResult> Details(string? madonhang, short? stt)
    {
        if (madonhang == null || stt == null)
        {
            return NotFound();
        }

        var chitietdonhang = _context.ChiTietDonHang_CT(madonhang, stt.Value);
        if (chitietdonhang == null)
        {
            return NotFound();
        }
        var tenSanPhams = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);

        ViewBag.TenBienThe = _context.BienTheBanh_DS()
            .ToDictionary(
                bt => bt.MaBienThe,
                bt => $"{tenSanPhams[bt.MaSP]} - {bt.TenBienThe}"
            );
        return View(chitietdonhang);
    }

    // GET: CHITIETDONHANGS/Create
    public IActionResult Create()
    {
        ViewBag.MaDonHang = new SelectList(
            _context.DonHang_DS(),
            "MaDonHang",
            "MaDonHang"
        );

        var tenSanPhams = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);

        ViewBag.MaBienThe = new SelectList(
            _context.BienTheBanh_DS().Select(bt => new
            {
                bt.MaBienThe,
                HienThi = $"{bt.MaBienThe} - {tenSanPhams[bt.MaSP]} - {bt.TenBienThe}"
            }),
            "MaBienThe",
            "HienThi"
        );
        return View();
    }

    // POST: CHITIETDONHANGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaDonHang,STT,MaBienThe,SoLuong,DonGiaTaiThoiDiemDat,SoLuongCoSanDaGiu,SoLuongCanLam,SoLuongDaLam,NgayDuKienCoHang,GhiChu")] ChiTietDonHang chitietdonhang)
    {
        if (ModelState.IsValid)
        {
            _context.ChiTietDonHang_Them(chitietdonhang);
            return RedirectToAction(nameof(Index));
        }
        return View(chitietdonhang);
    }

    // GET: CHITIETDONHANGS/Edit/5
    public async Task<IActionResult> Edit(string? madonhang, short? stt)
    {
        if (madonhang == null || stt == null)
        {
            return NotFound();
        }

        var chitietdonhang = _context.ChiTietDonHang_CT(madonhang, stt.Value);
        if (chitietdonhang == null)
        {
            return NotFound();
        }
        ViewBag.MaDonHang = new SelectList(
            _context.DonHang_DS(),
            "MaDonHang",
            "MaDonHang",
            chitietdonhang.MaDonHang
        );

        var tenSanPhams = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);

        ViewBag.MaBienThe = new SelectList(
            _context.BienTheBanh_DS().Select(bt => new
            {
                bt.MaBienThe,
                HienThi = $"{bt.MaBienThe} - {tenSanPhams[bt.MaSP]} - {bt.TenBienThe}"
            }),
            "MaBienThe",
            "HienThi",
            chitietdonhang.MaBienThe
        );
        return View(chitietdonhang);
    }

    // POST: CHITIETDONHANGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? madonhang, short? stt, [Bind("MaDonHang,STT,MaBienThe,SoLuong,DonGiaTaiThoiDiemDat,SoLuongCoSanDaGiu,SoLuongCanLam,SoLuongDaLam,NgayDuKienCoHang,GhiChu")] ChiTietDonHang chitietdonhang)
    {
        if (madonhang != chitietdonhang.MaDonHang || stt != chitietdonhang.STT)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.ChiTietDonHang_Sua(chitietdonhang);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChiTietDonHangExists(chitietdonhang.MaDonHang))
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
        return View(chitietdonhang);
    }

    // GET: CHITIETDONHANGS/Delete/5
    public async Task<IActionResult> Delete(string? madonhang, short? stt)
    {
        if (madonhang == null || stt == null)
        {
            return NotFound();
        }

        var chitietdonhang = _context.ChiTietDonHang_CT(madonhang, stt.Value);
        if (chitietdonhang == null)
        {
            return NotFound();
        }
        var tenSanPhams = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);

        ViewBag.TenBienThe = _context.BienTheBanh_DS()
            .ToDictionary(
                bt => bt.MaBienThe,
                bt => $"{tenSanPhams[bt.MaSP]} - {bt.TenBienThe}"
            );
        return View(chitietdonhang);
    }

    // POST: CHITIETDONHANGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? madonhang, short? stt)
    {
        if (madonhang == null || stt == null) return NotFound();

        var chiTiet = _context.ChiTietDonHang_CT(madonhang, stt.Value);
        if (chiTiet == null) return NotFound();

        try
        {
            _context.ChiTietDonHang_Xoa(madonhang, stt.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", chiTiet);
        }
    }

    private bool ChiTietDonHangExists(string? madonhang)
    {
        return _context.ChiTietDonHangs.Any(e => e.MaDonHang == madonhang);
    }
}
