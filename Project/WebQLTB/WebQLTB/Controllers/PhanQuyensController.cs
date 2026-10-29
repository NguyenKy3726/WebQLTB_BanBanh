using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class PhanQuyensController : Controller
{
    private readonly AppDbContext _context;

    public PhanQuyensController(AppDbContext context)
    {
        _context = context;
    }

    // GET: PHANQUYENS
    public async Task<IActionResult> Index()    
    {
        ViewBag.TenQuyen = _context.Quyen_DS()
            .ToDictionary(q => q.MaQuyen, q => q.TenQuyen);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(n => n.MaNV, n => n.HoNV + " " + n.TenNV);
        return View(_context.PhanQuyen_DS());
    }

    // GET: PHANQUYENS/Details/5
    public async Task<IActionResult> Details(byte? maquyen, byte? manv)
    {
        if (maquyen == null || manv == null)
        {
            return NotFound();
        }

        var phanquyen = _context.PhanQuyen_CT(maquyen.Value, manv.Value);
        if (phanquyen == null)
        {
            return NotFound();
        }

        return View(phanquyen);
    }

    // GET: PHANQUYENS/Create
    public IActionResult Create()
    {
        ViewBag.MaQuyen = new SelectList(_context.Quyen_DS(), "MaQuyen", "TenQuyen");
        ViewBag.MaNV = new SelectList(
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

    // POST: PHANQUYENS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaQuyen,MaNV")] PhanQuyen phanquyen)
    {
        if (ModelState.IsValid)
        {
            _context.PhanQuyen_Them(phanquyen);
            return RedirectToAction(nameof(Index));
        }
        ViewBag.MaQuyen = new SelectList(_context.Quyen_DS(), "MaQuyen", "TenQuyen", phanquyen.MaQuyen);
        ViewBag.MaNV = new SelectList(
            _context.NhanVien_DS().Select(nv => new
            {
                nv.MaNV,
                HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
            }),
            "MaNV",
            "HienThi",
            phanquyen.MaNV
        );
        return View(phanquyen);
    }

    // GET: PHANQUYENS/Edit/5
    public async Task<IActionResult> Edit(byte? maquyen, byte? manv)
    {
        if (maquyen == null || manv == null)
        {
            return NotFound();
        }

        var phanQuyen = _context.PhanQuyen_CT(maquyen.Value, manv.Value);
        if (phanQuyen == null)
        {
            return NotFound();
        }
        ViewBag.MaQuyen = new SelectList(_context.Quyen_DS(), "MaQuyen", "TenQuyen", phanQuyen.MaQuyen);
        ViewBag.MaNV = new SelectList(
            _context.NhanVien_DS().Select(nv => new
            {
                nv.MaNV,
                HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
            }),
            "MaNV",
            "HienThi",
            phanQuyen.MaNV
        );
        return View(phanQuyen);
    }

    // POST: PHANQUYENS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(byte? maquyen, byte? manv, [Bind("MaQuyen,MaNV")] PhanQuyen phanQuyen)
    {
        if (maquyen != phanQuyen.MaQuyen || manv != phanQuyen.MaNV)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.PhanQuyen_Them(phanQuyen);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PhanQuyenExists(phanQuyen.MaQuyen, phanQuyen.MaNV))
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
        ViewBag.MaQuyen = new SelectList(_context.Quyen_DS(), "MaQuyen", "TenQuyen", phanQuyen.MaQuyen);
        ViewBag.MaNV = new SelectList(
            _context.NhanVien_DS().Select(nv => new
            {
                nv.MaNV,
                HienThi = $"{nv.MaNV} - {nv.HoNV} {nv.TenNV}"
            }),
            "MaNV",
            "HienThi",
            phanQuyen.MaNV
        );
        return View(phanQuyen);
    }

    // GET: PHANQUYENS/Delete/5
    public async Task<IActionResult> Delete(byte? maquyen, byte? manv)
    {
        if (maquyen == null || manv == null)
        {
            return NotFound();
        }

        var phanQuyen = _context.PhanQuyen_CT(maquyen.Value, manv.Value);
        if (phanQuyen == null)
        {
            return NotFound();
        }
        ViewBag.TenQuyen = _context.Quyen_DS()
            .ToDictionary(q => q.MaQuyen, q => q.TenQuyen);

        ViewBag.TenNV = _context.NhanVien_DS()
            .ToDictionary(n => n.MaNV, n => n.HoNV + " " + n.TenNV);
        return View(phanQuyen);
    }

    // POST: PHANQUYENS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(byte? maquyen, byte? manv)
    {
        if (maquyen == null || manv == null) return NotFound();

        var phanQuyen = _context.PhanQuyen_CT(maquyen.Value, manv.Value);
        if (phanQuyen == null) return NotFound();

        try
        {
            _context.PhanQuyen_Xoa(maquyen.Value, manv.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", phanQuyen);
        }
    }

    private bool PhanQuyenExists(byte? maquyen, byte? manv)
    {
        return _context.PhanQuyens.Any(e => e.MaQuyen == maquyen && e.MaNV == manv);
    }
}
