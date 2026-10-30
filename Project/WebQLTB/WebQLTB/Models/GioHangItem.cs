using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

public class GioHangItem
{
    public short MaBienThe { get; set; }
    public string MaSP { get; set; } = "";
    public string? DuongDanAnh { get; set; }
    public bool DangBan { get; set; } = true;
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

public class GioHangCapNhatItem
{
    [Range(1, short.MaxValue)]
    public short MaBienThe { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng phải từ 1 đến 50.")]
    public int SoLuong { get; set; }
}

