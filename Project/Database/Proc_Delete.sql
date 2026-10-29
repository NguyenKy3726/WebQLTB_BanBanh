USE DbQLTB;
GO

/*
    11 PROC XÓA THEO CÁCH QLBH ĐƠN GIẢN.
    Chỉ kiểm tra tham chiếu FK trước DELETE; không tự xóa dây chuyền.
    Thứ tự bên dưới là một thứ tự hợp lệ khi cần xóa dữ liệu liên quan.
    Không bắt buộc xóa mọi bảng: chỉ xử lý những bản ghi đang tham chiếu bản ghi cần xóa.
    ThanhToan có FK tự tham chiếu: xóa khoản REFUND trước khoản PAYMENT gốc.
    Chi tiết dùng khóa ghép mã phiếu/đơn + STT, phân quyền dùng MaQuyen + MaNV.

    Giới hạn của bản đơn giản theo yêu cầu:
    - Đơn/chi tiết chỉ xóa theo trạng thái cho phép; giao dịch thành công được giữ lịch sử.
    - Chi tiết đơn bị xóa sẽ tính lại tổng đơn.
    - Xóa bản ghi ảnh không xóa file ảnh trên ổ đĩa.
    - Không có dòng khớp khóa thì DELETE ảnh hưởng 0 dòng.
    Chạy file này chỉ tạo/cập nhật proc, không tự gọi EXEC hay xóa dữ liệu.
*/

-- 1. ThanhToan
CREATE OR ALTER PROC dbo.ThanhToan_Xoa
    @MaThanhToan char(11)
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT<>0
    BEGIN
        RAISERROR(N'Hãy gọi proc này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @K int;
        EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @K<0
        BEGIN
            ;THROW 50001,N'Không lấy được khóa xử lý.',1;
        END;
        IF EXISTS(SELECT 1 FROM dbo.ThanhToan WHERE MaThanhToanGoc=@MaThanhToan)
        BEGIN
            ;THROW 50001,N'Không thể xóa giao dịch đang được khoản hoàn tham chiếu.',1;
        END;
        IF EXISTS(SELECT 1 FROM dbo.ThanhToan WHERE MaThanhToan=@MaThanhToan AND TrangThaiThanhToan='SUCCESS')
        BEGIN
            ;THROW 50001,N'Không xóa giao dịch thành công; hãy giữ lịch sử thu/hoàn tiền.',1;
        END;
        DELETE FROM dbo.ThanhToan WHERE MaThanhToan=@MaThanhToan;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- 2. ChiTietDonHang
CREATE OR ALTER PROC dbo.ChiTietDonHang_Xoa
    @MaDonHang char(11),
    @STT smallint
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT<>0
    BEGIN
        RAISERROR(N'Hãy gọi proc này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @K int;
        EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @K<0
        BEGIN
            ;THROW 50001,N'Không lấy được khóa xử lý.',1;
        END;
        DECLARE @TrangThai varchar(30),@Phi decimal(12,2),@Giam decimal(12,2),@Coc decimal(12,2),@Hang decimal(14,2),@PhaiTra decimal(14,2);
        SELECT @TrangThai=TrangThaiDonHang,@Phi=PhiGiaoHang,@Giam=TienGiamGia,@Coc=ISNULL(TienCocYeuCau,0)
        FROM dbo.DonHang WITH(UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang;
        IF @TrangThai IS NULL BEGIN ;THROW 50001,N'Không tìm thấy đơn hàng.',1; END;
        IF @TrangThai NOT IN ('CHO_NHAN_VIEN_XU_LY','CHO_KHACH_XAC_NHAN','CHO_THANH_TOAN_COC')
        BEGIN ;THROW 50001,N'Không xóa chi tiết đơn đã được chấp nhận; hãy hủy đơn theo quy trình.',1; END;
        IF EXISTS(SELECT 1 FROM dbo.ThanhToan WITH(UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang AND TrangThaiThanhToan='SUCCESS')
        BEGIN ;THROW 50001,N'Đơn đã có tiền thành công; không xóa dòng làm thay đổi tổng tiền.',1; END;
        DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang AND STT=@STT;
        IF @@ROWCOUNT=0 BEGIN ;THROW 50001,N'Không tìm thấy dòng chi tiết đơn hàng.',1; END;
        SELECT @Hang=ISNULL(SUM(CONVERT(decimal(18,2),SoLuong)*DonGiaTaiThoiDiemDat),0) FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang;
        SET @PhaiTra=@Hang+@Phi-@Giam;
        IF @PhaiTra<0 OR @Coc>@PhaiTra BEGIN ;THROW 50001,N'Tổng đơn mới không hợp lệ hoặc thấp hơn tiền cọc.',1; END;
        UPDATE dbo.DonHang SET TongTienHang=@Hang,TongTienPhaiTra=@PhaiTra WHERE MaDonHang=@MaDonHang;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- 3. HinhAnhBanh
CREATE OR ALTER PROC dbo.HinhAnhBanh_Xoa
    @MaAnh smallint
AS
BEGIN
    DELETE FROM dbo.HinhAnhBanh
    WHERE MaAnh = @MaAnh;
END;
GO

-- 4. PhanQuyen
CREATE OR ALTER PROC dbo.PhanQuyen_Xoa
    @MaQuyen tinyint,
    @MaNV tinyint
AS
BEGIN
    DELETE FROM dbo.PhanQuyen
    WHERE MaQuyen = @MaQuyen AND MaNV = @MaNV;
END;
GO

-- 5. DonHang
CREATE OR ALTER PROC dbo.DonHang_Xoa
    @MaDonHang char(11)
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT<>0
    BEGIN
        RAISERROR(N'Hãy gọi proc này ngoài transaction đang mở của caller.',16,1);
        RETURN;
    END;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @K int;
        EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @K<0
        BEGIN
            ;THROW 50001,N'Không lấy được khóa xử lý.',1;
        END;
        IF EXISTS(SELECT 1 FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang)
        BEGIN ;THROW 50001,N'Đơn còn chi tiết; hãy xóa các dòng chi tiết trước.',1; END;
        IF EXISTS(SELECT 1 FROM dbo.ThanhToan WHERE MaDonHang=@MaDonHang)
        BEGIN ;THROW 50001,N'Đơn còn lịch sử thanh toán hoặc hoàn tiền.',1; END;
        IF EXISTS(SELECT 1 FROM dbo.DonHang WHERE MaDonHang=@MaDonHang AND TrangThaiDonHang<>'CHO_NHAN_VIEN_XU_LY')
        BEGIN ;THROW 50001,N'Chỉ xóa đơn nháp chưa được xử lý; các đơn khác cần giữ lịch sử.',1; END;
        DELETE FROM dbo.DonHang WHERE MaDonHang=@MaDonHang;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- 6. BienTheBanh
CREATE OR ALTER PROC dbo.BienTheBanh_Xoa
    @MaBienThe smallint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE MaBienThe = @MaBienThe)
       OR EXISTS (SELECT 1 FROM dbo.BienTheBanh WHERE MaBienThe=@MaBienThe AND (SoLuongThucTe>0 OR SoLuongDaGiu>0))
    BEGIN
        RAISERROR(N'Không thể xóa biến thể bánh vì đã được sử dụng trong chi tiết đơn hàng.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.BienTheBanh
    WHERE MaBienThe = @MaBienThe;
END;
GO

-- 7. SanPham
CREATE OR ALTER PROC dbo.SanPham_Xoa
    @MaSP char(9)
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.HinhAnhBanh WHERE MaSP = @MaSP)
    BEGIN
        RAISERROR(N'Không thể xóa sản phẩm vì còn hình ảnh bánh. Hãy xóa các hình ảnh trước.', 16, 1);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.BienTheBanh WHERE MaSP = @MaSP)
    BEGIN
        RAISERROR(N'Không thể xóa sản phẩm vì còn biến thể bánh. Hãy xóa các biến thể trước.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.SanPham
    WHERE MaSP = @MaSP;
END;
GO

-- 8. KhachHang
CREATE OR ALTER PROC dbo.KhachHang_Xoa
    @MaKH smallint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.DonHang WHERE MaKH = @MaKH)
    BEGIN
        RAISERROR(N'Không thể xóa khách hàng vì còn đơn hàng của khách hàng này.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.KhachHang
    WHERE MaKH = @MaKH;
END;
GO

-- 9. NhanVien
CREATE OR ALTER PROC dbo.NhanVien_Xoa
    @MaNV tinyint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PhanQuyen WHERE MaNV = @MaNV)
    BEGIN
        RAISERROR(N'Không thể xóa nhân viên vì còn phân quyền. Hãy xóa các phân quyền của nhân viên trước.', 16, 1);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.SanPham WHERE MaNVTao = @MaNV OR MaNVCapNhat = @MaNV)
    BEGIN
        RAISERROR(N'Không thể xóa nhân viên vì đang được ghi nhận là người tạo hoặc cập nhật sản phẩm.', 16, 1);
        RETURN;
    END;


    IF EXISTS (SELECT 1 FROM dbo.DonHang WHERE MaNVXuLy = @MaNV OR MaNVXacNhan = @MaNV OR MaNVHoanTat = @MaNV)
    BEGIN
        RAISERROR(N'Không thể xóa nhân viên vì còn đơn hàng do nhân viên xử lý, xác nhận hoặc hoàn tất.', 16, 1);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE MaNVXacNhan = @MaNV)
    BEGIN
        RAISERROR(N'Không thể xóa nhân viên vì còn giao dịch thanh toán hoặc hoàn tiền do nhân viên xác nhận.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.NhanVien
    WHERE MaNV = @MaNV;
END;
GO

-- 10. Quyen
CREATE OR ALTER PROC dbo.Quyen_Xoa
    @MaQuyen tinyint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PhanQuyen WHERE MaQuyen = @MaQuyen)
    BEGIN
        RAISERROR(N'Không thể xóa quyền vì quyền này đang được cấp cho nhân viên.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.Quyen
    WHERE MaQuyen = @MaQuyen;
END;
GO

-- 11. LoaiBanh
CREATE OR ALTER PROC dbo.LoaiBanh_Xoa
    @MaLoaiBanh tinyint
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.SanPham WHERE MaLoaiBanh = @MaLoaiBanh)
    BEGIN
        RAISERROR(N'Không thể xóa loại bánh vì còn sản phẩm thuộc loại bánh này.', 16, 1);
        RETURN;
    END;

    DELETE FROM dbo.LoaiBanh
    WHERE MaLoaiBanh = @MaLoaiBanh;
END;
GO
