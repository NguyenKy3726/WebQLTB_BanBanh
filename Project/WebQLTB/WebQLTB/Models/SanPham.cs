using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class SanPham
    {
        [Key]
        public string MaSP { get; set; } = null!;

        public byte MaLoaiBanh { get; set; }
        public string TenSP { get; set; } = null!;
        public string? MoTaNgan { get; set; }
        public string? MoTaChiTiet { get; set; }
        public string TrangThaiHienThi { get; set; } = "DRAFT";
        public byte MaNVTao { get; set; }
        public DateTime NgayTao { get; set; }
        public byte? MaNVCapNhat { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}
