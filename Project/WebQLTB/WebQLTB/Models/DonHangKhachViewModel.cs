namespace WebQLTB.Models;

public class DonHangKhachDanhSachViewModel
{
    public IReadOnlyList<DonHangKhachDanhSachItemViewModel> DonHangs { get; init; }
        = Array.Empty<DonHangKhachDanhSachItemViewModel>();

    public IReadOnlyDictionary<string, string> CacTrangThai { get; init; }
        = new Dictionary<string, string>();

    public string? TrangThaiDangChon { get; init; }
}

public class DonHangKhachDanhSachItemViewModel
{
    public string MaDonHang { get; init; } = string.Empty;
    public DateTime NgayDat { get; init; }
    public DateTime? NgayGiaoDuKien { get; init; }
    public string TrangThaiDonHang { get; init; } = string.Empty;
    public string TenTrangThai { get; init; } = string.Empty;
    public decimal? TienCocYeuCau { get; init; }
    public decimal TongTienPhaiTra { get; init; }
}

public class DonHangKhachChiTietViewModel
{
    public DonHang DonHang { get; init; } = null!;
    public string TenTrangThai { get; init; } = string.Empty;
    public IReadOnlyList<DonHangKhachMonHangViewModel> CacMon { get; init; }
        = Array.Empty<DonHangKhachMonHangViewModel>();
    public IReadOnlyList<LichSuTrangThaiDonHang> LichSu { get; init; }
        = Array.Empty<LichSuTrangThaiDonHang>();
    public IReadOnlyList<ThanhToan> GiaoDich { get; init; }
        = Array.Empty<ThanhToan>();
    public IReadOnlyDictionary<string, string> CacTrangThai { get; init; }
        = new Dictionary<string, string>();

    public decimal DaThanhToan { get; init; }
    public decimal ConPhaiTra { get; init; }
    public decimal DaThuCoc { get; init; }
    public decimal ConThieuCoc { get; init; }
}

public class DonHangKhachMonHangViewModel
{
    public string TenBanh { get; init; } = string.Empty;
    public string TenBienThe { get; init; } = string.Empty;
    public int SoLuong { get; init; }
    public decimal DonGia { get; init; }
    public decimal ThanhTien { get; init; }
    public string? GhiChu { get; init; }
}
