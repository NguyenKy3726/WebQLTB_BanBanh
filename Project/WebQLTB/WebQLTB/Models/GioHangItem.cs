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
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(80, ErrorMessage = "Tên người nhận tối đa 80 ký tự.")]
    public string TenNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận.")]
    [StringLength(11, MinimumLength = 10, ErrorMessage = "Số điện thoại gồm 10 hoặc 11 chữ số.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại chỉ gồm 10 hoặc 11 chữ số.")]
    public string DienThoaiNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [StringLength(200, ErrorMessage = "Địa chỉ mặc định của khách hàng tối đa 200 ký tự.")]
    public string DiaChiGiaoHang { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? GhiChu { get; set; }
}

public class GioHangCapNhatItem
{
    [Range(1, short.MaxValue)]
    public short MaBienThe { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng phải từ 1 đến 50.")]
    public int SoLuong { get; set; }
}

