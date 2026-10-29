USE DbQLTB;
GO

/*
    18 procedure danh sách đầy đủ cột cho các bảng của DbQLTB.
    MatKhau của NhanVien và KhachHang trả varbinary rỗng để không lộ hash mật khẩu và tránh lỗi ánh xạ giá trị NULL.
    Không nên hiển thị trường MatKhau trên giao diện.
    Nếu ánh xạ EF Core vào entity, nên dùng DTO/ViewModel cho trang Index.
    Script chỉ tạo/cập nhật procedure; không thêm, sửa hoặc xóa dữ liệu.
*/

-- Nhóm A: bảng độc lập
CREATE OR ALTER PROC dbo.NhanVien_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaNV, HoNV, TenNV, DienThoai, Email, TenDN,
           CONVERT(varbinary(MAX), '') AS MatKhau, AnhNV
    FROM dbo.NhanVien
    ORDER BY MaNV;
END;
GO

CREATE OR ALTER PROC dbo.Quyen_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaQuyen, TenQuyen, MoTa
    FROM dbo.Quyen
    ORDER BY MaQuyen;
END;
GO

CREATE OR ALTER PROC dbo.LoaiBanh_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaLoaiBanh, TenLoaiBanh, MoTa
    FROM dbo.LoaiBanh
    ORDER BY MaLoaiBanh;
END;
GO

CREATE OR ALTER PROC dbo.KhachHang_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaKH, HoKH, TenKH, DienThoai, Email, DiaChiThuongDung, TenDN,
           CONVERT(varbinary(MAX), '') AS MatKhau
    FROM dbo.KhachHang
    ORDER BY MaKH;
END;
GO

-- Nhóm B: danh mục và sản phẩm
CREATE OR ALTER PROC dbo.PhanQuyen_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaQuyen, MaNV
    FROM dbo.PhanQuyen
    ORDER BY MaQuyen, MaNV;
END;
GO

CREATE OR ALTER PROC dbo.SanPham_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaSP, MaLoaiBanh, TenSP, MoTaNgan, MoTaChiTiet,
           TrangThaiHienThi, MaNVTao, NgayTao, MaNVCapNhat, NgayCapNhat
    FROM dbo.SanPham
    ORDER BY MaSP;
END;
GO

CREATE OR ALTER PROC dbo.HinhAnhBanh_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaAnh, MaSP, DuongDanAnh, LaAnhDaiDien, ThuTuHienThi
    FROM dbo.HinhAnhBanh
    ORDER BY MaSP, ThuTuHienThi, MaAnh;
END;
GO

CREATE OR ALTER PROC dbo.BienTheBanh_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaBienThe, MaSP, TenBienThe, GiaBan, SoLuongThucTe,
           SoLuongDaGiu, SoNgayChuanBiDuKien, TrangThaiBan
    FROM dbo.BienTheBanh
    ORDER BY MaBienThe;
END;
GO

CREATE OR ALTER PROC dbo.DonHang_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaDonHang, MaKH, MaNVXuLy, MaNVXacNhan, MaNVHoanTat,
           NgayDat, NgayXacNhan, NgayHoanTat, NgayGiaoDuKien,
           TrangThaiDonHang, TenNguoiNhan, DienThoaiNguoiNhan,
           DiaChiGiaoHang, PhiGiaoHang, TienGiamGia, TienCocYeuCau,
           TongTienHang, TongTienPhaiTra, GhiChu
    FROM dbo.DonHang
    ORDER BY NgayDat DESC, MaDonHang DESC;
END;
GO

CREATE OR ALTER PROC dbo.ChiTietDonHang_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaDonHang, STT, MaBienThe, SoLuong, DonGiaTaiThoiDiemDat,
           SoLuongCoSanDaGiu, SoLuongCanLam, SoLuongDaLam,
           NgayDuKienCoHang, GhiChu
    FROM dbo.ChiTietDonHang
    ORDER BY MaDonHang, STT;
END;
GO

CREATE OR ALTER PROC dbo.ThanhToan_DS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaThanhToan, MaDonHang, MaThanhToanGoc, LoaiGiaoDich,
           SoTien, PhuongThuc, TrangThaiThanhToan, ThoiGianTao,
           ThoiGianXacNhan, MaNVXacNhan, DuongDanChungTu, GhiChu
    FROM dbo.ThanhToan
    ORDER BY ThoiGianTao DESC, MaThanhToan DESC;
END;
GO
