USE DbQLTB;
GO

/* ===== Nhóm A: bảng độc lập ===== */

-- 1. Chi tiết nhân viên
CREATE OR ALTER PROC dbo.NhanVien_CT
    @MaNV tinyint
AS
    SELECT MaNV, HoNV, TenNV, DienThoai, Email,
           TenDN, MatKhau, AnhNV
    FROM dbo.NhanVien
    WHERE MaNV = @MaNV;
GO

-- 2. Chi tiết quyền
CREATE OR ALTER PROC dbo.Quyen_CT
    @MaQuyen tinyint
AS
    SELECT MaQuyen, TenQuyen, MoTa
    FROM dbo.Quyen
    WHERE MaQuyen = @MaQuyen;
GO

-- 3. Chi tiết loại bánh
CREATE OR ALTER PROC dbo.LoaiBanh_CT
    @MaLoaiBanh tinyint
AS
    SELECT MaLoaiBanh, TenLoaiBanh, MoTa
    FROM dbo.LoaiBanh
    WHERE MaLoaiBanh = @MaLoaiBanh;
GO

-- 4. Chi tiết khách hàng
CREATE OR ALTER PROC dbo.KhachHang_CT
    @MaKH smallint
AS
    SELECT MaKH, HoKH, TenKH, DienThoai, Email,
           DiaChiThuongDung, TenDN, MatKhau
    FROM dbo.KhachHang
    WHERE MaKH = @MaKH;
GO

/* ===== Nhóm B ===== */

-- 5. Chi tiết phân quyền: khóa chính ghép
CREATE OR ALTER PROC dbo.PhanQuyen_CT
    @MaQuyen tinyint,
    @MaNV    tinyint
AS
    SELECT MaQuyen, MaNV
    FROM dbo.PhanQuyen
    WHERE MaQuyen = @MaQuyen
      AND MaNV    = @MaNV;
GO

-- 6. Chi tiết sản phẩm
CREATE OR ALTER PROC dbo.SanPham_CT
    @MaSP char(9)
AS
    SELECT MaSP, MaLoaiBanh, TenSP, MoTaNgan, MoTaChiTiet,
           TrangThaiHienThi, MaNVTao, NgayTao, MaNVCapNhat, NgayCapNhat
    FROM dbo.SanPham
    WHERE MaSP = @MaSP;
GO

-- 7. Chi tiết hình ảnh bánh
CREATE OR ALTER PROC dbo.HinhAnhBanh_CT
    @MaAnh smallint
AS
    SELECT MaAnh, MaSP, DuongDanAnh, LaAnhDaiDien, ThuTuHienThi
    FROM dbo.HinhAnhBanh
    WHERE MaAnh = @MaAnh;
GO

-- 8. Chi tiết biến thể bánh
CREATE OR ALTER PROC dbo.BienTheBanh_CT
    @MaBienThe smallint
AS
    SELECT MaBienThe, MaSP, TenBienThe, GiaBan, SoLuongThucTe,
           SoLuongDaGiu, SoNgayChuanBiDuKien, TrangThaiBan
    FROM dbo.BienTheBanh
    WHERE MaBienThe = @MaBienThe;
GO

-- 9. Chi tiết đơn hàng
CREATE OR ALTER PROC dbo.DonHang_CT
    @MaDonHang char(11)
AS
    SELECT MaDonHang, MaKH, MaNVXuLy, MaNVXacNhan, MaNVHoanTat,
           NgayDat, NgayXacNhan, NgayHoanTat, NgayGiaoDuKien,
           TrangThaiDonHang, TenNguoiNhan, DienThoaiNguoiNhan,
           DiaChiGiaoHang, PhiGiaoHang, TienGiamGia, TienCocYeuCau,
           TongTienHang, TongTienPhaiTra, GhiChu
    FROM dbo.DonHang
    WHERE MaDonHang = @MaDonHang;
GO

-- 10. Chi tiết một dòng đơn hàng
CREATE OR ALTER PROC dbo.ChiTietDonHang_CT
    @MaDonHang char(11),
    @STT       smallint
AS
    SELECT MaDonHang, STT, MaBienThe, SoLuong, DonGiaTaiThoiDiemDat,
           SoLuongCoSanDaGiu, SoLuongCanLam, SoLuongDaLam,
           NgayDuKienCoHang, GhiChu
    FROM dbo.ChiTietDonHang
    WHERE MaDonHang = @MaDonHang
      AND STT       = @STT;
GO

-- 11. Chi tiết thanh toán / hoàn tiền
CREATE OR ALTER PROC dbo.ThanhToan_CT
    @MaThanhToan char(11)
AS
    SELECT MaThanhToan, MaDonHang, MaThanhToanGoc, LoaiGiaoDich, SoTien,
           PhuongThuc, TrangThaiThanhToan, ThoiGianTao, ThoiGianXacNhan,
           MaNVXacNhan, DuongDanChungTu, GhiChu
    FROM dbo.ThanhToan
    WHERE MaThanhToan = @MaThanhToan;
GO

-- Nhật ký xử lý trạng thái của một đơn hàng.
CREATE OR ALTER PROC dbo.DonHang_LichSu
    @MaDonHang char(11)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaLichSu,MaDonHang,TrangThaiCu,TrangThaiMoi,NguoiThucHien,
           MaNVThucHien,ThoiDiem,LyDo
    FROM dbo.LichSuTrangThaiDonHang
    WHERE MaDonHang=@MaDonHang
    ORDER BY MaLichSu;
END;
GO
