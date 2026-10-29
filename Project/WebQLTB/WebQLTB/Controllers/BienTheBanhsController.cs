using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class BienTheBanhsController : Controller
{
    private readonly AppDbContext _context;

    public BienTheBanhsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: BIENTHEBANHS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(_context.BienTheBanh_DS());
    }

    // GET: BIENTHEBANHS/Details/5
    public async Task<IActionResult> Details(short? mabienthe)
    {
        if (mabienthe == null)
        {
            return NotFound();
        }

        var bienthebanh = _context.BienTheBanh_CT(mabienthe.Value);
        if (bienthebanh == null)
        {
            return NotFound();
        }
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(bienthebanh);
    }

    // GET: BIENTHEBANHS/Create
    public IActionResult Create()
    {
        ViewBag.MaSP = new SelectList(
            _context.SanPham_DS().Select(sp => new
            {
                sp.MaSP,
                HienThi = $"{sp.MaSP} - {sp.TenSP}"
            }),
            "MaSP",
            "HienThi"
        );
        return View();
    }

    // POST: BIENTHEBANHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaBienThe,MaSP,TenBienThe,GiaBan,SoLuongThucTe,SoLuongDaGiu,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh bienthebanh)
    {
        if (ModelState.IsValid)
        {
            _context.BienTheBanh_Them(bienthebanh);
            return RedirectToAction(nameof(Index));
        }
        return View(bienthebanh);
    }

    // GET: BIENTHEBANHS/Edit/5
    public async Task<IActionResult> Edit(short? mabienthe)
    {
        if (mabienthe == null)
        {
            return NotFound();
        }

        var bienthebanh = _context.BienTheBanh_CT(mabienthe.Value);
        if (bienthebanh == null)
        {
            return NotFound();
        }
        ViewBag.MaSP = new SelectList(
            _context.SanPham_DS().Select(sp => new
            {
                sp.MaSP,
                HienThi = $"{sp.MaSP} - {sp.TenSP}"
            }),
            "MaSP",
            "HienThi",
            bienthebanh.MaSP
        );
        return View(bienthebanh);
    }

    // POST: BIENTHEBANHS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(short? mabienthe, [Bind("MaBienThe,MaSP,TenBienThe,GiaBan,SoLuongThucTe,SoLuongDaGiu,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh bienthebanh)
    {
        if (mabienthe != bienthebanh.MaBienThe)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.BienTheBanh_Sua(bienthebanh);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BienTheBanhExists(bienthebanh.MaBienThe))
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
        return View(bienthebanh);
    }

    // GET: BIENTHEBANHS/Delete/5
    public async Task<IActionResult> Delete(short? mabienthe)
    {
        if (mabienthe == null)
        {
            return NotFound();
        }

        var bienthebanh = _context.BienTheBanh_CT(mabienthe.Value);
        if (bienthebanh == null)
        {
            return NotFound();
        }
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(bienthebanh);
    }

    // POST: BIENTHEBANHS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(short? mabienthe)
    {
        if (mabienthe == null) return NotFound();

        var bienThe = _context.BienTheBanh_CT(mabienthe.Value);
        if (bienThe == null) return NotFound();

        try
        {
            _context.BienTheBanh_Xoa(mabienthe.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", bienThe);
        }
    }

    private bool BienTheBanhExists(short? mabienthe)
    {
        return _context.BienTheBanhs.Any(e => e.MaBienThe == mabienthe);
    }
}
