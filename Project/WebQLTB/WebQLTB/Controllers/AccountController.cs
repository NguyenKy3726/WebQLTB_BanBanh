using Microsoft.AspNetCore.Mvc;
using WebQLTB.Data;

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
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string HoKH, string TenKH, string TenDN, string MatKhau, string? DienThoai, string? Email, string? DiaChiThuongDung)
        {
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
                _context.KhachHang_Them(new WebQLTB.Models.KhachHang { HoKH=HoKH.Trim(), TenKH=TenKH.Trim(), TenDN=TenDN.Trim(), DienThoai=DienThoai, Email=Email, DiaChiThuongDung=DiaChiThuongDung }, MatKhau);
                return RedirectToAction(nameof(Login));
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View();
            }
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
