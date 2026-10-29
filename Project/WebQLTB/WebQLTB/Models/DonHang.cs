using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class DonHang
    {
        [Key]
        public string MaDonHang { get; set; } = null!;

        public short? MaKH { get; set; }
        public byte? MaNVXuLy { get; set; }
        public byte? MaNVXacNhan { get; set; }
        public byte? MaNVHoanTat { get; set; }

        public DateTime NgayDat { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public DateTime? NgayHoanTat { get; set; }
        public DateTime? NgayGiaoDuKien { get; set; }

        public string TrangThaiDonHang { get; set; }
            = "CHO_NHAN_VIEN_XU_LY";

        public string TenNguoiNhan { get; set; } = null!;
        public string DienThoaiNguoiNhan { get; set; } = null!;
        public string DiaChiGiaoHang { get; set; } = null!;

        public decimal PhiGiaoHang { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal? TienCocYeuCau { get; set; }
        public decimal TongTienHang { get; set; }
        public decimal TongTienPhaiTra { get; set; }

        public string? GhiChu { get; set; }
    }
}
