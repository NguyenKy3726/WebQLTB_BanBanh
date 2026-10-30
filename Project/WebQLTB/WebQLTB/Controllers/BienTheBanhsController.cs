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
    public IActionResult Index(string? masp)
    {
        var products = _context.SanPham_DS();
        ViewBag.TenSP = products.ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        ViewBag.MaSPDangChon = masp;
        ViewBag.ThongBao = TempData["ThongBaoBienThe"];
        var variants = _context.BienTheBanh_DS();
        return View(string.IsNullOrWhiteSpace(masp) ? variants : variants.Where(bt => bt.MaSP == masp).ToList());
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
    public IActionResult Create(string? masp)
    {
        PrepareProducts(masp);
        return View();
    }

    // POST: BIENTHEBANHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("MaSP,TenBienThe,GiaBan,SoLuongThucTe,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh bienthebanh)
    {
        bienthebanh.SoLuongDaGiu = 0;
        if (ModelState.IsValid)
        {
            try
            {
                _context.BienTheBanh_Them(bienthebanh);
                TempData["ThongBaoBienThe"] = "Đã thêm phiên bản bánh.";
                return RedirectToAction(nameof(Index), new { masp = bienthebanh.MaSP });
            }
            catch (SqlException ex) { ModelState.AddModelError(string.Empty, SqlErrorMessage.ForUser(ex, "Không thể lưu phiên bản bánh lúc này. Vui lòng kiểm tra dữ liệu và thử lại.")); }
        }
        PrepareProducts(bienthebanh.MaSP);
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
        PrepareProducts(bienthebanh.MaSP);
        return View(bienthebanh);
    }

    // POST: BIENTHEBANHS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(short? mabienthe, [Bind("MaBienThe,MaSP,TenBienThe,GiaBan,SoLuongThucTe,SoNgayChuanBiDuKien,TrangThaiBan")] BienTheBanh bienthebanh)
    {
        if (mabienthe != bienthebanh.MaBienThe)
        {
            return NotFound();
        }

        var current = _context.BienTheBanh_CT(mabienthe.Value);
        if (current == null) return NotFound();
        bienthebanh.MaSP = current.MaSP;
        bienthebanh.SoLuongDaGiu = current.SoLuongDaGiu;
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
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, SqlErrorMessage.ForUser(ex, "Không thể cập nhật phiên bản bánh lúc này. Vui lòng thử lại."));
            }
            if (!ModelState.IsValid)
            {
                PrepareProducts(bienthebanh.MaSP);
                return View(bienthebanh);
            }
            return RedirectToAction(nameof(Index), new { masp = bienthebanh.MaSP });
        }
        PrepareProducts(bienthebanh.MaSP);
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
            return RedirectToAction(nameof(Index), new { masp = bienThe.MaSP });
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = SqlErrorMessage.ForUser(ex, "Không thể xóa phiên bản vì có thể đang được dùng trong đơn hàng.");
            return View("Delete", bienThe);
        }
    }

    private bool BienTheBanhExists(short? mabienthe)
    {
        return _context.BienTheBanhs.Any(e => e.MaBienThe == mabienthe);
    }

    private void PrepareProducts(string? selected)
    {
        ViewBag.MaSP = new SelectList(_context.SanPham_DS().Select(sp => new
        {
            sp.MaSP,
            HienThi = $"{sp.MaSP} - {sp.TenSP}"
        }), "MaSP", "HienThi", selected);
    }
}
