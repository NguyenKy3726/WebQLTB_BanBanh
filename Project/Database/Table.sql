use DbQLTB
go

/*
    Nhóm A: các bảng độc lập, chưa có khóa ngoại đến bảng khác.
    Mật khẩu dùng varbinary(MAX) giống cấu trúc trong Table(1).sql.
    Nên lưu giá trị băm mật khẩu dưới dạng byte, không lưu mật khẩu rõ.
*/

-- 1. Nhân viên
CREATE TABLE dbo.NhanVien
(
    MaNV       tinyint       NOT NULL,
    HoNV       nvarchar(30)  NOT NULL,
    TenNV      nvarchar(20)  NOT NULL,
    DienThoai  varchar(11)   NOT NULL,
    Email      varchar(40)   NOT NULL,
    TenDN      nvarchar(50)  NOT NULL,
    MatKhau    varbinary(MAX) NOT NULL,
    AnhNV      nvarchar(250) NULL,

    CONSTRAINT PK_NhanVien PRIMARY KEY (MaNV),
    CONSTRAINT UQ_NhanVien_TenDN UNIQUE (TenDN)
);
GO

-- 2. Quyền
CREATE TABLE dbo.Quyen
(
    MaQuyen  tinyint       NOT NULL,
    TenQuyen nvarchar(50)  NOT NULL,
    MoTa     nvarchar(500) NULL,

    CONSTRAINT PK_Quyen PRIMARY KEY (MaQuyen),
    CONSTRAINT UQ_Quyen_TenQuyen UNIQUE (TenQuyen)
);
GO

-- 3. Loại bánh
CREATE TABLE dbo.LoaiBanh
(
    MaLoaiBanh tinyint       NOT NULL,
    TenLoaiBanh nvarchar(45) NOT NULL,
    MoTa        nvarchar(200) NULL,

    CONSTRAINT PK_LoaiBanh PRIMARY KEY (MaLoaiBanh),
    CONSTRAINT UQ_LoaiBanh_TenLoaiBanh UNIQUE (TenLoaiBanh)
);
GO

-- 4. Khách hàng
CREATE TABLE dbo.KhachHang
(
    MaKH            smallint       NOT NULL,
    HoKH            nvarchar(30)   NOT NULL,
    TenKH           nvarchar(20)   NOT NULL,
    DienThoai       varchar(11)    NULL,
    Email           varchar(40)    NULL,
    DiaChiThuongDung nvarchar(200) NULL,
    TenDN           nvarchar(50)   NOT NULL,
    MatKhau         varbinary(MAX) NOT NULL,

    CONSTRAINT PK_KhachHang PRIMARY KEY (MaKH),
    CONSTRAINT UQ_KhachHang_TenDN UNIQUE (TenDN)
);
GO

/*
    Nhóm B: tạo sau nhóm A.
    Các mã không dùng IDENTITY để phù hợp cách cấp mã của database gốc.
    MaSP: YYMMDD + số thứ tự 001-999 (9 chữ số).
*/
-- 5. Phân quyền nhân viên
CREATE TABLE dbo.PhanQuyen
(
    MaQuyen tinyint NOT NULL,
    MaNV    tinyint NOT NULL,

    CONSTRAINT PK_PhanQuyen PRIMARY KEY (MaQuyen, MaNV),
    CONSTRAINT FK_PhanQuyen_Quyen
        FOREIGN KEY (MaQuyen) REFERENCES dbo.Quyen (MaQuyen),
    CONSTRAINT FK_PhanQuyen_NhanVien
        FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien (MaNV)
);
GO

-- 6. Sản phẩm bánh: mỗi mẫu bánh chỉ có một dòng ở bảng này
CREATE TABLE dbo.SanPham
(
    MaSP              char(9)        NOT NULL,
    MaLoaiBanh        tinyint        NOT NULL,
    TenSP             nvarchar(100)  NOT NULL,
    MoTaNgan          nvarchar(300)  NULL,
    MoTaChiTiet       nvarchar(MAX)  NULL,
    TrangThaiHienThi  varchar(12)    NOT NULL
        CONSTRAINT DF_SanPham_TrangThaiHienThi DEFAULT ('DRAFT'),
    MaNVTao           tinyint        NOT NULL,
    NgayTao           datetime2(0)   NOT NULL
        CONSTRAINT DF_SanPham_NgayTao DEFAULT (SYSDATETIME()),
    MaNVCapNhat       tinyint        NULL,
    NgayCapNhat       datetime2(0)   NULL,

    CONSTRAINT PK_SanPham PRIMARY KEY (MaSP),
    CONSTRAINT FK_SanPham_LoaiBanh
        FOREIGN KEY (MaLoaiBanh) REFERENCES dbo.LoaiBanh (MaLoaiBanh),
    CONSTRAINT FK_SanPham_NhanVienTao
        FOREIGN KEY (MaNVTao) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT FK_SanPham_NhanVienCapNhat
        FOREIGN KEY (MaNVCapNhat) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT CK_SanPham_MaSP
        CHECK
        (
            MaSP COLLATE Latin1_General_100_BIN2 LIKE
                'SP[0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            AND RIGHT(MaSP, 7) BETWEEN '0000001' AND '9999999'
        ),
    CONSTRAINT CK_SanPham_TrangThaiHienThi
        CHECK (TrangThaiHienThi IN ('DRAFT', 'PUBLISHED', 'HIDDEN')),
    CONSTRAINT CK_SanPham_CapNhat
        CHECK
        (
            (MaNVCapNhat IS NULL AND NgayCapNhat IS NULL)
            OR
            (MaNVCapNhat IS NOT NULL AND NgayCapNhat IS NOT NULL)
        )
);
GO

-- 7. Ảnh bánh: một mẫu bánh có thể có nhiều ảnh
CREATE TABLE dbo.HinhAnhBanh
(
    MaAnh          smallint       NOT NULL,
    MaSP           char(9)        NOT NULL,
    DuongDanAnh    nvarchar(500)  NOT NULL,
    LaAnhDaiDien   bit            NOT NULL
        CONSTRAINT DF_HinhAnhBanh_LaAnhDaiDien DEFAULT (0),
    ThuTuHienThi   smallint       NOT NULL
        CONSTRAINT DF_HinhAnhBanh_ThuTuHienThi DEFAULT (1),

    CONSTRAINT PK_HinhAnhBanh PRIMARY KEY (MaAnh),
    CONSTRAINT FK_HinhAnhBanh_SanPham
        FOREIGN KEY (MaSP) REFERENCES dbo.SanPham (MaSP),
    CONSTRAINT CK_HinhAnhBanh_ThuTuHienThi
        CHECK (ThuTuHienThi > 0)
);
GO

-- Mỗi mẫu bánh chỉ có tối đa một ảnh đại diện
CREATE UNIQUE INDEX UX_HinhAnhBanh_AnhDaiDien
    ON dbo.HinhAnhBanh (MaSP)
    WHERE LaAnhDaiDien = 1;
GO

-- 8. Biến thể bánh: kích thước/lựa chọn bán và số lượng của lựa chọn đó
CREATE TABLE dbo.BienTheBanh
(
    MaBienThe             smallint       NOT NULL,
    MaSP                  char(9)        NOT NULL,
    TenBienThe            nvarchar(50)   NOT NULL,
    GiaBan                decimal(12,2)  NOT NULL,
    SoLuongThucTe         int            NOT NULL
        CONSTRAINT DF_BienTheBanh_SoLuongThucTe DEFAULT (0),
    SoLuongDaGiu          int            NOT NULL
        CONSTRAINT DF_BienTheBanh_SoLuongDaGiu DEFAULT (0),
    SoNgayChuanBiDuKien   smallint       NOT NULL
        CONSTRAINT DF_BienTheBanh_SoNgayChuanBi DEFAULT (0),
    TrangThaiBan          bit            NOT NULL
        CONSTRAINT DF_BienTheBanh_TrangThaiBan DEFAULT (1),

    CONSTRAINT PK_BienTheBanh PRIMARY KEY (MaBienThe),
    CONSTRAINT FK_BienTheBanh_SanPham
        FOREIGN KEY (MaSP) REFERENCES dbo.SanPham (MaSP),
    CONSTRAINT UQ_BienTheBanh_SanPham_Ten
        UNIQUE (MaSP, TenBienThe),
    CONSTRAINT CK_BienTheBanh_GiaBan
        CHECK (GiaBan >= 0),
    CONSTRAINT CK_BienTheBanh_SoLuong
        CHECK
        (
            SoLuongThucTe >= 0
            AND SoLuongDaGiu >= 0
            AND SoLuongDaGiu <= SoLuongThucTe
        ),
    CONSTRAINT CK_BienTheBanh_SoNgayChuanBi
        CHECK (SoNgayChuanBiDuKien >= 0)
);
GO


/*
    Nhóm C: tạo sau nhóm A và nhóm B.
    Các bảng trong nhóm này là bảng đầu phiếu/đơn hàng; bảng chi tiết thuộc nhóm D.
    MaDonHang: DH + YYMMDD + số thứ tự 001-999.
*/

-- 9. Đơn hàng
CREATE TABLE dbo.DonHang
(
    MaDonHang          char(11)        NOT NULL,
    MaKH               smallint        NULL,
    MaNVXuLy           tinyint         NULL,
    MaNVXacNhan        tinyint         NULL,
    MaNVHoanTat        tinyint         NULL,
    NgayDat            datetime2(0)    NOT NULL
        CONSTRAINT DF_DonHang_NgayDat DEFAULT (SYSDATETIME()),
    NgayXacNhan        datetime2(0)    NULL,
    NgayHoanTat        datetime2(0)    NULL,
    NgayGiaoDuKien     datetime2(0)    NULL,
    TrangThaiDonHang   varchar(30)     NOT NULL
        CONSTRAINT DF_DonHang_TrangThai DEFAULT ('CHO_NHAN_VIEN_XU_LY'),
    TenNguoiNhan       nvarchar(80)    NOT NULL,
    DienThoaiNguoiNhan varchar(11)     NOT NULL,
    DiaChiGiaoHang     nvarchar(300)   NOT NULL,
    PhiGiaoHang        decimal(12,2)   NOT NULL
        CONSTRAINT DF_DonHang_PhiGiaoHang DEFAULT (0),
    TienGiamGia        decimal(12,2)   NOT NULL
        CONSTRAINT DF_DonHang_TienGiamGia DEFAULT (0),
    TienCocYeuCau      decimal(12,2)   NULL,
    TongTienHang       decimal(14,2)   NOT NULL
        CONSTRAINT DF_DonHang_TongTienHang DEFAULT (0),
    TongTienPhaiTra    decimal(14,2)   NOT NULL
        CONSTRAINT DF_DonHang_TongTienPhaiTra DEFAULT (0),
    GhiChu             nvarchar(1000)  NULL,

    CONSTRAINT PK_DonHang PRIMARY KEY (MaDonHang),
    CONSTRAINT FK_DonHang_KhachHang
        FOREIGN KEY (MaKH) REFERENCES dbo.KhachHang (MaKH),
    CONSTRAINT FK_DonHang_NhanVienXuLy
        FOREIGN KEY (MaNVXuLy) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT FK_DonHang_NhanVienXacNhan
        FOREIGN KEY (MaNVXacNhan) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT FK_DonHang_NhanVienHoanTat
        FOREIGN KEY (MaNVHoanTat) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT CK_DonHang_MaDonHang
        CHECK
        (
            MaDonHang COLLATE Latin1_General_100_BIN2 LIKE
                'DH[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            AND RIGHT(MaDonHang, 3) BETWEEN '001' AND '999'
            AND TRY_CONVERT(date, '20' + SUBSTRING(MaDonHang, 3, 6), 112) IS NOT NULL
        ),
    CONSTRAINT CK_DonHang_TrangThai
        CHECK
        (
            TrangThaiDonHang IN
            (
                'CHO_NHAN_VIEN_XU_LY',
                'CHO_KHACH_XAC_NHAN',
                'CHO_THANH_TOAN_COC',
                'DA_XAC_NHAN',
                'DANG_CHUAN_BI',
                'SAN_SANG_GIAO',
                'DANG_GIAO',
                'HOAN_TAT',
                'DA_HUY'
            )
        ),
    CONSTRAINT CK_DonHang_Tien
        CHECK
        (
            PhiGiaoHang >= 0
            AND TienGiamGia >= 0
            AND TongTienHang >= 0
            AND TongTienPhaiTra >= 0
            AND TienGiamGia <= TongTienHang + PhiGiaoHang
            AND
            (
                TienCocYeuCau IS NULL
                OR
                (
                    TienCocYeuCau >= 0
                    AND TienCocYeuCau <= TongTienPhaiTra
                )
            )
        )
);
GO

-- 10. Nhật ký thay đổi trạng thái đơn: giữ người thực hiện, thời điểm và lý do.
CREATE TABLE dbo.LichSuTrangThaiDonHang
(
    MaLichSu          bigint IDENTITY(1,1) NOT NULL,
    MaDonHang         char(11) NOT NULL,
    TrangThaiCu       varchar(30) NULL,
    TrangThaiMoi      varchar(30) NOT NULL,
    NguoiThucHien     varchar(20) NOT NULL,
    MaNVThucHien      tinyint NULL,
    ThoiDiem          datetime2(0) NOT NULL CONSTRAINT DF_LichSuDonHang_ThoiDiem DEFAULT (SYSDATETIME()),
    LyDo              nvarchar(500) NULL,

    CONSTRAINT PK_LichSuTrangThaiDonHang PRIMARY KEY (MaLichSu),
    CONSTRAINT FK_LichSuDonHang_DonHang FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang(MaDonHang),
    CONSTRAINT FK_LichSuDonHang_NhanVien FOREIGN KEY (MaNVThucHien) REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT CK_LichSuDonHang_NguoiThucHien CHECK (NguoiThucHien IN ('NHAN_VIEN','KHACH_HANG','HE_THONG')),
    CONSTRAINT CK_LichSuDonHang_NhanVien CHECK
    (
        (NguoiThucHien = 'NHAN_VIEN' AND MaNVThucHien IS NOT NULL)
        OR (NguoiThucHien <> 'NHAN_VIEN' AND MaNVThucHien IS NULL)
    )
);
GO

/*
    Nhóm D: chi tiết và giao dịch phụ thuộc các bảng đơn hàng.
    Các bảng chi tiết dùng khóa chính ghép giữa mã chứng từ và số thứ tự dòng.
    MaThanhToan: TT + YYMMDD + số thứ tự 001-999.
*/

-- 11. Chi tiết đơn hàng
CREATE TABLE dbo.ChiTietDonHang
(
    MaDonHang             char(11)       NOT NULL,
    STT                   smallint       NOT NULL,
    MaBienThe             smallint       NOT NULL,
    SoLuong               int            NOT NULL,
    DonGiaTaiThoiDiemDat  decimal(12,2)  NOT NULL,
    SoLuongCoSanDaGiu     int            NOT NULL
        CONSTRAINT DF_CTDH_SoLuongCoSanDaGiu DEFAULT (0),
    SoLuongCanLam         int            NOT NULL
        CONSTRAINT DF_CTDH_SoLuongCanLam DEFAULT (0),
    SoLuongDaLam          int            NOT NULL
        CONSTRAINT DF_CTDH_SoLuongDaLam DEFAULT (0),
    NgayDuKienCoHang      datetime2(0)   NULL,
    GhiChu                nvarchar(500)  NULL,

    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY (MaDonHang, STT),
    CONSTRAINT FK_CTDH_DonHang
        FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang (MaDonHang),
    CONSTRAINT FK_CTDH_BienTheBanh
        FOREIGN KEY (MaBienThe) REFERENCES dbo.BienTheBanh (MaBienThe),
    CONSTRAINT CK_CTDH_STT
        CHECK (STT > 0),
    CONSTRAINT CK_CTDH_SoLuong
        CHECK
        (
            SoLuong > 0
            AND SoLuongCoSanDaGiu >= 0
            AND SoLuongCanLam >= 0
            AND SoLuongDaLam >= 0
            AND SoLuongDaLam <= SoLuongCanLam
            AND SoLuongCoSanDaGiu + SoLuongCanLam <= SoLuong
        ),
    CONSTRAINT CK_CTDH_DonGia
        CHECK (DonGiaTaiThoiDiemDat >= 0)
);
GO

-- 12. Thanh toán và hoàn tiền
CREATE TABLE dbo.ThanhToan
(
    MaThanhToan       char(11)       NOT NULL,
    MaDonHang         char(11)       NOT NULL,
    MaThanhToanGoc    char(11)       NULL,
    LoaiGiaoDich      varchar(10)    NOT NULL,
    SoTien            decimal(14,2)  NOT NULL,
    PhuongThuc        varchar(20)    NOT NULL,
    TrangThaiThanhToan varchar(10)   NOT NULL
        CONSTRAINT DF_ThanhToan_TrangThai DEFAULT ('PENDING'),
    ThoiGianTao       datetime2(0)   NOT NULL
        CONSTRAINT DF_ThanhToan_ThoiGianTao DEFAULT (SYSDATETIME()),
    ThoiGianXacNhan   datetime2(0)   NULL,
    MaNVXacNhan       tinyint        NULL,
    DuongDanChungTu   nvarchar(500)  NULL,
    GhiChu            nvarchar(500)  NULL,

    CONSTRAINT PK_ThanhToan PRIMARY KEY (MaThanhToan),
    CONSTRAINT FK_ThanhToan_DonHang
        FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang (MaDonHang),
    CONSTRAINT FK_ThanhToan_ThanhToanGoc
        FOREIGN KEY (MaThanhToanGoc) REFERENCES dbo.ThanhToan (MaThanhToan),
    CONSTRAINT FK_ThanhToan_NhanVienXacNhan
        FOREIGN KEY (MaNVXacNhan) REFERENCES dbo.NhanVien (MaNV),
    CONSTRAINT CK_ThanhToan_MaThanhToan
        CHECK
        (
            MaThanhToan COLLATE Latin1_General_100_BIN2 LIKE
                'TT[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            AND RIGHT(MaThanhToan, 3) BETWEEN '001' AND '999'
            AND TRY_CONVERT(date, '20' + SUBSTRING(MaThanhToan, 3, 6), 112) IS NOT NULL
        ),
    CONSTRAINT CK_ThanhToan_LoaiGiaoDich
        CHECK
        (
            (LoaiGiaoDich = 'PAYMENT' AND MaThanhToanGoc IS NULL)
            OR (LoaiGiaoDich = 'REFUND' AND MaThanhToanGoc IS NOT NULL)
        ),
    CONSTRAINT CK_ThanhToan_SoTien
        CHECK (SoTien > 0),
    CONSTRAINT CK_ThanhToan_PhuongThuc
        CHECK (PhuongThuc IN ('CASH', 'BANK_TRANSFER')),
    CONSTRAINT CK_ThanhToan_TrangThai
        CHECK (TrangThaiThanhToan IN ('PENDING', 'SUCCESS', 'REJECTED'))
);
GO
