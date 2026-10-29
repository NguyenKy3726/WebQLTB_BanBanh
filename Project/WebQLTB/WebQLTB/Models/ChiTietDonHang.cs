using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    [PrimaryKey(nameof(MaDonHang), nameof(STT))]
    public class ChiTietDonHang
    {
        public string MaDonHang { get; set; } = null!;
        public short STT { get; set; }

        public short MaBienThe { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaTaiThoiDiemDat { get; set; }

        public int SoLuongCoSanDaGiu { get; set; }
        public int SoLuongCanLam { get; set; }
        public int SoLuongDaLam { get; set; }

        public DateTime? NgayDuKienCoHang { get; set; }
        public string? GhiChu { get; set; }
    }
}
