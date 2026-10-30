using WebQLTB.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebQLTB.Data;
using WebQLTB.Models;

[PermissionAuthorize("ADMIN")]
public class QuyensController : Controller
{
    private readonly AppDbContext _context;

    public QuyensController(AppDbContext context)
    {
        _context = context;
    }

    // GET: QUYENS
    public async Task<IActionResult> Index()    
    {
        return View(_context.Quyen_DS());
    }

    // GET: QUYENS/Details/5
    public async Task<IActionResult> Details(byte? maquyen)
    {
        if (maquyen == null)
        {
            return NotFound();
        }

        var quyen = _context.Quyen_CT(maquyen.Value);
        if (quyen == null)
        {
            return NotFound();
        }

        return View(quyen);
    }

    // GET: QUYENS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: QUYENS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaQuyen,TenQuyen,MoTa")] Quyen quyen)
    {
        if (ModelState.IsValid)
        {
            _context.Quyen_Them(quyen);
            return RedirectToAction(nameof(Index));
        }
        return View(quyen);
    }

    // GET: QUYENS/Edit/5
    public async Task<IActionResult> Edit(byte? maquyen)
    {
        if (maquyen == null)
        {
            return NotFound();
        }

        var quyen = _context.Quyen_CT(maquyen.Value);
        if (quyen == null)
        {
            return NotFound();
        }
        return View(quyen);
    }

    // POST: QUYENS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(byte? maquyen, [Bind("MaQuyen,TenQuyen,MoTa")] Quyen quyen)
    {
        if (maquyen != quyen.MaQuyen)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Quyen_Sua(quyen);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuyenExists(quyen.MaQuyen))
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
        return View(quyen);
    }

    // GET: QUYENS/Delete/5
    public async Task<IActionResult> Delete(byte? maquyen)
    {
        if (maquyen == null)
        {
            return NotFound();
        }

        var quyen = _context.Quyen_CT(maquyen.Value);
        if (quyen == null)
        {
            return NotFound();
        }

        return View(quyen);
    }

    // POST: QUYENS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(byte? maquyen)
    {
        if (maquyen == null) return NotFound();

        var quyen = _context.Quyen_CT(maquyen.Value);
        if (quyen == null) return NotFound();

        try
        {
            _context.Quyen_Xoa(maquyen.Value);
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = SqlErrorMessage.ForUser(ex, "Không thể xóa nhóm quyền vì có thể đang được gán cho nhân viên.");
            return View("Delete", quyen);
        }
    }

    private bool QuyenExists(byte? maquyen)
    {
        return _context.Quyens.Any(e => e.MaQuyen == maquyen);
    }
}
