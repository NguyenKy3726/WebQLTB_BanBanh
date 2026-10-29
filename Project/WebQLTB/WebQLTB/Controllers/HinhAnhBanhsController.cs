using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class HinhAnhBanhsController : Controller
{
    private readonly AppDbContext _context;

    public HinhAnhBanhsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: HINHANHBANHS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(_context.HinhAnhBanh_DS());
    }

    // GET: HINHANHBANHS/Details/5
    public async Task<IActionResult> Details(short? maanh)
    {
        if (maanh == null)
        {
            return NotFound();
        }

        var hinhanhbanh = _context.HinhAnhBanh_CT(maanh.Value);
        if (hinhanhbanh == null)
        {
            return NotFound();
        }
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(hinhanhbanh);
    }

    // GET: HINHANHBANHS/Create
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

    // POST: HINHANHBANHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaAnh,MaSP,DuongDanAnh,LaAnhDaiDien,ThuTuHienThi")] HinhAnhBanh hinhanhbanh)
    {
        if (ModelState.IsValid)
        {
            _context.HinhAnhBanh_Them(hinhanhbanh); 
            return RedirectToAction(nameof(Index));
        }
        return View(hinhanhbanh);
    }

    // GET: HINHANHBANHS/Edit/5
    public async Task<IActionResult> Edit(short? maanh)
    {
        if (maanh == null)
        {
            return NotFound();
        }

        var hinhanhbanh = _context.HinhAnhBanh_CT(maanh.Value);
        if (hinhanhbanh == null)
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
            hinhanhbanh.MaSP
        );
        return View(hinhanhbanh);
    }

    // POST: HINHANHBANHS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(short? maanh, [Bind("MaAnh,MaSP,DuongDanAnh,LaAnhDaiDien,ThuTuHienThi")] HinhAnhBanh hinhanhbanh)
    {
        if (maanh != hinhanhbanh.MaAnh)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.HinhAnhBanh_Sua(hinhanhbanh);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HinhAnhBanhExists(hinhanhbanh.MaAnh))
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
        return View(hinhanhbanh);
    }

    // GET: HINHANHBANHS/Delete/5
    public async Task<IActionResult> Delete(short? maanh)
    {
        if (maanh == null)
        {
            return NotFound();
        }

        var hinhanhbanh = _context.HinhAnhBanh_CT(maanh.Value);
        if (hinhanhbanh == null)
        {
            return NotFound();
        }
        ViewBag.TenSP = _context.SanPham_DS()
            .ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(hinhanhbanh);
    }

    // POST: HINHANHBANHS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(short? maanh)
    {
        if (maanh == null) return NotFound();

        var hinhAnh = _context.HinhAnhBanh_CT(maanh.Value);
        if (hinhAnh == null) return NotFound();

        try
        {
            _context.HinhAnhBanh_Xoa(maanh.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", hinhAnh);
        }
    }

    private bool HinhAnhBanhExists(short? maanh)
    {
        return _context.HinhAnhBanhs.Any(e => e.MaAnh == maanh);
    }
}
