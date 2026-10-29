using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

public class LichSuTrangThaiDonHang
{
    [Key]
    public long MaLichSu { get; set; }
    public string MaDonHang { get; set; } = string.Empty;
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = string.Empty;
    public string NguoiThucHien { get; set; } = string.Empty;
    public byte? MaNVThucHien { get; set; }
    public DateTime ThoiDiem { get; set; }
    public string? LyDo { get; set; }
}
