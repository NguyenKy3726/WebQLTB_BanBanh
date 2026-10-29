namespace WebQLTB.Models;

public class GioHangItem
{
    public short MaBienThe { get; set; }
    public string TenSP { get; set; } = "";
    public string TenBienThe { get; set; } = "";
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
}

public class DatHangViewModel
{
    public string TenNguoiNhan { get; set; } = "";
    public string DienThoaiNguoiNhan { get; set; } = "";
    public string DiaChiGiaoHang { get; set; } = "";
    public string? GhiChu { get; set; }
}
