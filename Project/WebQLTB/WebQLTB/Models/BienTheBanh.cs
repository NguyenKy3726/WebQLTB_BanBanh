using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class BienTheBanh
    {
        [Key]
        public short MaBienThe { get; set; }

        public string MaSP { get; set; } = null!;
        public string TenBienThe { get; set; } = null!;
        public decimal GiaBan { get; set; }
        public int SoLuongThucTe { get; set; }
        public int SoLuongDaGiu { get; set; }
        public short SoNgayChuanBiDuKien { get; set; }
        public bool TrangThaiBan { get; set; } = true;
    }
}
