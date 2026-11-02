/* Database đã có: chạy sau Migration_QuyTrinhTrangThaiDonHang.sql.
   Cài lại DonHang_Sua và thêm DonHang_KhachXacNhan. Có thể chạy lại.
   Database mới: Proc_Edit.sql đã bao gồm cả hai procedure này. */
USE DbQLTB;
GO

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
    @GhiChu nvarchar(1000)=NULL,
    @TrangThaiMongDoi varchar(30)=NULL,
    @MaNVThucHien tinyint=NULL,
    @LyDoHuy nvarchar(500)=NULL,
    @MaKHThucHien smallint=NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Hỗ trợ transaction lồng để proc chuyển trạng thái ghi lịch sử cùng lần cập nhật.
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
        IF @TrangThaiMongDoi IS NOT NULL AND @Cu<>@TrangThaiMongDoi
        BEGIN
            ;THROW 50001,N'Trạng thái đơn đã thay đổi. Hãy tải lại trang trước khi thao tác.',1;
        END;
        IF @Cu IN ('HOAN_TAT','DA_HUY')
        BEGIN
            ;THROW 50001,N'Đơn đã hoàn tất hoặc đã hủy nên không thể tiếp tục chỉnh sửa.',1;
        END;
        IF @Cu<>@TrangThaiDonHang
        BEGIN
            -- Khách chỉ được đồng ý báo giá của chính đơn mình, không được dùng đường này để hủy/giao/hoàn tất.
            IF @MaKHThucHien IS NOT NULL AND
               (@KHCu IS NULL OR @MaKHThucHien<>@KHCu OR @Cu<>'CHO_KHACH_XAC_NHAN'
                OR @TrangThaiDonHang NOT IN ('CHO_THANH_TOAN_COC','DA_XAC_NHAN') OR @MaNVThucHien IS NOT NULL)
                THROW 50001,N'Khách hàng không có quyền thực hiện thao tác này.',1;
            IF @MaKHThucHien IS NULL AND
               (@MaNVThucHien IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.NhanVien WHERE MaNV=@MaNVThucHien))
            BEGIN
                ;THROW 50001,N'Thiếu nhân viên thực hiện hợp lệ cho thao tác chuyển trạng thái.',1;
            END;
            IF @TrangThaiDonHang='DA_HUY' AND NULLIF(LTRIM(RTRIM(@LyDoHuy)),N'') IS NULL
            BEGIN
                ;THROW 50001,N'Hãy nhập lý do hủy đơn.',1;
            END;
        END;
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
            IF NULLIF(LTRIM(RTRIM(@LyDoHuy)),N'') IS NULL
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

        IF @Cu<>@TrangThaiDonHang
        BEGIN
            INSERT dbo.LichSuTrangThaiDonHang
                (MaDonHang,TrangThaiCu,TrangThaiMoi,NguoiThucHien,MaNVThucHien,ThoiDiem,LyDo)
            VALUES
                (@MaDonHang,@Cu,@TrangThaiDonHang,
                 CASE WHEN @MaKHThucHien IS NOT NULL THEN 'KHACH_HANG' ELSE 'NHAN_VIEN' END,
                 @MaNVThucHien,@Gio,
                 CASE WHEN @TrangThaiDonHang='DA_HUY' THEN @LyDoHuy ELSE NULL END);
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

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Khách đồng ý đúng báo giá đã xem; tái dùng toàn bộ kiểm tra cọc và phân bổ bánh của DonHang_Sua.
CREATE OR ALTER PROC dbo.DonHang_KhachXacNhan
    @MaDonHang char(11),
    @MaKH smallint,
    @TongTienPhaiTra decimal(14,2),
    @PhiGiaoHang decimal(14,2),
    @TienGiamGia decimal(14,2),
    @TienCocYeuCau decimal(14,2),
    @NgayGiaoDuKien datetime2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT<>0 THROW 50001,N'Hãy xác nhận đơn ngoài transaction đang mở.',1;
    IF @MaKH IS NULL OR @TongTienPhaiTra IS NULL OR @PhiGiaoHang IS NULL
       OR @TienGiamGia IS NULL OR @TienCocYeuCau IS NULL OR @NgayGiaoDuKien IS NULL
        THROW 50001,N'Thiếu thông tin báo giá. Vui lòng tải lại đơn hàng.',1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @K int,@ChuDon smallint,@TrangThai varchar(30),@Dich varchar(30),
                @NV tinyint,@Ten nvarchar(80),@DT varchar(11),@DC nvarchar(300),
                @Phi decimal(12,2),@Giam decimal(12,2),@Coc decimal(12,2),
                @Tong decimal(14,2),@Giao datetime2(0),@GhiChu nvarchar(1000);
        EXEC @K=sys.sp_getapplock @Resource=N'DbQLTB:ProcSua:NghiepVu',
            @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @K<0 THROW 50001,N'Không lấy được khóa xử lý đơn. Vui lòng thử lại.',1;
        SELECT @ChuDon=MaKH,@TrangThai=TrangThaiDonHang,@NV=MaNVXuLy,
               @Ten=TenNguoiNhan,@DT=DienThoaiNguoiNhan,@DC=DiaChiGiaoHang,
               @Phi=PhiGiaoHang,@Giam=TienGiamGia,@Coc=TienCocYeuCau,
               @Tong=TongTienPhaiTra,@Giao=NgayGiaoDuKien,@GhiChu=GhiChu
        FROM dbo.DonHang WITH (UPDLOCK,HOLDLOCK) WHERE MaDonHang=@MaDonHang;
        IF @ChuDon IS NULL OR @ChuDon<>@MaKH
            THROW 50001,N'Không tìm thấy đơn hàng của bạn.',1;
        IF @TrangThai<>'CHO_KHACH_XAC_NHAN'
            THROW 50001,N'Đơn đã được xử lý hoặc không còn chờ bạn xác nhận. Vui lòng tải lại trang.',1;
        IF @Coc IS NULL OR @Giao IS NULL OR @Tong<>@TongTienPhaiTra OR @Phi<>@PhiGiaoHang
           OR @Giam<>@TienGiamGia OR @Coc<>@TienCocYeuCau OR @Giao<>@NgayGiaoDuKien
            THROW 50001,N'Báo giá hoặc lịch giao đã thay đổi. Vui lòng xem lại trước khi xác nhận.',1;
        IF @NV IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.NhanVien WHERE MaNV=@NV)
            THROW 50001,N'Đơn chưa có nhân viên tiếp nhận hợp lệ.',1;
        SET @Dich=CASE WHEN @Coc>0 THEN 'CHO_THANH_TOAN_COC' ELSE 'DA_XAC_NHAN' END;
        -- Không cọc: xác nhận theo báo giá nhân viên đã tiếp nhận, vẫn phân bổ bánh qua proc chung.
        -- Có cọc: chỉ ghi nhận khách đồng ý; nhân viên phải đối chiếu tiền rồi xác nhận sau.
        EXEC dbo.DonHang_Sua @MaDonHang=@MaDonHang,@TrangThaiDonHang=@Dich,
            @TenNguoiNhan=@Ten,@DienThoaiNguoiNhan=@DT,@DiaChiGiaoHang=@DC,
            @PhiGiaoHang=@Phi,@TienGiamGia=@Giam,@MaKH=@MaKH,@MaNVXuLy=@NV,
            @MaNVXacNhan=@NV,@NgayGiaoDuKien=@Giao,@TienCocYeuCau=@Coc,@GhiChu=@GhiChu,
            @TrangThaiMongDoi='CHO_KHACH_XAC_NHAN',@MaKHThucHien=@MaKH;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
