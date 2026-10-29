using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class LoaiBanhsController : Controller
{
    private readonly AppDbContext _context;

    public LoaiBanhsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: LOAIBANHS
    public async Task<IActionResult> Index()    
    {
        return View(_context.LoaiBanh_DS());
    }

    // GET: LOAIBANHS/Details/5
    public async Task<IActionResult> Details(byte? maloaibanh)
    {
        if (maloaibanh == null)
        {
            return NotFound();
        }

        var loaibanh = _context.LoaiBanh_CT(maloaibanh.Value);
        if (loaibanh == null)
        {
            return NotFound();
        }

        return View(loaibanh);
    }

    // GET: LOAIBANHS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LOAIBANHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaLoaiBanh,TenLoaiBanh,MoTa")] LoaiBanh loaibanh)
    {
        if (ModelState.IsValid)
        {
            _context.LoaiBanh_Them(loaibanh);
            return RedirectToAction(nameof(Index));
        }
        return View(loaibanh);
    }

    // GET: LOAIBANHS/Edit/5
    public async Task<IActionResult> Edit(byte? maloaibanh)
    {
        if (maloaibanh == null)
        {
            return NotFound();
        }

        var loaibanh = _context.LoaiBanh_CT(maloaibanh.Value);
        if (loaibanh == null)
        {
            return NotFound();
        }
        return View(loaibanh);
    }

    // POST: LOAIBANHS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(byte? maloaibanh, [Bind("MaLoaiBanh,TenLoaiBanh,MoTa")] LoaiBanh loaibanh)
    {
        if (maloaibanh != loaibanh.MaLoaiBanh)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.LoaiBanh_Sua(loaibanh);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoaiBanhExists(loaibanh.MaLoaiBanh))
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
        return View(loaibanh);
    }

    // GET: LOAIBANHS/Delete/5
    public async Task<IActionResult> Delete(byte? maloaibanh)
    {
        if (maloaibanh == null)
        {
            return NotFound();
        }

        var loaibanh = _context.LoaiBanh_CT(maloaibanh.Value);
        if (loaibanh == null)
        {
            return NotFound();
        }

        return View(loaibanh);
    }

    // POST: LOAIBANHS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(byte? maloaibanh)
    {
        if (maloaibanh == null) return NotFound();

        var loaiBanh = _context.LoaiBanh_CT(maloaibanh.Value);
        if (loaiBanh == null) return NotFound();

        try
        {
            _context.LoaiBanh_Xoa(maloaibanh.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            return View("Delete", loaiBanh);
        }
    }

    private bool LoaiBanhExists(byte? maloaibanh)
    {
        return _context.LoaiBanhs.Any(e => e.MaLoaiBanh == maloaibanh);
    }
}
