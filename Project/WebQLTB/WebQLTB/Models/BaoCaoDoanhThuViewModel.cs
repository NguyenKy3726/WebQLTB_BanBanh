namespace WebQLTB.Models;

public class BaoCaoDoanhThuViewModel
{
    public int Thang { get; init; }
    public int Nam { get; init; }
    public IReadOnlyList<int> CacNam { get; init; } = Array.Empty<int>();
    public decimal TongTienThu { get; init; }
    public decimal TongTienHoan { get; init; }
    public decimal DoanhThuThuan { get; init; }
    public int SoGiaoDichThu { get; init; }
    public int SoGiaoDichHoan { get; init; }
    public int SoDonHangCoThanhToan { get; init; }
    public IReadOnlyList<DoanhThuTheoNgayViewModel> TheoNgay { get; init; }
        = Array.Empty<DoanhThuTheoNgayViewModel>();
    public IReadOnlyList<BaoCaoGiaoDichViewModel> GiaoDich { get; init; }
        = Array.Empty<BaoCaoGiaoDichViewModel>();
}

public class DoanhThuTheoNgayViewModel
{
    public DateTime Ngay { get; init; }
    public decimal TienThu { get; init; }
    public decimal TienHoan { get; init; }
    public decimal DoanhThuThuan { get; init; }
    public int ChieuCaoCot { get; set; }
}

public class BaoCaoGiaoDichViewModel
{
    public string MaThanhToan { get; init; } = string.Empty;
    public string MaDonHang { get; init; } = string.Empty;
    public string LoaiGiaoDich { get; init; } = string.Empty;
    public decimal SoTien { get; init; }
    public string PhuongThuc { get; init; } = string.Empty;
    public string TrangThai { get; init; } = string.Empty;
    public DateTime ThoiGian { get; init; }
}
