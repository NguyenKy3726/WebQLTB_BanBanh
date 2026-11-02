using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

// Các giá trị khách đã xem; SQL đối chiếu lại trong cùng transaction trước khi đồng ý.
public class DonHangKhachXacNhanViewModel
{
    [Required, StringLength(11)]
    public string MaDonHang { get; set; } = string.Empty;
    [Required] public decimal? TongTienPhaiTra { get; set; }
    [Required] public decimal? PhiGiaoHang { get; set; }
    [Required] public decimal? TienGiamGia { get; set; }
    [Required] public decimal? TienCocYeuCau { get; set; }
    [Required] public DateTime? NgayGiaoDuKien { get; set; }
}
