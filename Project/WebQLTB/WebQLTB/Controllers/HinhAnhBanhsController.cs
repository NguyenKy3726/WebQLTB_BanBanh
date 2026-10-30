using WebQLTB.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using WebQLTB.Data;
using WebQLTB.Models;
using WebQLTB.ViewModels;

[PermissionAuthorize("ADMIN")]
public class HinhAnhBanhsController : Controller
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public HinhAnhBanhsController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public IActionResult Index(string? masp)
    {
        var products = _context.SanPham_DS();
        ViewBag.TenSP = products.ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        ViewBag.MaSPDangChon = masp;
        ViewBag.ThongBao = TempData["ThongBaoAnh"];
        var images = _context.HinhAnhBanh_DS();
        return View(string.IsNullOrWhiteSpace(masp) ? images : images.Where(anh => anh.MaSP == masp).ToList());
    }

    public IActionResult Details(short? maanh)
    {
        if (maanh == null) return NotFound();
        var image = _context.HinhAnhBanh_CT(maanh.Value);
        if (image == null) return NotFound();
        ViewBag.TenSP = _context.SanPham_DS().ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(image);
    }

    public IActionResult Create(string? masp)
    {
        PrepareProducts(masp);
        return View(new HinhAnhBanhFormViewModel { MaSP = masp ?? string.Empty, ThuTuHienThi = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> Create(HinhAnhBanhFormViewModel form)
    {
        var path = await ResolveImagePath(form);
        if (path == null)
        {
            PrepareProducts(form.MaSP);
            return View(form);
        }

        try
        {
            var image = new HinhAnhBanh
            {
                MaSP = form.MaSP,
                DuongDanAnh = path,
                LaAnhDaiDien = false,
                ThuTuHienThi = form.ThuTuHienThi
            };
            _context.HinhAnhBanh_Them(image);

            // Proc thêm không có tham số trả mã; đường dẫn upload được tạo ngẫu nhiên nên nhận diện an toàn.
            if (form.LaAnhDaiDien)
            {
                var created = _context.HinhAnhBanh_DS()
                    .Where(x => x.MaSP == form.MaSP && x.DuongDanAnh == path)
                    .OrderByDescending(x => x.MaAnh)
                    .FirstOrDefault();
                if (created == null) throw new InvalidOperationException("Không tìm thấy ảnh vừa tạo để đặt làm ảnh đại diện.");
                created.LaAnhDaiDien = true;
                _context.HinhAnhBanh_Sua(created);
            }

            TempData["ThongBaoAnh"] = "Đã thêm ảnh cho mẫu bánh.";
            return RedirectToAction(nameof(Index), new { masp = form.MaSP });
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepareProducts(form.MaSP);
            return View(form);
        }
    }

    public IActionResult Edit(short? maanh)
    {
        if (maanh == null) return NotFound();
        var image = _context.HinhAnhBanh_CT(maanh.Value);
        if (image == null) return NotFound();
        PrepareProducts(image.MaSP);
        return View(new HinhAnhBanhFormViewModel
        {
            MaAnh = image.MaAnh,
            MaSP = image.MaSP,
            DuongDanAnh = image.DuongDanAnh,
            LaAnhDaiDien = image.LaAnhDaiDien,
            ThuTuHienThi = image.ThuTuHienThi
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> Edit(short? maanh, HinhAnhBanhFormViewModel form)
    {
        if (maanh != form.MaAnh) return NotFound();
        var current = _context.HinhAnhBanh_CT(maanh!.Value);
        if (current == null) return NotFound();

        // Không cho đổi sản phẩm của ảnh; form chỉ chọn file mới hoặc chỉnh thứ tự/ảnh đại diện.
        form.MaSP = current.MaSP;
        var path = await ResolveImagePath(form, current.DuongDanAnh);
        if (path == null)
        {
            PrepareProducts(current.MaSP);
            return View(form);
        }

        try
        {
            _context.HinhAnhBanh_Sua(new HinhAnhBanh
            {
                MaAnh = current.MaAnh,
                MaSP = current.MaSP,
                DuongDanAnh = path,
                LaAnhDaiDien = form.LaAnhDaiDien,
                ThuTuHienThi = form.ThuTuHienThi
            });
            TempData["ThongBaoAnh"] = "Đã cập nhật ảnh bánh.";
            return RedirectToAction(nameof(Index), new { masp = current.MaSP });
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PrepareProducts(current.MaSP);
            return View(form);
        }
    }

    public IActionResult Delete(short? maanh)
    {
        if (maanh == null) return NotFound();
        var image = _context.HinhAnhBanh_CT(maanh.Value);
        if (image == null) return NotFound();
        ViewBag.TenSP = _context.SanPham_DS().ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
        return View(image);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(short? maanh)
    {
        if (maanh == null) return NotFound();
        var image = _context.HinhAnhBanh_CT(maanh.Value);
        if (image == null) return NotFound();
        try
        {
            _context.HinhAnhBanh_Xoa(maanh.Value);
            TempData["ThongBaoAnh"] = "Đã xóa ảnh khỏi danh sách ảnh bánh.";
            return RedirectToAction(nameof(Index), new { masp = image.MaSP });
        }
        catch (SqlException ex)
        {
            ViewBag.LoiXoa = ex.Message;
            ViewBag.TenSP = _context.SanPham_DS().ToDictionary(sp => sp.MaSP, sp => sp.TenSP);
            return View("Delete", image);
        }
    }

    private void PrepareProducts(string? selected)
    {
        ViewBag.MaSP = new SelectList(_context.SanPham_DS().Select(sp => new
        {
            sp.MaSP,
            HienThi = $"{sp.MaSP} - {sp.TenSP}"
        }), "MaSP", "HienThi", selected);
    }

    private async Task<string?> ResolveImagePath(HinhAnhBanhFormViewModel form, string? currentPath = null)
    {
        if (!ModelState.IsValid) return null;
        if (form.TepAnh == null)
        {
            var manualPath = string.IsNullOrWhiteSpace(form.DuongDanAnh) ? currentPath : form.DuongDanAnh.Trim();
            if (string.IsNullOrWhiteSpace(manualPath))
                ModelState.AddModelError(nameof(form.TepAnh), "Chọn ảnh từ máy hoặc nhập đường dẫn ảnh.");
            return string.IsNullOrWhiteSpace(manualPath) ? null : manualPath;
        }

        var file = form.TepAnh;
        var extension = Path.GetExtension(file.FileName);
        if (file.Length == 0 || file.Length > MaxUploadBytes)
            ModelState.AddModelError(nameof(form.TepAnh), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
        if (!AllowedExtensions.Contains(extension))
            ModelState.AddModelError(nameof(form.TepAnh), "Chỉ nhận ảnh JPG, PNG hoặc WEBP.");
        if (!ModelState.IsValid) return null;

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        var validSignature = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => read >= 12 && header.Take(4).SequenceEqual("RIFF"u8.ToArray()) && header.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray()),
            _ => false
        };
        if (!validSignature)
        {
            ModelState.AddModelError(nameof(form.TepAnh), "Nội dung file không đúng định dạng ảnh đã chọn.");
            return null;
        }

        var folder = Path.Combine(_environment.WebRootPath, "assets", "images", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destination = Path.Combine(folder, fileName);
        stream.Position = 0;
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await stream.CopyToAsync(output);
        return $"~/assets/images/products/{fileName}";
    }
}
