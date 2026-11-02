/*
    Bắt buộc toàn bộ giá và số tiền giao dịch phải là số nguyên VNĐ.
    Chạy một lần trên database hiện có; có thể chạy lại sau đó.
    Nếu tồn tại dữ liệu lẻ, script dừng để tránh tự ý làm tròn tiền.
*/
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.BienTheBanh', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DonHang', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ChiTietDonHang', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ThanhToan', N'U') IS NULL
BEGIN
    ;THROW 52000, N'Không tìm thấy đủ bảng của WebQLTB. Hãy cài Table.sql trước.', 1;
END;
GO

IF EXISTS (SELECT 1 FROM dbo.BienTheBanh WHERE GiaBan <> ROUND(GiaBan, 0))
   OR EXISTS (SELECT 1 FROM dbo.DonHang WHERE
        PhiGiaoHang <> ROUND(PhiGiaoHang, 0)
        OR TienGiamGia <> ROUND(TienGiamGia, 0)
        OR TienCocYeuCau <> ROUND(TienCocYeuCau, 0)
        OR TongTienHang <> ROUND(TongTienHang, 0)
        OR TongTienPhaiTra <> ROUND(TongTienPhaiTra, 0))
   OR EXISTS (SELECT 1 FROM dbo.ChiTietDonHang
        WHERE DonGiaTaiThoiDiemDat <> ROUND(DonGiaTaiThoiDiemDat, 0))
   OR EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE SoTien <> ROUND(SoTien, 0))
BEGIN
    ;THROW 52001, N'Có giá hoặc số tiền lẻ trong dữ liệu hiện tại. Hãy đối chiếu và xử lý các dòng đó trước khi chạy lại migration.', 1;
END;
GO

BEGIN TRY
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BienTheBanh_GiaBan')
    ALTER TABLE dbo.BienTheBanh DROP CONSTRAINT CK_BienTheBanh_GiaBan;
ALTER TABLE dbo.BienTheBanh WITH CHECK
    ADD CONSTRAINT CK_BienTheBanh_GiaBan
    CHECK (GiaBan >= 0 AND GiaBan = ROUND(GiaBan, 0));

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DonHang_Tien')
    ALTER TABLE dbo.DonHang DROP CONSTRAINT CK_DonHang_Tien;
ALTER TABLE dbo.DonHang WITH CHECK
    ADD CONSTRAINT CK_DonHang_Tien
    CHECK
    (
        PhiGiaoHang >= 0
        AND PhiGiaoHang = ROUND(PhiGiaoHang, 0)
        AND TienGiamGia >= 0
        AND TienGiamGia = ROUND(TienGiamGia, 0)
        AND TongTienHang >= 0
        AND TongTienHang = ROUND(TongTienHang, 0)
        AND TongTienPhaiTra >= 0
        AND TongTienPhaiTra = ROUND(TongTienPhaiTra, 0)
        AND TienGiamGia <= TongTienHang + PhiGiaoHang
        AND
        (
            TienCocYeuCau IS NULL
            OR
            (
                TienCocYeuCau >= 0
                AND TienCocYeuCau = ROUND(TienCocYeuCau, 0)
                AND TienCocYeuCau <= TongTienPhaiTra
            )
        )
    );

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CTDH_DonGia')
    ALTER TABLE dbo.ChiTietDonHang DROP CONSTRAINT CK_CTDH_DonGia;
ALTER TABLE dbo.ChiTietDonHang WITH CHECK
    ADD CONSTRAINT CK_CTDH_DonGia
    CHECK (DonGiaTaiThoiDiemDat >= 0 AND DonGiaTaiThoiDiemDat = ROUND(DonGiaTaiThoiDiemDat, 0));

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ThanhToan_SoTien')
    ALTER TABLE dbo.ThanhToan DROP CONSTRAINT CK_ThanhToan_SoTien;
ALTER TABLE dbo.ThanhToan WITH CHECK
    ADD CONSTRAINT CK_ThanhToan_SoTien
    CHECK (SoTien > 0 AND SoTien = ROUND(SoTien, 0));

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    ;THROW;
END CATCH;
GO
