using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class KhachHang
    {
        [Key]
        public short MaKH { get; set; }

        public string HoKH { get; set; } = null!;
        public string TenKH { get; set; } = null!;
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChiThuongDung { get; set; }
        public string TenDN { get; set; } = null!;
        public byte[] MatKhau { get; set; } = null!;
    }
}
