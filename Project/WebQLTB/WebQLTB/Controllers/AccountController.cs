using Microsoft.AspNetCore.Mvc;
using WebQLTB.Data;
using WebQLTB.Models;

namespace WebQLTB.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string TenDN,
            string MatKhau,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(TenDN)
                || string.IsNullOrWhiteSpace(MatKhau))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Vui lòng nhập tên đăng nhập và mật khẩu.");

                return View();
            }

            // Kiểm tra tài khoản nhân viên
            var maNV = _context.NhanVien_DangNhap(TenDN, MatKhau);

            if (maNV != 0)
            {
                var nv = _context.NhanVien_CT(maNV);

                if (nv == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không tìm thấy thông tin nhân viên.");

                    return View();
                }

                HttpContext.Session.Clear();
                HttpContext.Session.SetString("Username", nv.TenDN);
                HttpContext.Session.SetString(
                    "DisplayName", nv.HoNV + " " + nv.TenNV);
                HttpContext.Session.SetString("Role", "NhanVien");
                HttpContext.Session.SetInt32("MaNV", nv.MaNV);
                foreach (var q in _context.NhanVien_QuyenDanhSach(nv.MaNV))
                    HttpContext.Session.SetString("Permission:" + q, "1");

                return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
            }

            // Kiểm tra tài khoản khách hàng
            var maKH = _context.KhachHang_DangNhap(TenDN, MatKhau);

            if (maKH != 0)
            {
                var kh = _context.KhachHang_CT(maKH);

                if (kh == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không tìm thấy thông tin khách hàng.");

                    return View();
                }

                HttpContext.Session.Clear();
                HttpContext.Session.SetString("Username", kh.TenDN);
                HttpContext.Session.SetString(
                    "DisplayName", kh.HoKH + " " + kh.TenKH);
                HttpContext.Session.SetString("Role", "KhachHang");
                HttpContext.Session.SetInt32("MaKH", kh.MaKH);

                return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(
                string.Empty,
                "Sai tên đăng nhập hoặc mật khẩu.");

            return View();
        }


        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string HoKH, string TenKH, string TenDN, string MatKhau, string? DienThoai, string? Email, string? DiaChiThuongDung, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrWhiteSpace(HoKH) || string.IsNullOrWhiteSpace(TenKH) || string.IsNullOrWhiteSpace(TenDN) || string.IsNullOrWhiteSpace(MatKhau))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập họ tên, tên đăng nhập và mật khẩu.");
                return View();
            }
            if (MatKhau.Length > 50 || TenDN.Length > 50)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu vượt quá độ dài cho phép.");
                return View();
            }
            try
            {
                _context.KhachHang_Them(new KhachHang { HoKH=HoKH.Trim(), TenKH=TenKH.Trim(), TenDN=TenDN.Trim(), DienThoai=DienThoai?.Trim(), Email=Email?.Trim(), DiaChiThuongDung=DiaChiThuongDung?.Trim() }, MatKhau);
                TempData["ThongBao"] = "Tạo tài khoản thành công. Hãy đăng nhập để tiếp tục.";
                var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
                return RedirectToAction(nameof(Login), new { returnUrl = safeReturnUrl });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult ThongTin()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "KhachHang")
            {
                var maKH = HttpContext.Session.GetInt32("MaKH");
                if (maKH is null || maKH < short.MinValue || maKH > short.MaxValue)
                    return RedirectToAction(nameof(Login), new { returnUrl = "/Account/ThongTin" });

                var kh = _context.KhachHang_CT((short)maKH.Value);
                if (kh == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction(nameof(Login), new { returnUrl = "/Account/ThongTin" });
                }

                return View(new TaiKhoanViewModel
                {
                    TenHienThi = (kh.HoKH + " " + kh.TenKH).Trim(),
                    TenDangNhap = kh.TenDN,
                    VaiTro = "Khách hàng",
                    DienThoai = kh.DienThoai,
                    Email = kh.Email,
                    DiaChi = kh.DiaChiThuongDung
                });
            }

            if (role == "NhanVien")
            {
                var maNV = HttpContext.Session.GetInt32("MaNV");
                if (maNV is null || maNV < 1 || maNV > byte.MaxValue)
                    return RedirectToAction(nameof(Login), new { returnUrl = "/Account/ThongTin" });

                var nv = _context.NhanVien_CT((byte)maNV.Value);
                if (nv == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction(nameof(Login), new { returnUrl = "/Account/ThongTin" });
                }

                return View(new TaiKhoanViewModel
                {
                    TenHienThi = (nv.HoNV + " " + nv.TenNV).Trim(),
                    TenDangNhap = nv.TenDN,
                    VaiTro = "Nhân viên",
                    DienThoai = nv.DienThoai,
                    Email = nv.Email
                });
            }

            return RedirectToAction(nameof(Login), new { returnUrl = "/Account/ThongTin" });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
