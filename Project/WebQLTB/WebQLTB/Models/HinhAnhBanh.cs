using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class HinhAnhBanh
    {
        [Key]
        public short MaAnh { get; set; }

        public string MaSP { get; set; } = null!;
        public string DuongDanAnh { get; set; } = null!;
        public bool LaAnhDaiDien { get; set; }
        public short ThuTuHienThi { get; set; } = 1;
    }
}
