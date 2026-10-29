/*
    Migration cho database DbQLTB đã được tạo trước khi có nhật ký trạng thái.
    Chạy một lần trước Proc_Edit.sql và Proc_Details.sql phiên bản mới.
    Database mới cài bằng Table.sql đã có bảng này thì script sẽ không tạo trùng.
*/
USE DbQLTB;
GO

IF OBJECT_ID(N'dbo.LichSuTrangThaiDonHang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuTrangThaiDonHang
    (
        MaLichSu          bigint IDENTITY(1,1) NOT NULL,
        MaDonHang         char(11) NOT NULL,
        TrangThaiCu       varchar(30) NULL,
        TrangThaiMoi      varchar(30) NOT NULL,
        NguoiThucHien     varchar(20) NOT NULL,
        MaNVThucHien      tinyint NULL,
        ThoiDiem          datetime2(0) NOT NULL
            CONSTRAINT DF_LichSuDonHang_ThoiDiem DEFAULT (SYSDATETIME()),
        LyDo              nvarchar(500) NULL,

        CONSTRAINT PK_LichSuTrangThaiDonHang PRIMARY KEY (MaLichSu),
        CONSTRAINT FK_LichSuDonHang_DonHang
            FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang(MaDonHang),
        CONSTRAINT FK_LichSuDonHang_NhanVien
            FOREIGN KEY (MaNVThucHien) REFERENCES dbo.NhanVien(MaNV),
        CONSTRAINT CK_LichSuDonHang_NguoiThucHien
            CHECK (NguoiThucHien IN ('NHAN_VIEN','KHACH_HANG','HE_THONG')),
        CONSTRAINT CK_LichSuDonHang_NhanVien CHECK
        (
            (NguoiThucHien = 'NHAN_VIEN' AND MaNVThucHien IS NOT NULL)
            OR (NguoiThucHien <> 'NHAN_VIEN' AND MaNVThucHien IS NULL)
        )
    );
END;
GO

-- Tạo mốc lịch sử ban đầu cho các đơn cũ, tránh để trang chi tiết trống.
INSERT dbo.LichSuTrangThaiDonHang
    (MaDonHang,TrangThaiCu,TrangThaiMoi,NguoiThucHien,MaNVThucHien,ThoiDiem,LyDo)
SELECT d.MaDonHang,NULL,d.TrangThaiDonHang,'HE_THONG',NULL,d.NgayDat,
       N'Tạo mốc lịch sử khi cài quy trình trạng thái.'
FROM dbo.DonHang d
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.LichSuTrangThaiDonHang l WHERE l.MaDonHang=d.MaDonHang
);
GO
