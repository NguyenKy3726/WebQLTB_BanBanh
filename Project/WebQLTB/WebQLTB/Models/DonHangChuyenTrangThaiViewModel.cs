using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

public class DonHangChuyenTrangThaiViewModel
{
    [Required]
    public string MaDonHang { get; set; } = string.Empty;

    [Required]
    public string TrangThaiHienTai { get; set; } = string.Empty;

    [Required]
    public string HanhDong { get; set; } = string.Empty;

    public DateTime? NgayGiaoDuKien { get; set; }
    public decimal? PhiGiaoHang { get; set; }
    public decimal? TienGiamGia { get; set; }
    public decimal? TienCocYeuCau { get; set; }

    [StringLength(500)]
    public string? LyDoHuy { get; set; }
}
