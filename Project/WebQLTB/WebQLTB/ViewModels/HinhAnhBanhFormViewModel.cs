using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebQLTB.ViewModels;

public class HinhAnhBanhFormViewModel
{
    public short? MaAnh { get; set; }

    [Required(ErrorMessage = "Hãy chọn mẫu bánh.")]
    public string MaSP { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DuongDanAnh { get; set; }

    public bool LaAnhDaiDien { get; set; }

    [Range(1, short.MaxValue, ErrorMessage = "Thứ tự ảnh phải từ 1 trở lên.")]
    public short ThuTuHienThi { get; set; } = 1;

    public IFormFile? TepAnh { get; set; }
}
