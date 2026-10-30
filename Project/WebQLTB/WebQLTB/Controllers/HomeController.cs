using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebQLTB.Data;
using WebQLTB.Models;

namespace WebQLTB.Controllers
{
    //[SessionAuthorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        public IActionResult Index()
        {
            var products = _db.SanPham_DS().Where(x => x.TrangThaiHienThi == "PUBLISHED")
                .OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.MaSP).ToList();
            var categories = _db.LoaiBanh_DS().ToDictionary(x => x.MaLoaiBanh, x => x.TenLoaiBanh);
            var images = _db.HinhAnhBanh_DS().ToLookup(x => x.MaSP);
            var variants = _db.BienTheBanh_DS().Where(x => x.TrangThaiBan).ToLookup(x => x.MaSP);
            var cards = products.Where(x => variants[x.MaSP].Any()).Select(product =>
            {
                var image = images[product.MaSP].OrderByDescending(x => x.LaAnhDaiDien)
                    .ThenBy(x => x.ThuTuHienThi).ThenBy(x => x.MaAnh).FirstOrDefault();
                var sellingVariants = variants[product.MaSP].ToList();
                return new ProductCardViewModel
                {
                    MaSP = product.MaSP, TenSP = product.TenSP, MoTaNgan = product.MoTaNgan,
                    TenLoaiBanh = categories.GetValueOrDefault(product.MaLoaiBanh, "Bánh Whiskful"),
                    DuongDanAnh = image?.DuongDanAnh,
                    GiaThapNhat = sellingVariants.Min(x => x.GiaBan),
                    SoBienTheDangBan = sellingVariants.Count
                };
            }).ToList();
            return View(new HomeViewModel
            {
                GoiY = cards.Take(5).ToList(),
                DanhMuc = cards.GroupBy(x => x.TenLoaiBanh).Select(group => new HomeCategoryViewModel
                {
                    Ten = group.Key, SoMau = group.Count()
                }).OrderBy(x => x.Ten).ToList()
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
