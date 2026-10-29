USE DbQLTB;
GO
/*
    PROC SỬA ĐÃ HIỆU CHỈNH — không cập nhật PK và các liên kết định danh.
    Không có EXEC hay UPDATE chạy ngoài thân procedure trong file này.
    PhanQuyen chỉ có khóa ghép nên không tạo proc sửa.

    Quy ước mã:
    - Giữ tham số mã của các proc cũ để kiểm tra đúng đối tượng; không SET lại chúng.
    - MaLoaiBanh của mẫu, MaSP của ảnh/biến thể, MaBienThe của dòng đơn và
      MaKH của đơn đều cố định. Muốn chuyển đối tượng phải tạo bản ghi đúng.
    - MaNVCapNhat, MaNVXuLy, MaNVXacNhan, MaNVHoanTat là người thao tác/phụ trách,
      không phải mã định danh của bản ghi đang sửa; lấy từ danh tính đã xác thực ở MVC.
    - Không đổi MaNVTao/NgayTao, ngày tạo đơn/phiếu hay bản chất giao dịch tiền.

    Thay đổi cách gọi MVC:
    - DonHang_Sua không nhận TongTienHang/TongTienPhaiTra, NgayXacNhan/NgayHoanTat;
      tiền được tính từ chi tiết, ngày xác nhận/hoàn tất lấy giờ máy chủ.
    - ThanhToan_Sua không nhận ThoiGianXacNhan; proc tự ghi khi xác nhận/từ chối.
    - NhanVien_Sua: AnhNV=NULL giữ ảnh cũ; N'' xóa đường dẫn ảnh.
    - Các tham số nội dung nullable còn lại là dữ liệu form đầy đủ: NULL có thể xóa nội dung.
      MVC phải đọc đầy đủ dữ liệu cho form sửa, không dùng proc DS rút gọn làm dữ liệu Edit.
    - SET NOCOUNT ON: không coi ExecuteNonQuery()>0 là điều kiện thành công;
      không có exception nghĩa là proc hoàn tất. Thông báo lỗi qua SqlException.

    Nghiệp vụ:
    - Đơn chờ: được sửa số lượng/giá; proc cập nhật lại tổng đầu đơn.
    - Đơn đã nhận: khóa số lượng, giá và phân bổ; chỉ cập nhật tiến độ làm bánh.
    - Xác nhận đơn giữ bánh sẵn; bánh thiếu trở thành phần cần làm.
    - Hoàn tất giảm tồn và giữ; hủy giải phóng phần giữ, không tự hoàn tiền.
      Thu tiền/cọc xác nhận trước, sau đó gọi DonHang_Sua để nhận đơn.
      Khi hủy có tiền đã thu: hủy đơn trước, sau đó xác nhận REFUND hợp lệ.
    - Giao trọn đơn, chưa hỗ trợ giao nhiều đợt. Đơn hoàn tất không mở lại.
    - Các proc sửa dùng cùng application lock để tránh xung đột lẫn nhau.
      Các proc thêm/xóa cũ chưa tham gia khóa này và chưa đồng bộ nghiệp vụ;
      phải sửa tương ứng trước khi cho nhiều người thao tác đồng thời.
    - Có thể trả bánh đã làm cho đơn hủy về lượng bán được; chưa xử lý bánh cá nhân hóa/hỏng.
    - Phiên làm việc phải dùng múi giờ nhất quán; SYSDATETIME() là giờ SQL Server.
*/

-- NhanVien_Sua
CREATE OR ALTER PROC dbo.NhanVien_Sua
    @MaNV       tinyint,
    @HoNV       nvarchar(30),
    @TenNV      nvarchar(20),
    @DienThoai  varchar(11),
    @Email      varchar(40),
    @TenDN      nvarchar(50),
    @AnhNV      nvarchar(250) = NULL,
    @matKhauMoi nvarchar(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@MaNV)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong NhanVien.', 1;
        END;
        UPDATE dbo.NhanVien
    SET HoNV      = @HoNV,
        TenNV     = @TenNV,
        DienThoai = @DienThoai,
        Email     = @Email,
        TenDN     = @TenDN,
        AnhNV     = COALESCE(@AnhNV, AnhNV),
        MatKhau   = CASE
            WHEN @MatKhauMoi IS NULL
                 OR DATALENGTH(@matKhauMoi) = 0
                THEN MatKhau
            ELSE HASHBYTES('SHA2_512', @matKhauMoi)
        END
    WHERE MaNV = @MaNV;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Quyen_Sua
CREATE OR ALTER PROC dbo.Quyen_Sua
    @MaQuyen  tinyint,
    @TenQuyen nvarchar(50),
    @MoTa     nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.Quyen WITH (UPDLOCK,HOLDLOCK) WHERE MaQuyen=@MaQuyen)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong Quyen.', 1;
        END;
        UPDATE dbo.Quyen
    SET TenQuyen = @TenQuyen,
        MoTa     = @MoTa
    WHERE MaQuyen = @MaQuyen;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- LoaiBanh_Sua
CREATE OR ALTER PROC dbo.LoaiBanh_Sua
    @MaLoaiBanh  tinyint,
    @TenLoaiBanh nvarchar(45),
    @MoTa        nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.LoaiBanh WITH (UPDLOCK,HOLDLOCK) WHERE MaLoaiBanh=@MaLoaiBanh)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong LoaiBanh.', 1;
        END;
        UPDATE dbo.LoaiBanh
    SET TenLoaiBanh = @TenLoaiBanh,
        MoTa        = @MoTa
    WHERE MaLoaiBanh = @MaLoaiBanh;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- KhachHang_Sua
CREATE OR ALTER PROC dbo.KhachHang_Sua
    @MaKH             smallint,
    @HoKH             nvarchar(30),
    @TenKH            nvarchar(20),
    @TenDN            nvarchar(50),
    @DienThoai        varchar(11)   = NULL,
    @Email            varchar(40)   = NULL,
    @DiaChiThuongDung nvarchar(200) = NULL,
    @matKhauMoi       nvarchar(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WITH (UPDLOCK,HOLDLOCK) WHERE MaKH=@MaKH)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong KhachHang.', 1;
        END;
        UPDATE dbo.KhachHang
    SET HoKH             = @HoKH,
        TenKH            = @TenKH,
        DienThoai        = @DienThoai,
        Email            = @Email,
        DiaChiThuongDung = @DiaChiThuongDung,
        TenDN            = @TenDN,
        MatKhau          = CASE
            WHEN @matKhauMoi IS NULL
                 OR DATALENGTH(@matKhauMoi) = 0
                THEN MatKhau
            ELSE HASHBYTES('SHA2_512', @matKhauMoi)
        END
    WHERE MaKH = @MaKH;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- SanPham_Sua
CREATE OR ALTER PROC dbo.SanPham_Sua
    @MaSP             char(9),
    @MaLoaiBanh       tinyint,
    @TenSP            nvarchar(100),
    @TrangThaiHienThi varchar(12),
    @MaNVCapNhat      tinyint,
    @MoTaNgan         nvarchar(300) = NULL,
    @MoTaChiTiet      nvarchar(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK) WHERE MaSP=@MaSP)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong SanPham.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.SanPham WHERE MaSP=@MaSP AND (MaLoaiBanh=@MaLoaiBanh OR (MaLoaiBanh IS NULL AND @MaLoaiBanh IS NULL)))
        BEGIN
            ;THROW 50001, N'Không được đổi MaLoaiBanh của bản ghi này.', 1;
        END;
        IF @MaNVCapNhat IS NULL
        BEGIN
            ;THROW 50001, N'Phải có nhân viên cập nhật lấy từ tài khoản đăng nhập.', 1;
        END;
        UPDATE dbo.SanPham
    SET TenSP            = @TenSP,
        MoTaNgan         = @MoTaNgan,
        MoTaChiTiet      = @MoTaChiTiet,
        TrangThaiHienThi = @TrangThaiHienThi,
        MaNVCapNhat      = @MaNVCapNhat,
        NgayCapNhat      = SYSDATETIME()
    WHERE MaSP = @MaSP;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Đổi ảnh đại diện: bỏ cờ ảnh cũ rồi chọn ảnh mới trong cùng transaction.
CREATE OR ALTER PROC dbo.HinhAnhBanh_Sua
    @MaAnh         smallint,
    @MaSP          char(9),
    @DuongDanAnh   nvarchar(500),
    @LaAnhDaiDien  bit,
    @ThuTuHienThi  smallint
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.HinhAnhBanh WITH (UPDLOCK,HOLDLOCK) WHERE MaAnh=@MaAnh)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong HinhAnhBanh.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.HinhAnhBanh WHERE MaAnh=@MaAnh AND (MaSP=@MaSP OR (MaSP IS NULL AND @MaSP IS NULL)))
        BEGIN
            ;THROW 50001, N'Không được đổi MaSP của bản ghi này.', 1;
        END;

        -- Khóa mẫu bánh để việc chuyển ảnh đại diện được xử lý cùng nhau.
        DECLARE @MaSPKhoa char(9);
        SELECT @MaSPKhoa=MaSP FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK) WHERE MaSP=@MaSP;
        IF @LaAnhDaiDien=1
            UPDATE dbo.HinhAnhBanh SET LaAnhDaiDien=0
            WHERE MaSP=@MaSP AND MaAnh<>@MaAnh AND LaAnhDaiDien=1;
        UPDATE dbo.HinhAnhBanh
        SET DuongDanAnh=@DuongDanAnh, LaAnhDaiDien=@LaAnhDaiDien, ThuTuHienThi=@ThuTuHienThi
        WHERE MaAnh=@MaAnh;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- BienTheBanh_Sua
CREATE OR ALTER PROC dbo.BienTheBanh_Sua
    @MaBienThe            smallint,
    @MaSP                 char(9),
    @TenBienThe           nvarchar(50),
    @GiaBan               decimal(12,2),
    @SoLuongThucTe        int,
    @SoNgayChuanBiDuKien  smallint,
    @TrangThaiBan         bit
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.BienTheBanh WITH (UPDLOCK,HOLDLOCK) WHERE MaBienThe=@MaBienThe)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong BienTheBanh.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.BienTheBanh WHERE MaBienThe=@MaBienThe AND (MaSP=@MaSP OR (MaSP IS NULL AND @MaSP IS NULL)))
        BEGIN
            ;THROW 50001, N'Không được đổi MaSP của bản ghi này.', 1;
        END;

        IF @SoLuongThucTe < (SELECT SoLuongDaGiu FROM dbo.BienTheBanh WHERE MaBienThe=@MaBienThe)
        BEGIN
            ;THROW 50001,N'Tồn thực tế không được nhỏ hơn số bánh đang giữ cho đơn.',1;
        END;
        UPDATE dbo.BienTheBanh
    SET TenBienThe          = @TenBienThe,
        GiaBan              = @GiaBan,
        SoLuongThucTe       = @SoLuongThucTe,
        SoNgayChuanBiDuKien = @SoNgayChuanBiDuKien,
        TrangThaiBan        = @TrangThaiBan
    WHERE MaBienThe = @MaBienThe;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Tự tính tổng tiền; xác nhận/hoàn tất/hủy chỉ tác động kho một lần khi đổi trạng thái.
CREATE OR ALTER PROC dbo.DonHang_Sua
    @MaDonHang char(11),
    @TrangThaiDonHang varchar(30),
    @TenNguoiNhan nvarchar(80),
    @DienThoaiNguoiNhan varchar(11),
    @DiaChiGiaoHang nvarchar(300),
    @PhiGiaoHang decimal(12,2),
    @TienGiamGia decimal(12,2),
    @MaKH smallint=NULL,
    @MaNVXuLy tinyint=NULL,
    @MaNVXacNhan tinyint=NULL,
    @MaNVHoanTat tinyint=NULL,
    @NgayGiaoDuKien datetime2(0)=NULL,
    @TienCocYeuCau decimal(12,2)=NULL,
    @GhiChu nvarchar(1000)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.DonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong DonHang.', 1;
        END;

        DECLARE @Cu varchar(30),@KHCu smallint,@NVCu tinyint,@NVHTCu tinyint,
                @NgayDatCu datetime2(0),@NgayXNCu datetime2(0),@NgayHTCu datetime2(0),
                @CocCu decimal(12,2),@TongCu decimal(14,2),@Hang decimal(14,2),
                @PhaiTra decimal(14,2),@DaThu decimal(14,2),@Gio datetime2(0)=SYSDATETIME(),
                @DaNhan bit,@SeNhan bit;
        SELECT @Cu=TrangThaiDonHang,@KHCu=MaKH,@NVCu=MaNVXacNhan,@NVHTCu=MaNVHoanTat,
               @NgayDatCu=NgayDat,@NgayXNCu=NgayXacNhan,@NgayHTCu=NgayHoanTat,
               @CocCu=TienCocYeuCau,@TongCu=TongTienPhaiTra,
               @NgayGiaoDuKien=COALESCE(@NgayGiaoDuKien,NgayGiaoDuKien)
        FROM dbo.DonHang WHERE MaDonHang=@MaDonHang;
        IF @MaKH IS NOT NULL AND (@KHCu IS NULL OR @MaKH<>@KHCu)
        BEGIN
            ;THROW 50001,N'Không được đổi mã khách hàng của đơn.',1;
        END;
        IF @NVCu IS NOT NULL AND @MaNVXacNhan IS NOT NULL AND @MaNVXacNhan<>@NVCu
        BEGIN
            ;THROW 50001,N'Không được đổi nhân viên đã xác nhận đơn.',1;
        END;
        IF @NVHTCu IS NOT NULL AND @MaNVHoanTat IS NOT NULL AND @MaNVHoanTat<>@NVHTCu
        BEGIN
            ;THROW 50001,N'Không được đổi nhân viên đã hoàn tất đơn.',1;
        END;
        IF @TrangThaiDonHang IS NULL OR NOT
        (
            @Cu=@TrangThaiDonHang
            OR (@Cu='CHO_NHAN_VIEN_XU_LY' AND @TrangThaiDonHang IN ('CHO_KHACH_XAC_NHAN','DA_HUY'))
            OR (@Cu='CHO_KHACH_XAC_NHAN' AND @TrangThaiDonHang IN ('CHO_THANH_TOAN_COC','DA_XAC_NHAN','DA_HUY'))
            OR (@Cu='CHO_THANH_TOAN_COC' AND @TrangThaiDonHang IN ('DA_XAC_NHAN','DA_HUY'))
            OR (@Cu='DA_XAC_NHAN' AND @TrangThaiDonHang IN ('DANG_CHUAN_BI','SAN_SANG_GIAO','DA_HUY'))
            OR (@Cu='DANG_CHUAN_BI' AND @TrangThaiDonHang IN ('SAN_SANG_GIAO','DA_HUY'))
            OR (@Cu='SAN_SANG_GIAO' AND @TrangThaiDonHang IN ('DANG_GIAO','HOAN_TAT','DA_HUY'))
            OR (@Cu='DANG_GIAO' AND @TrangThaiDonHang IN ('HOAN_TAT','DA_HUY'))
        )
        BEGIN
            ;THROW 50001,N'Không được chuyển trạng thái theo hướng này hoặc mở lại đơn đã kết thúc.',1;
        END;
        SELECT @Hang=ISNULL(SUM(CONVERT(decimal(18,2),SoLuong)*DonGiaTaiThoiDiemDat),0)
        FROM dbo.ChiTietDonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang;
        SET @PhaiTra=@Hang+@PhiGiaoHang-@TienGiamGia;
        IF @PhiGiaoHang IS NULL OR @TienGiamGia IS NULL OR @PhiGiaoHang<0 OR @TienGiamGia<0 OR @PhaiTra<0
        BEGIN
            ;THROW 50001,N'Phí giao, giảm giá hoặc tổng phải trả không hợp lệ.',1;
        END;
        SELECT @DaThu=ISNULL(SUM(CASE WHEN LoaiGiaoDich='PAYMENT' THEN SoTien ELSE -SoTien END),0)
        FROM dbo.ThanhToan WITH (UPDLOCK,HOLDLOCK)
        WHERE MaDonHang=@MaDonHang AND TrangThaiThanhToan='SUCCESS';
        SET @DaNhan=CASE WHEN @Cu IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT') OR @NgayXNCu IS NOT NULL THEN 1 ELSE 0 END;
        SET @SeNhan=CASE WHEN @TrangThaiDonHang IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT') THEN 1 ELSE 0 END;
        IF @DaNhan=1 OR EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE MaDonHang=@MaDonHang AND TrangThaiThanhToan='SUCCESS')
        BEGIN
            IF @PhaiTra<>@TongCu OR (@TienCocYeuCau IS NOT NULL AND (@CocCu IS NULL OR @TienCocYeuCau<>@CocCu))
            BEGIN
                ;THROW 50001,N'Đơn đã nhận hoặc đã có tiền: không đổi tổng tiền hay mức cọc trong proc sửa thông thường.',1;
            END;
            SET @TienCocYeuCau=@CocCu;
        END;
        IF @TienCocYeuCau<0 OR @TienCocYeuCau>@PhaiTra
        BEGIN
            ;THROW 50001,N'Tiền cọc phải từ 0 đến tổng phải trả.',1;
        END;
        IF @DaThu>@PhaiTra
        BEGIN
            ;THROW 50001,N'Tổng mới nhỏ hơn số tiền đã thu.',1;
        END;
        IF @TrangThaiDonHang IN ('CHO_KHACH_XAC_NHAN','CHO_THANH_TOAN_COC','DA_XAC_NHAN')
           AND (@TienCocYeuCau IS NULL OR @NgayGiaoDuKien IS NULL)
        BEGIN
            ;THROW 50001,N'Phải thiết lập tiền cọc (có thể bằng 0) và ngày giao dự kiến.',1;
        END;
        IF @TrangThaiDonHang='CHO_THANH_TOAN_COC' AND @TienCocYeuCau<=0
        BEGIN
            ;THROW 50001,N'Đơn không yêu cầu cọc có thể chuyển sang xác nhận khi khách đồng ý.',1;
        END;
        IF @NgayGiaoDuKien IS NOT NULL AND @NgayGiaoDuKien<@NgayDatCu
        BEGIN
            ;THROW 50001,N'Ngày giao dự kiến không được trước ngày đặt.',1;
        END;

        IF EXISTS
        (
            SELECT 1 FROM dbo.BienTheBanh b
            OUTER APPLY
            (
                SELECT SUM(c.SoLuongCoSanDaGiu+c.SoLuongDaLam) AS Giu
                FROM dbo.ChiTietDonHang c JOIN dbo.DonHang d ON d.MaDonHang=c.MaDonHang
                WHERE c.MaBienThe=b.MaBienThe AND d.TrangThaiDonHang IN
                    ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
            ) t WHERE b.SoLuongDaGiu<>ISNULL(t.Giu,0)
        )
        BEGIN
            ;THROW 50001,N'Số bánh đã giữ đang lệch với chi tiết các đơn. Cần đối chiếu dữ liệu trước khi sửa.',1;
        END;

        -- Lần đầu chấp nhận: phân bổ số bánh thực sự còn nhận được ở thời điểm hiện tại.
        IF @DaNhan=0 AND @SeNhan=1
        BEGIN
            IF @TrangThaiDonHang<>'DA_XAC_NHAN' OR @MaNVXacNhan IS NULL OR @Gio<@NgayDatCu
            BEGIN
                ;THROW 50001,N'Cần nhân viên xác nhận hợp lệ và bước DA_XAC_NHAN.',1;
            END;
            IF @TienCocYeuCau IS NULL OR @DaThu<@TienCocYeuCau
            BEGIN
                ;THROW 50001,N'Chưa nhận đủ cọc yêu cầu. Xác nhận giao dịch thu tiền trước.',1;
            END;
            IF NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang)
            BEGIN
                ;THROW 50001,N'Không xác nhận đơn chưa có chi tiết.',1;
            END;
            IF EXISTS
            (
                SELECT 1 FROM dbo.ChiTietDonHang c JOIN dbo.BienTheBanh b ON b.MaBienThe=c.MaBienThe
                JOIN dbo.SanPham s ON s.MaSP=b.MaSP
                WHERE c.MaDonHang=@MaDonHang AND
                  (b.TrangThaiBan=0 OR s.TrangThaiHienThi<>'PUBLISHED'
                   OR c.SoLuongCoSanDaGiu<>0 OR c.SoLuongCanLam<>0 OR c.SoLuongDaLam<>0)
            )
            BEGIN
                ;THROW 50001,N'Có bánh ngừng bán/không hiển thị hoặc phân bổ cũ chưa hợp lệ.',1;
            END;
            DECLARE @Dong smallint,@BT smallint,@SL int,@Con int,@Giu int;
            DECLARE PhanBo CURSOR LOCAL FAST_FORWARD FOR
                SELECT STT,MaBienThe,SoLuong FROM dbo.ChiTietDonHang
                WHERE MaDonHang=@MaDonHang ORDER BY MaBienThe,STT;
            OPEN PhanBo;
            FETCH NEXT FROM PhanBo INTO @Dong,@BT,@SL;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SELECT @Con=SoLuongThucTe-SoLuongDaGiu FROM dbo.BienTheBanh WITH (UPDLOCK,HOLDLOCK) WHERE MaBienThe=@BT;
                SET @Giu=CASE WHEN @Con>=@SL THEN @SL ELSE @Con END;
                UPDATE dbo.BienTheBanh SET SoLuongDaGiu=SoLuongDaGiu+@Giu WHERE MaBienThe=@BT;
                UPDATE dbo.ChiTietDonHang
                SET SoLuongCoSanDaGiu=@Giu,SoLuongCanLam=@SL-@Giu,
                    NgayDuKienCoHang=CASE WHEN @SL>@Giu THEN COALESCE(NgayDuKienCoHang,@NgayGiaoDuKien) ELSE NgayDuKienCoHang END
                WHERE MaDonHang=@MaDonHang AND STT=@Dong;
                FETCH NEXT FROM PhanBo INTO @Dong,@BT,@SL;
            END;
            CLOSE PhanBo;
            DEALLOCATE PhanBo;
            SET @NgayXNCu=@Gio;
            SET @NVCu=@MaNVXacNhan;
        END;
        IF @TrangThaiDonHang IN ('SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT') AND
          (NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang)
           OR EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang AND SoLuongCoSanDaGiu+SoLuongDaLam<>SoLuong))
        BEGIN
            ;THROW 50001,N'Chưa đủ bánh để giao hoặc hoàn tất đơn.',1;
        END;
        IF @Cu<>'HOAN_TAT' AND @TrangThaiDonHang='HOAN_TAT'
        BEGIN
            IF @DaThu<>@PhaiTra OR @MaNVHoanTat IS NULL OR @Gio<@NgayXNCu
            BEGIN
                ;THROW 50001,N'Hoàn tất đơn cần thu đủ tiền và có nhân viên hoàn tất hợp lệ.',1;
            END;
            ;WITH G AS
            (
                SELECT MaBienThe,SUM(SoLuong) AS SL FROM dbo.ChiTietDonHang
                WHERE MaDonHang=@MaDonHang GROUP BY MaBienThe
            )
            UPDATE b SET SoLuongThucTe=b.SoLuongThucTe-g.SL,SoLuongDaGiu=b.SoLuongDaGiu-g.SL
            FROM dbo.BienTheBanh b JOIN G g ON g.MaBienThe=b.MaBienThe;
            SET @NgayHTCu=@Gio;
            SET @NVHTCu=@MaNVHoanTat;
        END;
        IF @Cu<>'DA_HUY' AND @TrangThaiDonHang='DA_HUY'
        BEGIN
            IF NULLIF(LTRIM(RTRIM(@GhiChu)),N'') IS NULL
            BEGIN
                ;THROW 50001,N'Hãy ghi rõ lý do hủy đơn.',1;
            END;
            IF @Cu IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
            BEGIN
                ;WITH G AS
                (
                    SELECT MaBienThe,SUM(SoLuongCoSanDaGiu+SoLuongDaLam) AS SL
                    FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang GROUP BY MaBienThe
                )
                UPDATE b SET SoLuongDaGiu=b.SoLuongDaGiu-g.SL
                FROM dbo.BienTheBanh b JOIN G g ON g.MaBienThe=b.MaBienThe;
            END;
            -- Giữ phân bổ lịch sử trên dòng; tổng giữ chỉ tính đơn đang hoạt động.
            -- Bánh đã làm vẫn thuộc tồn thực tế. Nếu không thể bán lại phải xử lý hao hụt riêng.
        END;
        UPDATE dbo.DonHang
        SET MaNVXuLy=COALESCE(@MaNVXuLy,MaNVXuLy),MaNVXacNhan=@NVCu,MaNVHoanTat=@NVHTCu,
            NgayXacNhan=@NgayXNCu,NgayHoanTat=@NgayHTCu,
            NgayGiaoDuKien=COALESCE(@NgayGiaoDuKien,NgayGiaoDuKien),TrangThaiDonHang=@TrangThaiDonHang,
            TenNguoiNhan=@TenNguoiNhan,DienThoaiNguoiNhan=@DienThoaiNguoiNhan,DiaChiGiaoHang=@DiaChiGiaoHang,
            PhiGiaoHang=@PhiGiaoHang,TienGiamGia=@TienGiamGia,TienCocYeuCau=@TienCocYeuCau,
            TongTienHang=@Hang,TongTienPhaiTra=@PhaiTra,GhiChu=@GhiChu
        WHERE MaDonHang=@MaDonHang;

        IF EXISTS
        (
            SELECT 1 FROM dbo.BienTheBanh b
            OUTER APPLY
            (
                SELECT SUM(c.SoLuongCoSanDaGiu+c.SoLuongDaLam) AS Giu
                FROM dbo.ChiTietDonHang c JOIN dbo.DonHang d ON d.MaDonHang=c.MaDonHang
                WHERE c.MaBienThe=b.MaBienThe AND d.TrangThaiDonHang IN
                    ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
            ) t WHERE b.SoLuongDaGiu<>ISNULL(t.Giu,0)
        )
        BEGIN
            ;THROW 50001,N'Số bánh đã giữ đang lệch với chi tiết các đơn. Cần đối chiếu dữ liệu trước khi sửa.',1;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Sửa dòng và tính lại tổng đơn; bánh mới làm được cộng vào thực tế và giữ cho đơn.
CREATE OR ALTER PROC dbo.ChiTietDonHang_Sua
    @MaDonHang            char(11),
    @STT                  smallint,
    @MaBienThe            smallint,
    @SoLuong              int,
    @DonGiaTaiThoiDiemDat decimal(12,2),
    @SoLuongCoSanDaGiu    int,
    @SoLuongCanLam        int,
    @SoLuongDaLam         int,
    @NgayDuKienCoHang     datetime2(0)  = NULL,
    @GhiChu               nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.DonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong DonHang.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang AND STT=@STT)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong ChiTietDonHang.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang AND STT=@STT AND (MaBienThe=@MaBienThe OR (MaBienThe IS NULL AND @MaBienThe IS NULL)))
        BEGIN
            ;THROW 50001, N'Không được đổi MaBienThe của bản ghi này.', 1;
        END;

        DECLARE @TrangThai varchar(30),@SLcu int,@GiaCu decimal(12,2),@GiuCu int,@CanCu int,@LamCu int,
                @Delta int,@Tong decimal(14,2),@PhaiTra decimal(14,2),@Coc decimal(12,2);
        SELECT @TrangThai=TrangThaiDonHang,@Coc=TienCocYeuCau FROM dbo.DonHang WHERE MaDonHang=@MaDonHang;
        SELECT @SLcu=SoLuong,@GiaCu=DonGiaTaiThoiDiemDat,@GiuCu=SoLuongCoSanDaGiu,@CanCu=SoLuongCanLam,@LamCu=SoLuongDaLam
        FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang AND STT=@STT;
        IF @TrangThai IN ('HOAN_TAT','DA_HUY')
        BEGIN
            ;THROW 50001,N'Không sửa chi tiết đơn đã hoàn tất hoặc hủy.',1;
        END;

        IF EXISTS
        (
            SELECT 1 FROM dbo.BienTheBanh b
            OUTER APPLY
            (
                SELECT SUM(c.SoLuongCoSanDaGiu+c.SoLuongDaLam) AS Giu
                FROM dbo.ChiTietDonHang c JOIN dbo.DonHang d ON d.MaDonHang=c.MaDonHang
                WHERE c.MaBienThe=b.MaBienThe AND d.TrangThaiDonHang IN
                    ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
            ) t WHERE b.SoLuongDaGiu<>ISNULL(t.Giu,0)
        )
        BEGIN
            ;THROW 50001,N'Số bánh đã giữ đang lệch với chi tiết các đơn. Cần đối chiếu dữ liệu trước khi sửa.',1;
        END;

        IF @TrangThai IN ('CHO_NHAN_VIEN_XU_LY','CHO_KHACH_XAC_NHAN','CHO_THANH_TOAN_COC')
        BEGIN
            IF @SoLuongCoSanDaGiu<>0 OR @SoLuongCanLam<>0 OR @SoLuongDaLam<>0
            BEGIN
                ;THROW 50001,N'Đơn chưa nhận phải để các số lượng phân bổ bằng 0.',1;
            END;
            IF EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE MaDonHang=@MaDonHang AND TrangThaiThanhToan='SUCCESS')
               AND (@SoLuong<>@SLcu OR @DonGiaTaiThoiDiemDat<>@GiaCu)
            BEGIN
                ;THROW 50001,N'Đã có giao dịch tiền thành công: không thay đổi số lượng/giá bằng Edit thông thường.',1;
            END;
        END
        ELSE
        BEGIN
            IF @SoLuong<>@SLcu OR @DonGiaTaiThoiDiemDat<>@GiaCu OR @SoLuongCoSanDaGiu<>@GiuCu OR @SoLuongCanLam<>@CanCu
            BEGIN
                ;THROW 50001,N'Đơn đã nhận: giữ nguyên số lượng, giá và phân bổ bánh; chỉ sửa tiến độ hoặc ghi chú.',1;
            END;
            IF @TrangThai IN ('SAN_SANG_GIAO','DANG_GIAO') AND @SoLuongDaLam<>@LamCu
            BEGIN
                ;THROW 50001,N'Đơn đã sẵn sàng/đang giao: không sửa số bánh đã làm.',1;
            END;
        END;
        IF @NgayDuKienCoHang IS NOT NULL AND @NgayDuKienCoHang <
           (SELECT NgayDat FROM dbo.DonHang WHERE MaDonHang=@MaDonHang)
        BEGIN
            ;THROW 50001,N'Ngày có hàng dự kiến không được trước ngày đặt.',1;
        END;
        SET @Delta=@SoLuongDaLam-@LamCu;
        -- CHECK của dòng sẽ chặn số âm, đã làm vượt cần làm, giá/số lượng không hợp lệ.
        UPDATE dbo.ChiTietDonHang
        SET SoLuong=@SoLuong,DonGiaTaiThoiDiemDat=@DonGiaTaiThoiDiemDat,
            SoLuongCoSanDaGiu=@SoLuongCoSanDaGiu,SoLuongCanLam=@SoLuongCanLam,SoLuongDaLam=@SoLuongDaLam,
            NgayDuKienCoHang=@NgayDuKienCoHang,GhiChu=@GhiChu
        WHERE MaDonHang=@MaDonHang AND STT=@STT;
        IF @Delta<>0
        BEGIN
            UPDATE dbo.BienTheBanh
            SET SoLuongThucTe=SoLuongThucTe+@Delta,SoLuongDaGiu=SoLuongDaGiu+@Delta
            WHERE MaBienThe=@MaBienThe;
            IF @TrangThai='DA_XAC_NHAN'
                UPDATE dbo.DonHang SET TrangThaiDonHang='DANG_CHUAN_BI' WHERE MaDonHang=@MaDonHang;
        END;
        SELECT @Tong=ISNULL(SUM(CONVERT(decimal(18,2),SoLuong)*DonGiaTaiThoiDiemDat),0)
        FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang;
        SELECT @PhaiTra=@Tong+PhiGiaoHang-TienGiamGia FROM dbo.DonHang WHERE MaDonHang=@MaDonHang;
        IF @PhaiTra<0 OR @Coc>@PhaiTra
        BEGIN
            ;THROW 50001,N'Tổng tiền mới không hợp lệ hoặc thấp hơn mức cọc; hãy điều chỉnh điều kiện đơn trước.',1;
        END;
        UPDATE dbo.DonHang SET TongTienHang=@Tong,TongTienPhaiTra=@PhaiTra WHERE MaDonHang=@MaDonHang;

        IF EXISTS
        (
            SELECT 1 FROM dbo.BienTheBanh b
            OUTER APPLY
            (
                SELECT SUM(c.SoLuongCoSanDaGiu+c.SoLuongDaLam) AS Giu
                FROM dbo.ChiTietDonHang c JOIN dbo.DonHang d ON d.MaDonHang=c.MaDonHang
                WHERE c.MaBienThe=b.MaBienThe AND d.TrangThaiDonHang IN
                    ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
            ) t WHERE b.SoLuongDaGiu<>ISNULL(t.Giu,0)
        )
        BEGIN
            ;THROW 50001,N'Số bánh đã giữ đang lệch với chi tiết các đơn. Cần đối chiếu dữ liệu trước khi sửa.',1;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Khoản đã xử lý chỉ sửa chứng từ/ghi chú; khoản chờ được kiểm tra trước xác nhận.
CREATE OR ALTER PROC dbo.ThanhToan_Sua
    @MaThanhToan char(11),
    @SoTien decimal(14,2),
    @PhuongThuc varchar(20),
    @TrangThaiThanhToan varchar(10),
    @MaNVXacNhan tinyint=NULL,
    @DuongDanChungTu nvarchar(500)=NULL,
    @GhiChu nvarchar(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Chạy độc lập; RAISERROR tại đây không tự rollback transaction của caller.
    IF @@TRANCOUNT <> 0
    BEGIN
        RAISERROR(N'Hãy gọi proc sửa này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @KetQuaKhoa int;
        EXEC @KetQuaKhoa = sys.sp_getapplock
            @Resource=N'DbQLTB:ProcSua:NghiepVu', @LockMode='Exclusive',
            @LockOwner='Transaction', @LockTimeout=10000;
        IF @KetQuaKhoa < 0
        BEGIN
            ;THROW 50001, N'Không lấy được khóa xử lý. Hãy thử lại.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.ThanhToan WITH (UPDLOCK,HOLDLOCK) WHERE MaThanhToan=@MaThanhToan)
        BEGIN
            ;THROW 50001, N'Không tìm thấy bản ghi cần sửa trong ThanhToan.', 1;
        END;

        DECLARE @Don char(11),@Goc char(11),@Loai varchar(10),@Cu varchar(10),@PTCu varchar(20),
                @TienCu decimal(14,2),@NgayCu datetime2(0),@NVcu tinyint,@NgayTaoCu datetime2(0),
                @Gio datetime2(0)=SYSDATETIME(),@TrangThaiDon varchar(30),@Tong decimal(14,2),
                @TienKhac decimal(14,2),@TienGoc decimal(14,2),@HoanKhac decimal(14,2);
        SELECT @Don=MaDonHang,@Goc=MaThanhToanGoc,@Loai=LoaiGiaoDich,@Cu=TrangThaiThanhToan,
               @TienCu=SoTien,@PTCu=PhuongThuc,@NgayCu=ThoiGianXacNhan,@NVcu=MaNVXacNhan,@NgayTaoCu=ThoiGianTao
        FROM dbo.ThanhToan WHERE MaThanhToan=@MaThanhToan;
        SELECT @TrangThaiDon=TrangThaiDonHang,@Tong=TongTienPhaiTra
        FROM dbo.DonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@Don;
        IF @Cu IN ('SUCCESS','REJECTED')
        BEGIN
            IF @SoTien<>@TienCu OR @PhuongThuc<>@PTCu OR @TrangThaiThanhToan<>@Cu
               OR (@MaNVXacNhan IS NOT NULL AND (@NVcu IS NULL OR @MaNVXacNhan<>@NVcu))
            BEGIN
                ;THROW 50001,N'Giao dịch đã xử lý: không sửa số tiền, phương thức, trạng thái hay người xác nhận. Thu lại/hoàn tiền dùng giao dịch riêng.',1;
            END;
            -- Chỉ được bổ sung chứng từ/ghi chú; không viết lại thời điểm xác nhận cũ.
        END
        ELSE
        BEGIN
            IF @TrangThaiThanhToan IN ('SUCCESS','REJECTED')
            BEGIN
                IF @MaNVXacNhan IS NULL OR @Gio<@NgayTaoCu
                BEGIN
                    ;THROW 50001,N'Cần nhân viên xử lý và thời điểm xác nhận không trước ngày tạo.',1;
                END;
                SET @NVcu=@MaNVXacNhan;
                SET @NgayCu=@Gio;
            END
            ELSE
            BEGIN
                SET @NVcu=NULL;
                SET @NgayCu=NULL;
            END;
            IF @TrangThaiThanhToan='SUCCESS' AND @Loai='PAYMENT'
            BEGIN
                IF @TrangThaiDon IN ('DA_HUY','HOAN_TAT')
                BEGIN
                    ;THROW 50001,N'Không xác nhận thu mới cho đơn đã hủy hoặc hoàn tất.',1;
                END;
                SELECT @TienKhac=ISNULL(SUM(CASE WHEN LoaiGiaoDich='PAYMENT' THEN SoTien ELSE -SoTien END),0)
                FROM dbo.ThanhToan WITH (UPDLOCK,HOLDLOCK)
                WHERE MaDonHang=@Don AND MaThanhToan<>@MaThanhToan AND TrangThaiThanhToan='SUCCESS';
                IF @TienKhac+@SoTien>@Tong
                BEGIN
                    ;THROW 50001,N'Tổng thu sẽ vượt số tiền phải trả của đơn.',1;
                END;
            END;
            IF @TrangThaiThanhToan='SUCCESS' AND @Loai='REFUND'
            BEGIN
                IF @TrangThaiDon<>'DA_HUY'
                BEGIN
                    ;THROW 50001,N'Quy trình hiện tại chỉ xác nhận hoàn tiền sau khi đơn đã hủy.',1;
                END;
                SELECT @TienGoc=SoTien FROM dbo.ThanhToan WITH (UPDLOCK,HOLDLOCK)
                WHERE MaThanhToan=@Goc AND MaDonHang=@Don AND LoaiGiaoDich='PAYMENT'
                    AND TrangThaiThanhToan='SUCCESS' AND ThoiGianXacNhan<=@Gio;
                IF @TienGoc IS NULL OR @Goc=@MaThanhToan
                BEGIN
                    ;THROW 50001,N'Khoản hoàn phải tham chiếu khoản thu thành công của cùng đơn.',1;
                END;
                SELECT @HoanKhac=ISNULL(SUM(SoTien),0) FROM dbo.ThanhToan WITH (UPDLOCK,HOLDLOCK)
                WHERE MaThanhToanGoc=@Goc AND MaThanhToan<>@MaThanhToan
                  AND LoaiGiaoDich='REFUND' AND TrangThaiThanhToan='SUCCESS';
                IF @HoanKhac+@SoTien>@TienGoc
                BEGIN
                    ;THROW 50001,N'Tổng hoàn vượt số tiền của khoản thu gốc.',1;
                END;
            END;
        END;
        UPDATE dbo.ThanhToan
        SET SoTien=@SoTien,PhuongThuc=@PhuongThuc,TrangThaiThanhToan=@TrangThaiThanhToan,
            MaNVXacNhan=@NVcu,ThoiGianXacNhan=@NgayCu,
            DuongDanChungTu=COALESCE(@DuongDanChungTu,DuongDanChungTu),GhiChu=@GhiChu
        WHERE MaThanhToan=@MaThanhToan;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
