/*
    Buoc 7 - Luu dia chi nhan banh lam dia chi thuong dung.
    Chi cap nhat thu tuc dbo.DonHang_WebDat.
    Khong tao/xoa bang, khong sua cau truc, khong cap nhat don cu.
*/
USE [DbQLTB];
GO

CREATE OR ALTER PROCEDURE dbo.DonHang_WebDat
    @MaKH smallint,
    @TenNguoiNhan nvarchar(80),
    @DienThoaiNguoiNhan varchar(11),
    @DiaChiGiaoHang nvarchar(300),
    @GioHangJson nvarchar(max),
    @GhiChu nvarchar(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISJSON(@GioHangJson) <> 1
        THROW 51030, N'Giỏ hàng không hợp lệ.', 1;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE
            @K int,
            @Gio datetime2(0) = SYSDATETIME(),
            @Ma char(11),
            @Tong decimal(14,2);

        EXEC @K = sys.sp_getapplock
            @Resource = N'DbQLTB:ProcSua:NghiepVu',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @K < 0
            THROW 51031, N'Hệ thống đang xử lý giao dịch khác. Hãy thử lại.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WHERE MaKH = @MaKH)
            THROW 51032, N'Không tìm thấy tài khoản khách hàng.', 1;

        DECLARE @GioHang TABLE
        (
            MaBienThe smallint PRIMARY KEY,
            SoLuong int NOT NULL
        );

        INSERT @GioHang (MaBienThe, SoLuong)
        SELECT MaBienThe, SUM(SoLuong)
        FROM OPENJSON(@GioHangJson)
             WITH (MaBienThe smallint '$.maBienThe', SoLuong int '$.soLuong')
        GROUP BY MaBienThe;

        IF NOT EXISTS (SELECT 1 FROM @GioHang)
            THROW 51033, N'Giỏ hàng đang trống.', 1;

        IF EXISTS (SELECT 1 FROM @GioHang WHERE SoLuong <= 0)
            THROW 51034, N'Số lượng đặt phải lớn hơn 0.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @GioHang g
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM dbo.BienTheBanh b
                JOIN dbo.SanPham s ON s.MaSP = b.MaSP
                WHERE b.MaBienThe = g.MaBienThe
                  AND b.TrangThaiBan = 1
                  AND s.TrangThaiHienThi = 'PUBLISHED'
            )
        )
            THROW 51035, N'Có bánh đã ngừng bán hoặc không còn hiển thị.', 1;

        SELECT @Tong =
            SUM(CONVERT(decimal(18,2), g.SoLuong) * b.GiaBan)
        FROM @GioHang g
        JOIN dbo.BienTheBanh b ON b.MaBienThe = g.MaBienThe;

        DECLARE
            @Prefix char(8) = 'DH' + CONVERT(char(6), @Gio, 12),
            @Seq int = 1;

        WHILE @Seq <= 999
          AND EXISTS
          (
              SELECT 1
              FROM dbo.DonHang
              WHERE MaDonHang = @Prefix + RIGHT('000' + CONVERT(varchar(3), @Seq), 3)
          )
            SET @Seq += 1;

        IF @Seq > 999
            THROW 51036, N'Đã hết số đơn hàng trong ngày.', 1;

        SET @Ma = @Prefix + RIGHT('000' + CONVERT(varchar(3), @Seq), 3);

        INSERT dbo.DonHang
        (
            MaDonHang, MaKH, NgayDat, TrangThaiDonHang,
            TenNguoiNhan, DienThoaiNguoiNhan, DiaChiGiaoHang,
            PhiGiaoHang, TienGiamGia, TongTienHang, TongTienPhaiTra, GhiChu
        )
        VALUES
        (
            @Ma, @MaKH, @Gio, 'CHO_NHAN_VIEN_XU_LY',
            @TenNguoiNhan, @DienThoaiNguoiNhan, @DiaChiGiaoHang,
            0, 0, @Tong, @Tong, @GhiChu
        );

        INSERT dbo.ChiTietDonHang
        (
            MaDonHang, STT, MaBienThe, SoLuong, DonGiaTaiThoiDiemDat
        )
        SELECT
            @Ma,
            ROW_NUMBER() OVER (ORDER BY g.MaBienThe),
            g.MaBienThe,
            g.SoLuong,
            b.GiaBan
        FROM @GioHang g
        JOIN dbo.BienTheBanh b ON b.MaBienThe = g.MaBienThe;

        INSERT dbo.LichSuTrangThaiDonHang
        (
            MaDonHang, TrangThaiCu, TrangThaiMoi,
            NguoiThucHien, MaNVThucHien, ThoiDiem, LyDo
        )
        VALUES
        (
            @Ma, NULL, 'CHO_NHAN_VIEN_XU_LY',
            'KHACH_HANG', NULL, @Gio, N'Khách gửi đơn hàng qua website.'
        );

        -- Cung transaction: dia chi chi doi neu viec tao don thanh cong.
        UPDATE dbo.KhachHang
        SET DiaChiThuongDung = @DiaChiGiaoHang
        WHERE MaKH = @MaKH
          AND (DiaChiThuongDung IS NULL OR DiaChiThuongDung <> @DiaChiGiaoHang);

        COMMIT;

        SELECT @Ma;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH;
END;
GO
