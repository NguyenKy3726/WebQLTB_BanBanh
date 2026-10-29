namespace WebQLTB.Models
{
    public class ProductCardViewModel
    {
        public string MaSP { get; set; } = "";

        public string TenSP { get; set; } = "";

        public string? MoTaNgan { get; set; }

        public string TenLoaiBanh { get; set; } = "";

        public string? DuongDanAnh { get; set; }

        public decimal? GiaThapNhat { get; set; }

        public int SoBienTheDangBan { get; set; }
    }
}