using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class ThanhToan
    {
        [Key]
        public string MaThanhToan { get; set; } = null!;

        public string MaDonHang { get; set; } = null!;
        public string? MaThanhToanGoc { get; set; }

        public string LoaiGiaoDich { get; set; } = null!;
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = null!;
        public string TrangThaiThanhToan { get; set; } = "PENDING";

        public DateTime ThoiGianTao { get; set; }
        public DateTime? ThoiGianXacNhan { get; set; }
        public byte? MaNVXacNhan { get; set; }

        public string? DuongDanChungTu { get; set; }
        public string? GhiChu { get; set; }
    }
}
