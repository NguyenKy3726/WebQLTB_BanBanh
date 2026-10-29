using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class NhanVien
    {
        [Key]
        public byte MaNV { get; set; }

        public string HoNV { get; set; } = null!;
        public string TenNV { get; set; } = null!;
        public string DienThoai { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string TenDN { get; set; } = null!;
        public byte[] MatKhau { get; set; } = null!;
        public string? AnhNV { get; set; }

    }
}
