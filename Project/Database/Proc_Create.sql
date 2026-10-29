USE DbQLTB;
GO

/* ===== Nhóm A: bảng độc lập ===== */

-- 1. Nhân viên
CREATE OR ALTER PROC dbo.NhanVien_Them
    @HoNV       nvarchar(30),
    @TenNV      nvarchar(20),
    @DienThoai  varchar(11),
    @Email      varchar(40),
    @TenDN      nvarchar(50),
    @matKhauMoi nvarchar(50),
    @AnhNV      nvarchar(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @matKhauMoi IS NULL
       OR LEN(LTRIM(RTRIM(@matKhauMoi))) = 0
    BEGIN
        ;THROW 50001, N'Vui lòng nhập mật khẩu.', 1;
    END;

    DECLARE @MaNV int = 1;

    WHILE EXISTS (
        SELECT 1
        FROM dbo.NhanVien
        WHERE MaNV = @MaNV
    )
    BEGIN
        SET @MaNV = @MaNV + 1;
    END;

    INSERT INTO dbo.NhanVien
        (MaNV, HoNV, TenNV, DienThoai, Email,
         TenDN, MatKhau, AnhNV)
    VALUES
        (@MaNV, @HoNV, @TenNV, @DienThoai, @Email,
         @TenDN, HASHBYTES('SHA2_512', @matKhauMoi), @AnhNV);
END;
GO

-- 2. Quyền
CREATE OR ALTER PROC dbo.Quyen_Them
    @TenQuyen nvarchar(50),
    @MoTa     nvarchar(500) = NULL
AS
BEGIN
    DECLARE @MaQuyen int;
    SET @MaQuyen = 1;

    WHILE @MaQuyen IN (SELECT MaQuyen FROM dbo.Quyen)
    BEGIN
        SET @MaQuyen = @MaQuyen + 1;
    END

    INSERT INTO dbo.Quyen(MaQuyen, TenQuyen, MoTa)
    VALUES(@MaQuyen, @TenQuyen, @MoTa);
END
GO

-- 3. Loại bánh
CREATE OR ALTER PROC dbo.LoaiBanh_Them
    @TenLoaiBanh nvarchar(45),
    @MoTa        nvarchar(200) = NULL
AS
BEGIN
    DECLARE @MaLoaiBanh int;
    SET @MaLoaiBanh = 1;

    WHILE @MaLoaiBanh IN (SELECT MaLoaiBanh FROM dbo.LoaiBanh)
    BEGIN
        SET @MaLoaiBanh = @MaLoaiBanh + 1;
    END

    INSERT INTO dbo.LoaiBanh(MaLoaiBanh, TenLoaiBanh, MoTa)
    VALUES(@MaLoaiBanh, @TenLoaiBanh, @MoTa);
END
GO

-- 4. Khách hàng
CREATE OR ALTER PROC dbo.KhachHang_Them
    @HoKH             nvarchar(30),
    @TenKH            nvarchar(20),
    @TenDN            nvarchar(50),
    @matKhauMoi       nvarchar(50),
    @DienThoai        varchar(11)   = NULL,
    @Email            varchar(40)   = NULL,
    @DiaChiThuongDung nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @matKhauMoi IS NULL
       OR LEN(LTRIM(RTRIM(@matKhauMoi))) = 0
    BEGIN
        ;THROW 50001, N'Vui lòng nhập mật khẩu.', 1;
    END;

    DECLARE @MaKH int = 1;

    WHILE EXISTS (
        SELECT 1
        FROM dbo.KhachHang
        WHERE MaKH = @MaKH
    )
    BEGIN
        SET @MaKH = @MaKH + 1;
    END;

    INSERT INTO dbo.KhachHang
        (MaKH, HoKH, TenKH, DienThoai, Email,
         DiaChiThuongDung, TenDN, MatKhau)
    VALUES
        (@MaKH, @HoKH, @TenKH, @DienThoai, @Email,
         @DiaChiThuongDung, @TenDN,
         HASHBYTES('SHA2_512', @matKhauMoi));
END;
GO

/* ===== Nhóm B ===== */

-- 5. Phân quyền: hai mã được chọn từ bảng Quyen và NhanVien
CREATE OR ALTER PROC dbo.PhanQuyen_Them
    @MaQuyen tinyint,
    @MaNV    tinyint
AS
BEGIN
    INSERT INTO dbo.PhanQuyen(MaQuyen, MaNV)
    VALUES(@MaQuyen, @MaNV);
END
GO

-- 6. Sản phẩm
-- Mã SP + 7 chữ số (SP0000001, SP0000002...) theo CK_SanPham_MaSP.
-- NgayTao dùng giá trị mặc định; MaNVCapNhat/NgayCapNhat để NULL khi mới tạo.
CREATE OR ALTER PROC dbo.SanPham_Them
    @MaLoaiBanh       tinyint,
    @TenSP            nvarchar(100),
    @MaNVTao          tinyint,
    @MoTaNgan         nvarchar(300) = NULL,
    @MoTaChiTiet      nvarchar(MAX) = NULL,
    @TrangThaiHienThi varchar(12)   = 'DRAFT'
AS
BEGIN
    DECLARE @MaSo int;
    DECLARE @MaSP char(9);
    SET @MaSo = 1;

    WHILE EXISTS (SELECT 1 FROM dbo.SanPham
                  WHERE MaSP = 'SP' + RIGHT('0000000' + CAST(@MaSo AS varchar(7)), 7))
    BEGIN
        SET @MaSo = @MaSo + 1;
    END

    IF @MaSo > 9999999
        THROW 50001, N'Đã hết mã sản phẩm.', 1;

    SET @MaSP = 'SP' + RIGHT('0000000' + CAST(@MaSo AS varchar(7)), 7);

    INSERT INTO dbo.SanPham
        (MaSP, MaLoaiBanh, TenSP, MoTaNgan, MoTaChiTiet, TrangThaiHienThi, MaNVTao)
    VALUES
        (@MaSP, @MaLoaiBanh, @TenSP, @MoTaNgan, @MoTaChiTiet, @TrangThaiHienThi, @MaNVTao);
END
GO

-- 7. Hình ảnh bánh
CREATE OR ALTER PROC dbo.HinhAnhBanh_Them
    @MaSP          char(9),
    @DuongDanAnh   nvarchar(500),
    @LaAnhDaiDien  bit      = 0,
    @ThuTuHienThi  smallint = 1
AS
BEGIN
    DECLARE @MaAnh int;
    SET @MaAnh = 1;

    WHILE @MaAnh IN (SELECT MaAnh FROM dbo.HinhAnhBanh)
    BEGIN
        SET @MaAnh = @MaAnh + 1;
    END

    INSERT INTO dbo.HinhAnhBanh
        (MaAnh, MaSP, DuongDanAnh, LaAnhDaiDien, ThuTuHienThi)
    VALUES
        (@MaAnh, @MaSP, @DuongDanAnh, @LaAnhDaiDien, @ThuTuHienThi);
END
GO

-- 8. Biến thể bánh
-- SoLuongDaGiu luôn bắt đầu bằng 0 (do quy trình đặt hàng cập nhật).
CREATE OR ALTER PROC dbo.BienTheBanh_Them
    @MaSP                 char(9),
    @TenBienThe           nvarchar(50),
    @GiaBan               decimal(12,2),
    @SoLuongThucTe        int      = 0,
    @SoNgayChuanBiDuKien  smallint = 0,
    @TrangThaiBan         bit      = 1
AS
BEGIN
    DECLARE @MaBienThe int;
    SET @MaBienThe = 1;

    WHILE @MaBienThe IN (SELECT MaBienThe FROM dbo.BienTheBanh)
    BEGIN
        SET @MaBienThe = @MaBienThe + 1;
    END

    INSERT INTO dbo.BienTheBanh
        (MaBienThe, MaSP, TenBienThe, GiaBan, SoLuongThucTe,
         SoNgayChuanBiDuKien, TrangThaiBan)
    VALUES
        (@MaBienThe, @MaSP, @TenBienThe, @GiaBan, @SoLuongThucTe,
         @SoNgayChuanBiDuKien, @TrangThaiBan);
END
GO

/* ===== Nhóm C: bảng đầu phiếu/đơn ===== */

-- 9. Đơn hàng
-- Mã DH + yyMMdd + số thứ tự 001-999 (theo ngày đặt).
-- Trạng thái, tổng tiền, tiền giảm giá dùng giá trị mặc định khi mới tạo;
-- các cột xác nhận/hoàn tất và tiền cọc được cập nhật ở bước xử lý đơn.
CREATE OR ALTER PROC dbo.DonHang_Them
    @TenNguoiNhan       nvarchar(80),
    @DienThoaiNguoiNhan varchar(11),
    @DiaChiGiaoHang     nvarchar(300),
    @MaKH               smallint       = NULL,
    @MaNVXuLy           tinyint        = NULL,
    @PhiGiaoHang        decimal(12,2)  = 0,
    @NgayGiaoDuKien     datetime2(0)   = NULL,
    @GhiChu             nvarchar(1000) = NULL,
    @NgayDat            datetime2(0)   = NULL
AS
BEGIN
    DECLARE @Ngay datetime2(0);
    DECLARE @TienTo char(8);
    DECLARE @STT int;
    DECLARE @MaDonHang char(11);

    SET @Ngay = ISNULL(@NgayDat, SYSDATETIME());
    SET @TienTo = 'DH' + CONVERT(char(6), @Ngay, 12);
    SET @STT = 1;

    WHILE @STT <= 999
          AND EXISTS (SELECT 1 FROM dbo.DonHang
                      WHERE MaDonHang = @TienTo + RIGHT('000' + CAST(@STT AS varchar(3)), 3))
    BEGIN
        SET @STT = @STT + 1;
    END

    IF @STT > 999
        THROW 50001, N'Đã hết số thứ tự đơn hàng trong ngày.', 1;

    SET @MaDonHang = @TienTo + RIGHT('000' + CAST(@STT AS varchar(3)), 3);

    INSERT INTO dbo.DonHang
        (MaDonHang, MaKH, MaNVXuLy, NgayDat, NgayGiaoDuKien,
         TenNguoiNhan, DienThoaiNguoiNhan, DiaChiGiaoHang, PhiGiaoHang, GhiChu)
    VALUES
        (@MaDonHang, @MaKH, @MaNVXuLy, @Ngay, @NgayGiaoDuKien,
         @TenNguoiNhan, @DienThoaiNguoiNhan, @DiaChiGiaoHang, @PhiGiaoHang, @GhiChu);

    INSERT dbo.LichSuTrangThaiDonHang
        (MaDonHang,TrangThaiCu,TrangThaiMoi,NguoiThucHien,MaNVThucHien,ThoiDiem,LyDo)
    VALUES
        (@MaDonHang,NULL,'CHO_NHAN_VIEN_XU_LY',
         CASE WHEN @MaNVXuLy IS NULL THEN 'HE_THONG' ELSE 'NHAN_VIEN' END,
         @MaNVXuLy,@Ngay,N'Tạo đơn hàng mới.');
END
GO

/* ===== Nhóm D: bảng chi tiết (số thứ tự dòng STT tự cấp) ===== */

-- 10. Chi tiết đơn hàng
-- Nếu không truyền đơn giá thì lấy GiaBan hiện tại của biến thể bánh.
-- Các cột số lượng giữ/cần làm/đã làm dùng giá trị mặc định 0.
CREATE OR ALTER PROC dbo.ChiTietDonHang_Them
    @MaDonHang            char(11),
    @MaBienThe            smallint,
    @SoLuong              int,
    @DonGiaTaiThoiDiemDat decimal(12,2) = NULL,
    @NgayDuKienCoHang     datetime2(0)  = NULL,
    @GhiChu               nvarchar(500) = NULL
AS
BEGIN
    DECLARE @STT int;
    SET @STT = 1;

    WHILE EXISTS (SELECT 1 FROM dbo.ChiTietDonHang
                  WHERE MaDonHang = @MaDonHang AND STT = @STT)
    BEGIN
        SET @STT = @STT + 1;
    END

    IF @DonGiaTaiThoiDiemDat IS NULL
        SELECT @DonGiaTaiThoiDiemDat = GiaBan
        FROM dbo.BienTheBanh
        WHERE MaBienThe = @MaBienThe;

    INSERT INTO dbo.ChiTietDonHang
        (MaDonHang, STT, MaBienThe, SoLuong, DonGiaTaiThoiDiemDat,
         NgayDuKienCoHang, GhiChu)
    VALUES
        (@MaDonHang, @STT, @MaBienThe, @SoLuong, @DonGiaTaiThoiDiemDat,
         @NgayDuKienCoHang, @GhiChu);
END
GO

-- 11. Thanh toán / hoàn tiền
-- Mã TT + yyMMdd + số thứ tự 001-999 (theo ngày tạo).
-- PAYMENT: MaThanhToanGoc = NULL; REFUND: bắt buộc có MaThanhToanGoc.
-- Trạng thái mặc định PENDING; xác nhận (MaNVXacNhan, ThoiGianXacNhan) cập nhật sau.
CREATE OR ALTER PROC dbo.ThanhToan_Them
    @MaDonHang       char(11),
    @LoaiGiaoDich    varchar(10),
    @SoTien          decimal(14,2),
    @PhuongThuc      varchar(20),
    @MaThanhToanGoc  char(11)      = NULL,
    @DuongDanChungTu nvarchar(500) = NULL,
    @GhiChu          nvarchar(500) = NULL
AS
BEGIN
    DECLARE @Ngay datetime2(0);
    DECLARE @TienTo char(8);
    DECLARE @STT int;
    DECLARE @MaThanhToan char(11);

    SET @Ngay = SYSDATETIME();
    SET @TienTo = 'TT' + CONVERT(char(6), @Ngay, 12);
    SET @STT = 1;

    WHILE @STT <= 999
          AND EXISTS (SELECT 1 FROM dbo.ThanhToan
                      WHERE MaThanhToan = @TienTo + RIGHT('000' + CAST(@STT AS varchar(3)), 3))
    BEGIN
        SET @STT = @STT + 1;
    END

    IF @STT > 999
        THROW 50001, N'Đã hết số thứ tự thanh toán trong ngày.', 1;

    SET @MaThanhToan = @TienTo + RIGHT('000' + CAST(@STT AS varchar(3)), 3);

    INSERT INTO dbo.ThanhToan
        (MaThanhToan, MaDonHang, MaThanhToanGoc, LoaiGiaoDich, SoTien,
         PhuongThuc, ThoiGianTao, DuongDanChungTu, GhiChu)
    VALUES
        (@MaThanhToan, @MaDonHang, @MaThanhToanGoc, @LoaiGiaoDich, @SoTien,
         @PhuongThuc, @Ngay, @DuongDanChungTu, @GhiChu);
END
GO
