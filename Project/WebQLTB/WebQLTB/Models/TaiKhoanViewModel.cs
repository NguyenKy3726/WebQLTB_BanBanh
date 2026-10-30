using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebQLTB.Models;

public class TaiKhoanViewModel
{
    [BindNever] public string TenHienThi { get; set; } = "";
    [BindNever] public string TenDangNhap { get; set; } = "";
    [BindNever] public string VaiTro { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập họ."), StringLength(30), Display(Name = "Họ và tên đệm")]
    public string Ho { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập tên."), StringLength(20), Display(Name = "Tên")]
    public string Ten { get; set; } = "";
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Điện thoại cần có 10–11 chữ số."), Display(Name = "Điện thoại")]
    public string? DienThoai { get; set; }
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), StringLength(40), Display(Name = "Email")]
    public string? Email { get; set; }
    [StringLength(200), Display(Name = "Địa chỉ mặc định")]
    public string? DiaChi { get; set; }
}
