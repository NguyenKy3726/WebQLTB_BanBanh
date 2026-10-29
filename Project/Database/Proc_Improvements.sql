/*
  Bổ sung cho luồng bán bánh và phân quyền.
  Chạy sau Table.sql và các procedure nền.
  Script này giữ kiểm tra tài khoản, quyền và luồng bán hàng; không còn nghiệp vụ kho nguyên liệu.
*/
USE DbQLTB;
GO
CREATE OR ALTER TRIGGER dbo.TR_NhanVien_TenDN_DungChung
ON dbo.NhanVien
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource=N'DbQLTB:TaiKhoan:TenDN', @LockMode='Exclusive',
        @LockOwner='Transaction', @LockTimeout=10000;
    IF @LockResult < 0 THROW 51001, N'Không lấy được khóa kiểm tra tài khoản. Hãy thử lại.', 1;
    IF EXISTS
    (
        SELECT 1 FROM inserted i JOIN dbo.KhachHang k
          ON UPPER(LTRIM(RTRIM(i.TenDN))) = UPPER(LTRIM(RTRIM(k.TenDN)))
    ) THROW 51002, N'Tên đăng nhập đã được một tài khoản khác sử dụng.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_KhachHang_TenDN_DungChung
ON dbo.KhachHang
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource=N'DbQLTB:TaiKhoan:TenDN', @LockMode='Exclusive',
        @LockOwner='Transaction', @LockTimeout=10000;
    IF @LockResult < 0 THROW 51001, N'Không lấy được khóa kiểm tra tài khoản. Hãy thử lại.', 1;
    IF EXISTS
    (
        SELECT 1 FROM inserted i JOIN dbo.NhanVien n
          ON UPPER(LTRIM(RTRIM(i.TenDN))) = UPPER(LTRIM(RTRIM(n.TenDN)))
    ) THROW 51002, N'Tên đăng nhập đã được một tài khoản khác sử dụng.', 1;
END;
GO

/* Thêm dòng đơn, khóa biến thể, lấy giá máy chủ và cập nhật tổng tiền cùng giao dịch. */
CREATE OR ALTER PROC dbo.ChiTietDonHang_Them
    @MaDonHang char(11), @MaBienThe smallint, @SoLuong int,
    @DonGiaTaiThoiDiemDat decimal(12,2)=NULL, @NgayDuKienCoHang datetime2(0)=NULL, @GhiChu nvarchar(500)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
      BEGIN TRAN;
      DECLARE @K int,@TrangThai varchar(30),@Gia decimal(12,2),@STT smallint,@Tong decimal(14,2),@Tra decimal(14,2),@Coc decimal(12,2);
      EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
      IF @K<0 THROW 51020,N'Không lấy được khóa đơn hàng. Hãy thử lại.',1;
      SELECT @TrangThai=TrangThaiDonHang,@Coc=ISNULL(TienCocYeuCau,0) FROM dbo.DonHang WITH(UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang;
      IF @TrangThai IS NULL THROW 51021,N'Không tìm thấy đơn hàng.',1;
      IF @TrangThai NOT IN ('CHO_NHAN_VIEN_XU_LY','CHO_KHACH_XAC_NHAN','CHO_THANH_TOAN_COC') THROW 51022,N'Đơn đã được xác nhận hoặc kết thúc, không thể thêm bánh.',1;
      IF EXISTS(SELECT 1 FROM dbo.ThanhToan WHERE MaDonHang=@MaDonHang AND TrangThaiThanhToan='SUCCESS') THROW 51023,N'Đơn đã có giao dịch thành công, không thể đổi nội dung.',1;
      IF @SoLuong<=0 THROW 51024,N'Số lượng phải lớn hơn 0.',1;
      SELECT @Gia=GiaBan FROM dbo.BienTheBanh WITH(UPDLOCK,HOLDLOCK) WHERE MaBienThe=@MaBienThe AND TrangThaiBan=1;
      IF @Gia IS NULL THROW 51025,N'Biến thể không tồn tại hoặc đã ngừng bán.',1;
      IF EXISTS(SELECT 1 FROM dbo.BienTheBanh b JOIN dbo.SanPham s ON s.MaSP=b.MaSP WHERE b.MaBienThe=@MaBienThe AND s.TrangThaiHienThi<>'PUBLISHED') THROW 51026,N'Sản phẩm hiện chưa được công bố bán.',1;
      IF @DonGiaTaiThoiDiemDat IS NOT NULL AND @DonGiaTaiThoiDiemDat<>@Gia THROW 51027,N'Đơn giá phải bằng giá bán hiện hành.',1;
      SELECT @STT=ISNULL(MAX(STT),0)+1 FROM dbo.ChiTietDonHang WITH(UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang;
      INSERT dbo.ChiTietDonHang(MaDonHang,STT,MaBienThe,SoLuong,DonGiaTaiThoiDiemDat,NgayDuKienCoHang,GhiChu) VALUES(@MaDonHang,@STT,@MaBienThe,@SoLuong,@Gia,@NgayDuKienCoHang,@GhiChu);
      SELECT @Tong=ISNULL(SUM(CONVERT(decimal(18,2),SoLuong)*DonGiaTaiThoiDiemDat),0) FROM dbo.ChiTietDonHang WHERE MaDonHang=@MaDonHang;
      SELECT @Tra=@Tong+PhiGiaoHang-TienGiamGia FROM dbo.DonHang WHERE MaDonHang=@MaDonHang;
      IF @Tra<@Coc THROW 51028,N'Tổng đơn mới thấp hơn tiền cọc đã yêu cầu.',1;
      UPDATE dbo.DonHang SET TongTienHang=@Tong,TongTienPhaiTra=@Tra WHERE MaDonHang=@MaDonHang;
      COMMIT;
    END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO

/* Tạo đơn website và các dòng bánh nguyên tử từ giỏ JSON. */
CREATE OR ALTER PROC dbo.DonHang_WebDat
 @MaKH smallint, @TenNguoiNhan nvarchar(80), @DienThoaiNguoiNhan varchar(11),
 @DiaChiGiaoHang nvarchar(300), @GioHangJson nvarchar(max), @GhiChu nvarchar(1000)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@GioHangJson)<>1 THROW 51030,N'Giỏ hàng không hợp lệ.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @K int,@Gio datetime2(0)=SYSDATETIME(),@Ma char(11),@STT int=1,@Tong decimal(14,2);
  EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @K<0 THROW 51031,N'Hệ thống đang xử lý giao dịch khác. Hãy thử lại.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.KhachHang WHERE MaKH=@MaKH) THROW 51032,N'Không tìm thấy tài khoản khách hàng.',1;
  DECLARE @GioHang TABLE(MaBienThe smallint PRIMARY KEY,SoLuong int NOT NULL);
  INSERT @GioHang SELECT MaBienThe,SUM(SoLuong) FROM OPENJSON(@GioHangJson) WITH(MaBienThe smallint '$.maBienThe',SoLuong int '$.soLuong') GROUP BY MaBienThe;
  IF NOT EXISTS(SELECT 1 FROM @GioHang) THROW 51033,N'Giỏ hàng đang trống.',1;
  IF EXISTS(SELECT 1 FROM @GioHang WHERE SoLuong<=0) THROW 51034,N'Số lượng đặt phải lớn hơn 0.',1;
  IF EXISTS(SELECT 1 FROM @GioHang g WHERE NOT EXISTS(SELECT 1 FROM dbo.BienTheBanh b JOIN dbo.SanPham s ON s.MaSP=b.MaSP WHERE b.MaBienThe=g.MaBienThe AND b.TrangThaiBan=1 AND s.TrangThaiHienThi='PUBLISHED')) THROW 51035,N'Có bánh đã ngừng bán hoặc không còn hiển thị.',1;
  SELECT @Tong=SUM(CONVERT(decimal(18,2),g.SoLuong)*b.GiaBan) FROM @GioHang g JOIN dbo.BienTheBanh b ON b.MaBienThe=g.MaBienThe;
  DECLARE @Prefix char(8)='DH'+CONVERT(char(6),@Gio,12),@Seq int=1;
  WHILE @Seq<=999 AND EXISTS(SELECT 1 FROM dbo.DonHang WHERE MaDonHang=@Prefix+RIGHT('000'+CONVERT(varchar(3),@Seq),3)) SET @Seq+=1;
  IF @Seq>999 THROW 51036,N'Đã hết số đơn hàng trong ngày.',1;
  SET @Ma=@Prefix+RIGHT('000'+CONVERT(varchar(3),@Seq),3);
  INSERT dbo.DonHang(MaDonHang,MaKH,NgayDat,TrangThaiDonHang,TenNguoiNhan,DienThoaiNguoiNhan,DiaChiGiaoHang,PhiGiaoHang,TienGiamGia,TongTienHang,TongTienPhaiTra,GhiChu)
  VALUES(@Ma,@MaKH,@Gio,'CHO_NHAN_VIEN_XU_LY',@TenNguoiNhan,@DienThoaiNguoiNhan,@DiaChiGiaoHang,0,0,@Tong,@Tong,@GhiChu);
  INSERT dbo.ChiTietDonHang(MaDonHang,STT,MaBienThe,SoLuong,DonGiaTaiThoiDiemDat)
  SELECT @Ma,ROW_NUMBER() OVER(ORDER BY g.MaBienThe),g.MaBienThe,g.SoLuong,b.GiaBan FROM @GioHang g JOIN dbo.BienTheBanh b ON b.MaBienThe=g.MaBienThe;
  INSERT dbo.LichSuTrangThaiDonHang
      (MaDonHang,TrangThaiCu,TrangThaiMoi,NguoiThucHien,MaNVThucHien,ThoiDiem,LyDo)
  VALUES(@Ma,NULL,'CHO_NHAN_VIEN_XU_LY','KHACH_HANG',NULL,@Gio,N'Khách gửi đơn hàng qua website.');
  COMMIT;
  SELECT @Ma;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO

/* Vai trò theo chức năng. Chạy migration một lần trước khi cấp quyền nhân viên. */
IF NOT EXISTS(SELECT 1 FROM dbo.Quyen WHERE TenQuyen='ADMIN')
 INSERT dbo.Quyen(MaQuyen,TenQuyen,MoTa) SELECT ISNULL(MAX(MaQuyen),0)+1,'ADMIN',N'Quản trị toàn hệ thống' FROM dbo.Quyen;
IF NOT EXISTS(SELECT 1 FROM dbo.Quyen WHERE TenQuyen='SALES')
 INSERT dbo.Quyen(MaQuyen,TenQuyen,MoTa) SELECT ISNULL(MAX(MaQuyen),0)+1,'SALES',N'Xử lý khách hàng, đơn hàng và thanh toán' FROM dbo.Quyen;
GO
CREATE OR ALTER PROC dbo.NhanVien_CoQuyen @MaNV tinyint,@TenQuyen nvarchar(50)
AS
BEGIN
 SET NOCOUNT ON;
 SELECT CONVERT(int,CASE WHEN EXISTS(
   SELECT 1 FROM dbo.PhanQuyen pq JOIN dbo.Quyen q ON q.MaQuyen=pq.MaQuyen
   WHERE pq.MaNV=@MaNV AND q.TenQuyen IN ('ADMIN',@TenQuyen)
 ) THEN 1 ELSE 0 END);
END;
GO
CREATE OR ALTER PROC dbo.NhanVien_QuyenDanhSach @MaNV tinyint
AS
BEGIN
 SET NOCOUNT ON;
 SELECT q.TenQuyen FROM dbo.PhanQuyen pq JOIN dbo.Quyen q ON q.MaQuyen=pq.MaQuyen WHERE pq.MaNV=@MaNV;
END;
GO
