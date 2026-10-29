/* =============================================================================
   SEED DỮ LIỆU MẪU CHO DbQLTB (11 bảng)
   -----------------------------------------------------------------------------
   Phạm vi : chỉ INSERT dữ liệu mẫu và chạy truy vấn kiểm tra.
             Không CREATE lại bảng, không DROP/TRUNCATE/DELETE, không tắt FK/CHECK,
             không thêm/sửa cột, khóa hay bảng.
   Giả định: database trống. Nếu bất kỳ bảng đích nào đã có dữ liệu, script dừng
             bằng THROW và không xóa gì.
   Ngày chốt dữ liệu: 2026-10-24 18:00:00 (biến @NgayChot). Mọi sự kiện đã xảy ra
             (đặt đơn, thanh toán, đặt đơn/thanh toán...) đều không muộn hơn mốc này.
             Chỉ các ngày DỰ KIẾN (giao hàng, có hàng) mới nằm sau mốc này.
   Tính xác định: toàn bộ giá trị là hằng số, không dùng ngẫu nhiên, không dùng
             SYSDATETIME() hay NEWID(), nên chạy lại cho cùng kết quả.
   Mật khẩu : mọi tài khoản mẫu dùng mật khẩu 123456, lưu bằng
             HASHBYTES('SHA2_512', N'123456') - cùng cơ chế với bài DbQLBH
             (chuỗi NVARCHAR, không salt). Procedure đăng nhập so sánh
             MatKhau = HASHBYTES('SHA2_512', @MatKhau) với @MatKhau nvarchar(50).
             Mật khẩu này CHỈ dành cho môi trường học tập, không dùng thực tế.
   Ghi chú  : đường dẫn ảnh/chứng từ là đường dẫn MẪU, cần có file thật thì
             website mới hiển thị được; script không khẳng định file đã tồn tại.
             Số điện thoại dạng 09000000xx/028000000x và email @example.com là
             dữ liệu giả lập, không thuộc người thật.
   Trạng thái kiểm tra: script được kiểm tra tĩnh (xem phần bàn giao), CHƯA được
             thực thi trên SQL Server thật.
   ============================================================================= */
USE DbQLTB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @NgayChot      datetime2(0)   = '2026-10-24T18:00:00';
-- Mật khẩu mẫu 123456: phải giữ tiền tố N để băm UTF-16, khớp tham số nvarchar khi đăng nhập.
DECLARE @MatKhauMau    varbinary(MAX) = HASHBYTES('SHA2_512', N'123456');
DECLARE @BangCoDuLieu  nvarchar(MAX)  = N'';
DECLARE @ThongBao      nvarchar(2048);

-- Bảng tạm dạng biến để gom lỗi nghiệp vụ. Biến bảng không bị ROLLBACK xóa nên
-- khối CATCH vẫn đọc được danh sách lỗi để hiển thị trước khi THROW lại.
DECLARE @Loi TABLE
(
    Nhom     nvarchar(80)   NOT NULL,
    DoiTuong nvarchar(100)  NOT NULL,
    ChiTiet  nvarchar(1000) NOT NULL
);

-- Danh sách mã chứng từ + ngày nghiệp vụ để kiểm tra mã/ngày/số thứ tự.
DECLARE @MaCT TABLE
(
    Bang    nvarchar(20) NOT NULL,
    Ma      char(11)     NOT NULL,
    MaNgay  char(6)      NOT NULL,
    NgayGio datetime2(0) NOT NULL,
    Stt     int          NOT NULL
);

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================================
       0. KIỂM TRA ĐIỀU KIỆN TIÊN QUYẾT: 11 BẢNG ĐÍCH PHẢI TRỐNG
       Lý do: bộ seed là ảnh chụp trạng thái hoàn chỉnh cho database trống. Trộn với
       dữ liệu có sẵn sẽ làm sai các số liệu đối chiếu (tiền và số bánh giữ).
       Script không tự xóa dữ liệu; chỉ dừng và báo bảng nào đang có dữ liệu.
       ========================================================================= */
    SELECT @BangCoDuLieu = @BangCoDuLieu + CASE WHEN @BangCoDuLieu = N'' THEN N'' ELSE N', ' END + t.TenBang
    FROM
    (
        SELECT N'NhanVien' AS TenBang, (SELECT COUNT(*) FROM dbo.NhanVien) AS SoDong
        UNION ALL SELECT N'Quyen',              (SELECT COUNT(*) FROM dbo.Quyen)
        UNION ALL SELECT N'LoaiBanh',           (SELECT COUNT(*) FROM dbo.LoaiBanh)
        UNION ALL SELECT N'KhachHang',          (SELECT COUNT(*) FROM dbo.KhachHang)
        UNION ALL SELECT N'PhanQuyen',          (SELECT COUNT(*) FROM dbo.PhanQuyen)
        UNION ALL SELECT N'SanPham',            (SELECT COUNT(*) FROM dbo.SanPham)
        UNION ALL SELECT N'HinhAnhBanh',        (SELECT COUNT(*) FROM dbo.HinhAnhBanh)
        UNION ALL SELECT N'BienTheBanh',        (SELECT COUNT(*) FROM dbo.BienTheBanh)
        UNION ALL SELECT N'DonHang',            (SELECT COUNT(*) FROM dbo.DonHang)
        UNION ALL SELECT N'ChiTietDonHang',     (SELECT COUNT(*) FROM dbo.ChiTietDonHang)
        UNION ALL SELECT N'ThanhToan',          (SELECT COUNT(*) FROM dbo.ThanhToan)
    ) AS t
    WHERE t.SoDong > 0;

    SET @ThongBao = CONCAT(N'Dừng seed: các bảng đích đã có dữ liệu: ', @BangCoDuLieu,
                           N'. Script chỉ dành cho database trống và không tự xóa dữ liệu.');
    IF @BangCoDuLieu <> N'' THROW 50001, @ThongBao, 1;

    /* =========================================================================
       THỨ TỰ CHÈN (bắt buộc, theo phụ thuộc FK):
       NhanVien, Quyen, LoaiBanh, KhachHang, PhanQuyen, SanPham, HinhAnhBanh,
       BienTheBanh, DonHang, ChiTietDonHang, ThanhToan.
       ========================================================================= */

    /* -------------------------------------------------------------------------
       BẢNG 1/11: dbo.NhanVien
       Dùng làm gì: tài khoản nhân viên của tiệm (đăng nhập, người tạo bánh, người
         xử lý/xác nhận/hoàn tất đơn và người xác nhận thanh toán).
       FK cần có trước: không (bảng độc lập).
       Quy tắc: MaNV tinyint bắt đầu từ 1; không có HoTen/MaPB/PhongBan, chỉ HoNV + TenNV.
         TenDN duy nhất. MatKhau = băm SHA2_512 của N'123456' (không salt).
       Vai trò: nhân viên được phân quyền ADMIN hoặc SALES; không có quyền kho nguyên liệu.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.NhanVien (MaNV, HoNV, TenNV, DienThoai, Email, TenDN, MatKhau, AnhNV)
    VALUES
    (1, N'Nguyễn Thanh', N'Hải', '0900000001', 'admin.hai@example.com',    N'admin.hai',    @MatKhauMau, N'/images/staff/nv-01.jpg'),
    (2, N'Lê Thị Thu',   N'Hà',  '0900000002', 'banh.ha@example.com',      N'banh.ha',      @MatKhauMau, N'/images/staff/nv-02.jpg'),
    (3, N'Phạm Quốc',    N'Bảo', '0900000003', 'xuly.bao@example.com',     N'xuly.bao',     @MatKhauMau, NULL),
    (5, N'Đặng Minh',    N'Tâm', '0900000005', 'thungan.tam@example.com',  N'thungan.tam',  @MatKhauMau, N'/images/staff/nv-05.jpg');

    /* -------------------------------------------------------------------------
       BẢNG 2/11: dbo.Quyen
       Dùng làm gì: quyền quản trị và xử lý bán hàng được kiểm tra trong MVC.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.Quyen (MaQuyen, TenQuyen, MoTa)
    VALUES
    (1, N'ADMIN', N'Quản trị sản phẩm, nhân viên, quyền và toàn bộ nghiệp vụ.'),
    (2, N'SALES', N'Xử lý khách hàng, đơn hàng, tiến độ bánh và thanh toán.');

    /* -------------------------------------------------------------------------
       BẢNG 3/11: dbo.LoaiBanh
       Dùng làm gì: nhóm các mẫu bánh (SanPham.MaLoaiBanh tham chiếu bảng này).
       FK cần có trước: không.
       Quy tắc: MaLoaiBanh tinyint từ 1; TenLoaiBanh duy nhất.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.LoaiBanh (MaLoaiBanh, TenLoaiBanh, MoTa)
    VALUES
    (1, N'Bánh kem',      N'Bánh kem sinh nhật và bánh kem tiệc, đặt theo kích thước.'),
    (2, N'Bánh mousse',   N'Bánh mousse lạnh, cần thời gian làm đông nên thường phải đặt trước.'),
    (3, N'Bánh quy',      N'Bánh quy nướng đóng hộp, bảo quản được nhiều ngày.'),
    (4, N'Bánh su',       N'Bánh su nhân kem, nên dùng trong ngày.'),
    (5, N'Bánh mì ngọt',  N'Bánh mì mềm vị ngọt nhẹ, làm theo mẻ trong ngày.');

    /* -------------------------------------------------------------------------
       BẢNG 4/11: dbo.KhachHang
       Dùng làm gì: tài khoản khách đặt bánh; DonHang.MaKH tham chiếu (cột này cho phép NULL).
       FK cần có trước: không.
       Quy tắc: dùng HoKH + TenKH + DiaChiThuongDung. TenDN duy nhất. MatKhau như nhân viên.
         DiaChiThuongDung là địa chỉ HỒ SƠ HIỆN TẠI, có thể khác địa chỉ đã chụp trên đơn cũ
         (KH 1 đã chuyển nhà sau đơn DH260915001; KH 10 chưa khai báo địa chỉ hồ sơ).
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.KhachHang (MaKH, HoKH, TenKH, DienThoai, Email, DiaChiThuongDung, TenDN, MatKhau)
    VALUES
    (1,  N'Nguyễn Minh', N'Anh',    '0900000101', 'kh.minhanh@example.com',  N'Số 25 Đường Hoa Mai, Phường 5, Quận Phú Nhuận, TP. Hồ Chí Minh',   N'kh.minhanh',  @MatKhauMau),
    (2,  N'Trần Quốc',   N'Huy',    '0900000102', 'kh.quochuy@example.com',  N'Số 102 Đường Lá Xanh, Phường 7, Quận 3, TP. Hồ Chí Minh',          N'kh.quochuy',  @MatKhauMau),
    (3,  N'Lê Thị Mai',  N'Phương', '0900000103', 'kh.maiphuong@example.com',N'Số 7 Đường Cây Me, Phường 10, Quận Tân Bình, TP. Hồ Chí Minh',     N'kh.maiphuong',@MatKhauMau),
    (4,  N'Phạm Gia',    N'Bảo',    '0900000104', 'kh.giabao@example.com',   N'Số 56 Đường Sao Mai, Phường 4, Quận 8, TP. Hồ Chí Minh',            N'kh.giabao',   @MatKhauMau),
    (5,  N'Hoàng Thu',   N'Trang',  '0900000105', 'kh.thutrang@example.com', N'Số 31 Đường Hồ Sen, Phường 12, Quận Gò Vấp, TP. Hồ Chí Minh',      N'kh.thutrang', @MatKhauMau),
    (6,  N'Vũ Đức',      N'Long',   '0900000106', 'kh.duclong@example.com',  N'Số 90 Đường Cầu Mới, Phường 2, Quận Bình Thạnh, TP. Hồ Chí Minh',   N'kh.duclong',  @MatKhauMau),
    (7,  N'Đỗ Khánh',    N'Linh',   '0900000107', 'kh.khanhlinh@example.com',N'Số 14 Đường Hoa Hồng, Phường Tân Quy, Quận 7, TP. Hồ Chí Minh',    N'kh.khanhlinh',@MatKhauMau),
    (8,  N'Bùi Quang',   N'Vinh',   '0900000108', 'kh.quangvinh@example.com',N'Số 63 Đường Bến Gỗ, Phường Phú Hữu, TP. Thủ Đức, TP. Hồ Chí Minh', N'kh.quangvinh',@MatKhauMau),
    (9,  N'Ngô Bảo',     N'Châu',   '0900000109', 'kh.baochau@example.com',  N'Số 5 Đường Thảo Điền Mẫu, Phường Thảo Điền, TP. Thủ Đức, TP. Hồ Chí Minh', N'kh.baochau', @MatKhauMau),
    (10, N'Dương Tuấn',  N'Kiệt',   '0900000110', NULL,                      NULL,                                                                   N'kh.tuankiet', @MatKhauMau);

    /* -------------------------------------------------------------------------
       BẢNG 5/11: dbo.PhanQuyen
       ADMIN có toàn quyền; SALES xử lý đơn hàng và thanh toán.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.PhanQuyen (MaQuyen, MaNV)
    VALUES
    (1, 1),
    (2, 2), (2, 3), (2, 5);

    /* -------------------------------------------------------------------------
       BẢNG 6/11: dbo.SanPham
       Dùng làm gì: MỖI MẪU BÁNH CHỈ MỘT DÒNG. Giá và số lượng nằm ở BienTheBanh, ảnh ở
         HinhAnhBanh. Khách đặt trước bánh hết hàng không tạo thêm SanPham.
       FK cần có trước: LoaiBanh, NhanVien (MaNVTao, MaNVCapNhat).
       Quy tắc: MaSP = 'SP' + 7 chữ số từ SP0000001 (không chứa ngày; chú thích cũ trong DDL
         nói YYMMDD nhưng CHECK và đặc tả là SP + 7 số). TrangThaiHienThi: DRAFT (đang soạn),
         PUBLISHED (đang hiển thị), HIDDEN (tạm ẩn). MaNVCapNhat và NgayCapNhat cùng NULL
         hoặc cùng có giá trị; NgayCapNhat >= NgayTao.
       Tình huống: SP0000004 bị ẩn (hết mùa chanh dây) bởi quản trị; SP0000008 còn là bản nháp.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.SanPham (MaSP, MaLoaiBanh, TenSP, MoTaNgan, MoTaChiTiet, TrangThaiHienThi, MaNVTao, NgayTao, MaNVCapNhat, NgayCapNhat)
    VALUES
    ('SP0000001', 1, N'Bánh socola',        N'Bánh kem socola đậm vị, phủ ganache mềm.',
        N'Cốt bánh bông lan cacao xen kem socola, phủ ganache socola đen. Có hai kích thước 16 cm và 20 cm.',
        'PUBLISHED', 2, '2026-08-20T09:00:00', 2, '2026-10-01T10:00:00'),
    ('SP0000002', 1, N'Bánh kem dâu',       N'Bánh kem vani trang trí dâu tươi.',
        N'Cốt bánh bông lan vani, kem tươi và dâu tây. Làm theo đơn nên cần đặt trước.',
        'PUBLISHED', 2, '2026-08-20T09:30:00', NULL, NULL),
    ('SP0000003', 2, N'Bánh mousse xoài',   N'Mousse xoài chua ngọt, đế bánh quy bơ.',
        N'Lớp mousse xoài mịn trên đế bánh quy bơ, phủ gelee xoài. Cần thời gian làm đông.',
        'PUBLISHED', 2, '2026-08-22T10:00:00', NULL, NULL),
    ('SP0000004', 2, N'Bánh mousse chanh dây', N'Mousse chanh dây vị chua thanh.',
        N'Mousse chanh dây trên đế bông lan. Tạm ẩn vì hết mùa nguyên liệu.',
        'HIDDEN', 2, '2026-08-22T10:30:00', 1, '2026-10-10T16:00:00'),
    ('SP0000005', 3, N'Bánh quy bơ',        N'Bánh quy bơ giòn tan đóng hộp.',
        N'Bánh quy bơ nướng giòn, đóng hộp 250 g hoặc 500 g, làm sẵn để bán tại tiệm.',
        'PUBLISHED', 2, '2026-08-25T09:00:00', NULL, NULL),
    ('SP0000006', 4, N'Bánh su kem',        N'Bánh su vỏ giòn, nhân kem vani.',
        N'Vỏ su nướng giòn, bơm kem vani khi nhận đơn. Hộp 6 hoặc 12 cái.',
        'PUBLISHED', 2, '2026-08-25T09:30:00', NULL, NULL),
    ('SP0000007', 5, N'Bánh mì sữa ngọt',   N'Bánh mì mềm vị sữa, ăn sáng hoặc ăn nhẹ.',
        N'Bánh mì sữa ngọt nhẹ, nướng theo mẻ trong ngày, đóng túi 4 cái.',
        'PUBLISHED', 2, '2026-08-28T08:30:00', NULL, NULL),
    ('SP0000008', 2, N'Bánh tiramisu',      N'Bánh tiramisu vị cà phê, đang soạn nội dung.',
        N'Bản nháp: chưa chốt hình ảnh, giá và thời điểm mở bán.',
        'DRAFT', 2, '2026-10-20T14:00:00', NULL, NULL);

    /* -------------------------------------------------------------------------
       BẢNG 7/11: dbo.HinhAnhBanh
       Dùng làm gì: ảnh của từng mẫu bánh. Một mẫu có thể có nhiều ảnh.
       FK cần có trước: SanPham.
       Quy tắc: tối đa MỘT ảnh đại diện cho mỗi mẫu (index lọc UX_HinhAnhBanh_AnhDaiDien);
         ThuTuHienThi > 0. SP0000008 (bản nháp) có ảnh nhưng chưa chọn ảnh đại diện.
         Đường dẫn là đường dẫn tương đối theo website và là MẪU: cần có file ảnh thật
         ở đúng đường dẫn thì ảnh mới hiển thị.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.HinhAnhBanh (MaAnh, MaSP, DuongDanAnh, LaAnhDaiDien, ThuTuHienThi)
    VALUES
    (1,  'SP0000001', N'/images/products/banh-socola-16.jpg',          1, 1),
    (2,  'SP0000001', N'/images/products/banh-socola-20.jpg',          0, 2),
    (3,  'SP0000001', N'/images/products/banh-socola-lat-cat.jpg',     0, 3),
    (4,  'SP0000002', N'/images/products/banh-kem-dau-16.jpg',         1, 1),
    (5,  'SP0000002', N'/images/products/banh-kem-dau-20.jpg',         0, 2),
    (6,  'SP0000003', N'/images/products/banh-mousse-xoai-14.jpg',     1, 1),
    (7,  'SP0000003', N'/images/products/banh-mousse-xoai-18.jpg',     0, 2),
    (8,  'SP0000004', N'/images/products/banh-mousse-chanh-day-14.jpg',1, 1),
    (9,  'SP0000005', N'/images/products/banh-quy-bo-hop-250g.jpg',    1, 1),
    (10, 'SP0000005', N'/images/products/banh-quy-bo-hop-500g.jpg',    0, 2),
    (11, 'SP0000006', N'/images/products/banh-su-kem-hop-6.jpg',       1, 1),
    (12, 'SP0000006', N'/images/products/banh-su-kem-hop-12.jpg',      0, 2),
    (13, 'SP0000007', N'/images/products/banh-mi-sua-ngot.jpg',        1, 1),
    (14, 'SP0000008', N'/images/products/banh-tiramisu-16.jpg',        0, 1);

    /* -------------------------------------------------------------------------
       BẢNG 8/11: dbo.BienTheBanh
       Dùng làm gì: lựa chọn bán (kích thước/quy cách) của một mẫu bánh, mang giá và tồn.
       FK cần có trước: SanPham.
       Ý nghĩa số lượng (là SỐ CUỐI KỲ tại ngày chốt, không chạy lại luồng cập nhật):
         SoLuongThucTe : bánh đã làm xong đang có trong tiệm, GỒM cả bánh đã giữ cho đơn.
         SoLuongDaGiu  : phần đã dành cho đơn được chấp nhận = SUM(CoSanDaGiu + DaLam) của các
                         đơn DA_XAC_NHAN, DANG_CHUAN_BI, SAN_SANG_GIAO, DANG_GIAO.
         Bánh có thể nhận ngay = SoLuongThucTe - SoLuongDaGiu (không có cột riêng).
         Đơn HOAN_TAT/DA_HUY và đơn chưa chấp nhận KHÔNG còn giữ bánh.
       TrangThaiBan: 1 cho bán và đặt trước; 0 ngừng bán (biến thể 7 vì mẫu bị ẩn, 13 vì mẫu nháp).
       Ba trạng thái tồn: còn hàng (biến thể 1, 2, 5, 6, 8, 10, 12), hết hàng vẫn nhận đặt trước
         (biến thể 3, 4, 11: tồn 0, TrangThaiBan = 1, cần SoNgayChuanBiDuKien), ngừng bán (7, 13).
       Giá hiện hành khác giá chụp trên đơn cũ: biến thể 1 (đơn 1 mua 285.000, nay 295.000) và
         biến thể 7 (đơn 3 mua 230.000, nay 240.000).

       Số tồn đầu/đã làm/đã giao MINH HỌA để giải thích tồn cuối (schema chưa có lịch sử nhập/xuất
       bánh thành phẩm nên không thể kiểm chứng từ dữ liệu; đây chỉ là giải thích):
         Tồn cuối = Tồn đầu 01/09 + Làm sẵn cho kho + Làm theo đơn - Đã giao (đơn HOAN_TAT)
         BT | Đầu | Làm sẵn | Làm theo đơn | Đã giao | Cuối | Đang giữ | Nhận ngay
          1 |  3  |   4     |   0          |   1     |  6   |   2      |   4
          2 |  2  |   1     |   1          |   0     |  4   |   2      |   2
          3 |  0  |   0     |   0          |   0     |  0   |   0      |   0
          4 |  0  |   0     |   0          |   0     |  0   |   0      |   0
          5 |  3  |   2     |   0          |   0     |  5   |   2      |   3
          6 |  2  |   1     |   1          |   1     |  3   |   2      |   1
          7 |  0  |   0     |   2          |   2     |  0   |   0      |   0
          8 |  6  |   0     |   0          |   2     |  4   |   0      |   4
          9 |  2  |   0     |   0          |   0     |  2   |   2      |   0
         10 |  1  |   2     |   2          |   2     |  3   |   1      |   2
         11 |  0  |   0     |   0          |   0     |  0   |   0      |   0
         12 |  6  |   0     |   0          |   3     |  3   |   0      |   3
         13 |  0  |   0     |   0          |   0     |  0   |   0      |   0
         Số "đã giao" là tổng SoLuong của các đơn HOAN_TAT; đã được trừ sẵn nên KHÔNG trừ lại
         khi nhập SoLuongThucTe.
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.BienTheBanh (MaBienThe, MaSP, TenBienThe, GiaBan, SoLuongThucTe, SoLuongDaGiu, SoNgayChuanBiDuKien, TrangThaiBan)
    VALUES
    (1,  'SP0000001', N'16 cm',        295000.00, 6, 2, 2, 1),
    (2,  'SP0000001', N'20 cm',        395000.00, 4, 2, 3, 1),
    (3,  'SP0000002', N'16 cm',        320000.00, 0, 0, 2, 1),
    (4,  'SP0000002', N'20 cm',        430000.00, 0, 0, 3, 1),
    (5,  'SP0000003', N'14 cm',        250000.00, 5, 2, 2, 1),
    (6,  'SP0000003', N'18 cm',        340000.00, 3, 2, 3, 1),
    (7,  'SP0000004', N'14 cm',        240000.00, 0, 0, 2, 0),
    (8,  'SP0000005', N'Hộp 250 g',    120000.00, 4, 0, 1, 1),
    (9,  'SP0000005', N'Hộp 500 g',    220000.00, 2, 2, 1, 1),
    (10, 'SP0000006', N'Hộp 6 cái',     90000.00, 3, 1, 1, 1),
    (11, 'SP0000006', N'Hộp 12 cái',   165000.00, 0, 0, 2, 1),
    (12, 'SP0000007', N'Túi 4 cái',     60000.00, 3, 0, 1, 1),
    (13, 'SP0000008', N'16 cm',        360000.00, 0, 0, 2, 0);

    /* -------------------------------------------------------------------------
       BẢNG 9/11: dbo.DonHang
       Dùng làm gì: đầu đơn đặt bánh. Cột tiền là SỐ ĐÃ LƯU, phải khớp chi tiết:
         TongTienHang = SUM(SoLuong * DonGiaTaiThoiDiemDat) của ChiTietDonHang,
         TongTienPhaiTra = TongTienHang + PhiGiaoHang - TienGiamGia,
         TienCocYeuCau NULL (chưa quyết định) <> 0 (không yêu cầu cọc) <= TongTienPhaiTra.
       FK cần có trước: KhachHang, NhanVien (MaNVXuLy, MaNVXacNhan, MaNVHoanTat).
       Quy tắc mã: DH + YYMMDD + 001..999, ngày trong mã = ngày NgayDat, đánh số lại mỗi ngày.
       Luồng: khách gửi đơn (chưa giữ bánh) -> nhân viên báo ngày nhận và tiền cọc riêng của đơn ->
         khách đồng ý -> cọc được xác nhận (hoặc cọc = 0) -> NgayXacNhan: đơn được chấp nhận và bắt
         đầu giữ bánh -> làm bánh -> sẵn sàng giao -> đang giao -> hoàn tất (cần MaNVHoanTat,
         NgayHoanTat, đã thu đủ). DA_HUY không có NgayHoanTat.
       TenNguoiNhan/DienThoaiNguoiNhan/DiaChiGiaoHang là bản CHỤP trên đơn, không đọc lại hồ sơ
         khách nên đổi hồ sơ không làm đổi đơn cũ.

       BÁO CÁO 12 TÌNH HUỐNG ĐƠN (mô tả mua gì, phần sẵn/phần làm, ngày nhận, cọc, thanh toán, ảnh hưởng tồn):
       1 DH260915001 - KH1, HOAN_TAT. 1 bánh socola 16 cm (có sẵn, giá lúc đó 285.000, nay 295.000) +
         2 hộp su 6 cái (làm theo đơn). Hàng 465.000 + ship 30.000 = 495.000. Cọc 150.000 (chuyển khoản),
         phần còn lại 345.000 trả tiền mặt khi giao ngày 18/09. Đã giao: tồn bánh socola 16 cm giảm 1.
       2 DH260930001 - KH2, HOAN_TAT, KHÔNG yêu cầu cọc (TienCocYeuCau = 0). Khách nhận tại tiệm: 2 hộp
         quy bơ 250 g + 3 túi bánh mì sữa, toàn bộ có sẵn. Hàng 420.000, giảm 20.000, phải trả 400.000
         trả tiền mặt khi nhận ngày 01/10.
       3 DH261005001 - KH1 (mua lần 2), HOAN_TAT. 2 bánh mousse chanh dây 14 cm (làm theo đơn, giá lúc đó
         230.000, nay 240.000, mẫu hiện bị ẩn) + 1 mousse xoài 18 cm (có sẵn). Hàng 800.000, ship 40.000,
         giảm 40.000, phải trả 800.000. Cọc MỘT PHẦN 300.000, thanh toán nốt 500.000 trước khi giao ngày 08/10.
         Giao đến địa chỉ khác hồ sơ của khách.
       4 DH261008001 - KH3, DA_HUY SAU KHI ĐƯỢC CHẤP NHẬN. Đã cọc 250.000 (xác nhận 09/10), khách hủy ngày
         12/10 nên giải phóng toàn bộ bánh đã giữ (các cột phân bổ về 0) và HOÀN ĐỦ 250.000. Thực thu = 0.
       5 DH261012001 - KH4, DA_HUY TRƯỚC KHI CHẤP NHẬN. Khách chuyển cọc 200.000 nhưng giao dịch bị TỪ CHỐI
         (không khớp sao kê), khách bỏ đơn ngày 13/10. Chưa từng chấp nhận, không giữ bánh, thực thu = 0.
       6 DH261018001 - KH5, DA_XAC_NHAN. ĐƠN HỖN HỢP: 2 mousse xoài 14 cm giữ từ tồn ngay khi chấp nhận,
         1 bánh kem dâu 20 cm CẦN LÀM (dự kiến có hàng 26/10). Cọc 400.000 đã xác nhận, còn 565.000 phải trả.
         Giữ 2 bánh mousse xoài 14 cm; bánh kem dâu chưa làm nên chưa vào tồn và chưa giữ.
       7 DH261019001 - KH6, DANG_CHUAN_BI. 3 bánh socola 20 cm: giữ 1 từ tồn, làm thêm 2 (đã làm xong 1) +
         2 hộp su 12 cái (làm 2, chưa làm). Cọc 600.000 đã xác nhận, còn 905.000. Đang giữ 2 bánh socola 20 cm
         (1 từ tồn + 1 mới làm xong).
       8 DH261021001 - KH7, SAN_SANG_GIAO. 2 mousse xoài 18 cm (1 từ tồn + 1 mới làm) + 2 hộp quy bơ 500 g
         (từ tồn). Lần chuyển cọc đầu bị TỪ CHỐI, lần hai 500.000 thành công. Còn 650.000 thu khi giao.
         Đang giữ đủ cả 4 bánh.
       9 DH261022001 - KH2 (mua lần 2), DANG_GIAO. 2 bánh socola 16 cm + 1 hộp su 6 cái, toàn bộ giữ từ tồn.
         Cọc 200.000 đã xác nhận, còn 505.000 thu tiền mặt khi giao hôm nay (24/10).
      10 DH261023001 - KH8, CHO_THANH_TOAN_COC. Khách đã đồng ý, tiệm yêu cầu cọc 300.000. Khách chuyển khoản và
         gửi ảnh nhưng giao dịch còn PENDING (ảnh chưa đồng nghĩa thành công), chưa chấp nhận, chưa giữ bánh.
      11 DH261024001 - KH9, CHO_KHACH_XAC_NHAN. Nhân viên đã báo ngày nhận 27/10 và cọc 200.000, đang chờ
         khách đồng ý. Chưa thu tiền, chưa giữ bánh.
      12 DH261024002 - KH10, CHO_NHAN_VIEN_XU_LY. Đơn mới gửi sáng nay, chưa có nhân viên nhận, TienCocYeuCau = NULL
         (chưa quyết định), phí giao tạm 0 chờ nhân viên cập nhật. Chưa giữ bánh.
       Khách mua nhiều lần: KH1 (đơn 1, 3) và KH2 (đơn 2, 9).
       ------------------------------------------------------------------------- */
    INSERT INTO dbo.DonHang
        (MaDonHang, MaKH, MaNVXuLy, MaNVXacNhan, MaNVHoanTat, NgayDat, NgayXacNhan, NgayHoanTat, NgayGiaoDuKien,
         TrangThaiDonHang, TenNguoiNhan, DienThoaiNguoiNhan, DiaChiGiaoHang,
         PhiGiaoHang, TienGiamGia, TienCocYeuCau, TongTienHang, TongTienPhaiTra, GhiChu)
    VALUES
    -- Đơn 1: hoàn tất, giao đến ĐỊA CHỈ CŨ của KH1 (khác hồ sơ hiện tại ở quận Phú Nhuận).
    ('DH260915001', 1, 3, 3, 3, '2026-09-15T09:10:00', '2026-09-15T15:30:00', '2026-09-18T11:00:00', '2026-09-18T10:00:00',
        'HOAN_TAT', N'Nguyễn Minh Anh', '0900000101', N'Số 12 Hẻm 45 Đường Hoa Lan, Phường Tân Định, Quận 1, TP. Hồ Chí Minh',
        30000.00, 0.00, 150000.00, 465000.00, 495000.00,
        N'Đơn hoàn tất. Giá bánh socola 16 cm chụp lúc đặt là 285.000, hiện giá bán là 295.000.'),
    -- Đơn 2: không yêu cầu cọc, khách nhận tại tiệm nên phí giao 0.
    ('DH260930001', 2, 3, 3, 3, '2026-09-30T16:20:00', '2026-09-30T17:00:00', '2026-10-01T09:40:00', '2026-10-01T09:00:00',
        'HOAN_TAT', N'Trần Quốc Huy', '0900000102', N'Nhận tại tiệm: Số 8 Đường Bánh Ngọt, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
        0.00, 20000.00, 0.00, 420000.00, 400000.00,
        N'Không yêu cầu cọc vì đơn nhỏ, toàn bộ bánh có sẵn. Trả tiền mặt khi nhận. Giảm 20.000 khách quen.'),
    -- Đơn 3: người nhận khác khách đặt (gửi quà), địa chỉ khác hồ sơ.
    ('DH261005001', 1, 3, 3, 3, '2026-10-05T10:00:00', '2026-10-05T14:00:00', '2026-10-08T16:30:00', '2026-10-08T16:00:00',
        'HOAN_TAT', N'Nguyễn Hồng Nhung', '0900000201', N'Tầng 4, Tòa nhà An Phát, Số 60 Đường Số 2, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh',
        40000.00, 40000.00, 300000.00, 800000.00, 800000.00,
        N'Gửi quà cho đồng nghiệp. Cọc một phần 300.000, phần còn lại 500.000 chuyển khoản trước khi giao. Giá mousse chanh dây chụp lúc đặt là 230.000.'),
    -- Đơn 4: đã chấp nhận rồi mới hủy; không có NgayHoanTat.
    ('DH261008001', 3, 3, 3, NULL, '2026-10-08T11:00:00', '2026-10-09T10:00:00', NULL, '2026-10-12T10:00:00',
        'DA_HUY', N'Lê Thị Mai Phương', '0900000103', N'Số 7 Đường Cây Me, Phường 10, Quận Tân Bình, TP. Hồ Chí Minh',
        30000.00, 0.00, 250000.00, 550000.00, 580000.00,
        N'Khách hủy ngày 12/10/2026 vì đổi kế hoạch. Đã giải phóng bánh giữ và hoàn đủ 250.000 tiền cọc.'),
    -- Đơn 5: hủy trước khi chấp nhận, nên NgayXacNhan và MaNVXacNhan là NULL.
    ('DH261012001', 4, 3, NULL, NULL, '2026-10-12T19:30:00', NULL, NULL, '2026-10-16T10:00:00',
        'DA_HUY', N'Phạm Gia Bảo', '0900000104', N'Số 56 Đường Sao Mai, Phường 4, Quận 8, TP. Hồ Chí Minh',
        25000.00, 0.00, 200000.00, 485000.00, 510000.00,
        N'Giao dịch cọc bị từ chối do không khớp sao kê, khách bỏ đơn ngày 13/10/2026. Chưa từng được chấp nhận.'),
    -- Đơn 6: đơn hỗn hợp, vừa giữ bánh có sẵn vừa có bánh cần làm.
    ('DH261018001', 5, 3, 3, NULL, '2026-10-18T09:30:00', '2026-10-18T14:30:00', NULL, '2026-10-27T10:00:00',
        'DA_XAC_NHAN', N'Hoàng Thu Trang', '0900000105', N'Số 31 Đường Hồ Sen, Phường 12, Quận Gò Vấp, TP. Hồ Chí Minh',
        35000.00, 0.00, 400000.00, 930000.00, 965000.00,
        N'Đơn hỗn hợp: mousse xoài 14 cm giữ từ tồn, bánh kem dâu 20 cm cần làm, dự kiến có hàng 26/10. Còn phải trả 565.000.'),
    -- Đơn 7: đang chuẩn bị, tiến độ làm bánh mới hoàn thành một phần.
    ('DH261019001', 6, 3, 3, NULL, '2026-10-19T08:45:00', '2026-10-19T11:30:00', NULL, '2026-10-25T15:00:00',
        'DANG_CHUAN_BI', N'Vũ Đức Long', '0900000106', N'Số 90 Đường Cầu Mới, Phường 2, Quận Bình Thạnh, TP. Hồ Chí Minh',
        40000.00, 50000.00, 600000.00, 1515000.00, 1505000.00,
        N'Giảm 50.000 theo chương trình khách hàng mới. Bánh socola 20 cm làm xong 1/2 chiếc cần làm, bánh su chưa làm. Còn phải trả 905.000.'),
    -- Đơn 8: sẵn sàng giao, đã đủ bánh.
    ('DH261021001', 7, 3, 3, NULL, '2026-10-21T10:00:00', '2026-10-21T13:00:00', NULL, '2026-10-25T10:00:00',
        'SAN_SANG_GIAO', N'Đỗ Khánh Linh', '0900000107', N'Số 14 Đường Hoa Hồng, Phường Tân Quy, Quận 7, TP. Hồ Chí Minh',
        30000.00, 0.00, 500000.00, 1120000.00, 1150000.00,
        N'Giao dịch cọc lần 1 bị từ chối, lần 2 thành công 500.000. Còn 650.000 thu khi giao.'),
    -- Đơn 9: đang giao, KH2 mua lần 2.
    ('DH261022001', 2, 3, 3, NULL, '2026-10-22T09:00:00', '2026-10-22T10:45:00', NULL, '2026-10-24T16:00:00',
        'DANG_GIAO', N'Trần Quốc Huy', '0900000102', N'Số 102 Đường Lá Xanh, Phường 7, Quận 3, TP. Hồ Chí Minh',
        25000.00, 0.00, 200000.00, 680000.00, 705000.00,
        N'Đang giao chiều 24/10, thu nốt 505.000 tiền mặt khi giao.'),
    -- Đơn 10: chờ thanh toán cọc; giao dịch cọc còn PENDING nên chưa được chấp nhận.
    ('DH261023001', 8, 3, NULL, NULL, '2026-10-23T14:00:00', NULL, NULL, '2026-10-28T10:00:00',
        'CHO_THANH_TOAN_COC', N'Bùi Quang Vinh', '0900000108', N'Số 63 Đường Bến Gỗ, Phường Phú Hữu, TP. Thủ Đức, TP. Hồ Chí Minh',
        30000.00, 0.00, 300000.00, 660000.00, 690000.00,
        N'Khách đã đồng ý ngày nhận 28/10, đang chờ nhân viên đối chiếu giao dịch chuyển khoản cọc.'),
    -- Đơn 11: nhân viên đã báo ngày và cọc, chờ khách đồng ý.
    ('DH261024001', 9, 3, NULL, NULL, '2026-10-24T08:20:00', NULL, NULL, '2026-10-27T15:00:00',
        'CHO_KHACH_XAC_NHAN', N'Ngô Bảo Châu', '0900000109', N'Số 5 Đường Thảo Điền Mẫu, Phường Thảo Điền, TP. Thủ Đức, TP. Hồ Chí Minh',
        30000.00, 0.00, 200000.00, 635000.00, 665000.00,
        N'Nhân viên đã báo ngày nhận 27/10 và tiền cọc 200.000, chờ khách trả lời.'),
    -- Đơn 12: mới gửi, chưa nhân viên nào nhận (MaNVXuLy NULL), cọc chưa quyết định (NULL).
    ('DH261024002', 10, NULL, NULL, NULL, '2026-10-24T10:15:00', NULL, NULL, NULL,
        'CHO_NHAN_VIEN_XU_LY', N'Dương Tuấn Kiệt', '0900000110', N'Số 88 Đường Mới, Phường 9, Quận 5, TP. Hồ Chí Minh',
        0.00, 0.00, NULL, 650000.00, 650000.00,
        N'Đơn mới gửi từ website, chờ nhân viên liên hệ báo ngày nhận và tiền cọc.');

    /* =====================================================================
       PHẦN BỔ SUNG: CHI TIẾT ĐƠN HÀNG VÀ THANH TOÁN
       Tiếp nối BEGIN TRY / BEGIN TRANSACTION ở phần đầu file gốc.
       Tồn bánh trong phần đầu là số CUỐI KỲ: không UPDATE/trừ kho lần nữa.
       ===================================================================== */

    /* 17. Chi tiết đơn: giữ nguyên đơn giá theo thời điểm đặt.
       Các đơn hoàn tất giữ lại phân bổ lịch sử, nhưng không tính vào giữ hiện tại.
       Đơn hủy đặt các số phân bổ về 0, đúng mô tả phần đầu file.
       Đơn đang chờ giữ 0; các ngày có hàng lúc này mới là dự kiến.
       Ví dụ DH261019001: 3 bánh socola = 1 sẵn + 2 cần làm;
       đã làm 1 nên hiện giữ 2 bánh, KHÔNG phải giữ cả 3.
       Các số đã giao đã nằm trong tồn cuối kỳ; không trừ kho lại. */
    INSERT INTO dbo.ChiTietDonHang
        (MaDonHang, STT, MaBienThe, SoLuong, DonGiaTaiThoiDiemDat, SoLuongCoSanDaGiu, SoLuongCanLam, SoLuongDaLam, NgayDuKienCoHang, GhiChu)
    VALUES
    (N'DH260915001', 1, 1, 1, 285000, 1, 0, 0, NULL, N'Giá lịch sử; bánh sẵn đã giao, không còn giữ.'),
    (N'DH260915001', 2, 10, 2, 90000, 0, 2, 2, N'2026-09-18T09:00:00', N'Làm đủ 2 hộp và đã giao; tồn cuối đã phản ánh.'),
    (N'DH260930001', 1, 8, 2, 120000, 2, 0, 0, NULL, N'Bánh sẵn đã giao, đơn không yêu cầu cọc.'),
    (N'DH260930001', 2, 12, 3, 60000, 3, 0, 0, NULL, N'Ba túi 4 cái; đơn vị bán là túi, không phải từng chiếc.'),
    (N'DH261005001', 1, 7, 2, 230000, 0, 2, 2, N'2026-10-08T14:00:00', N'Giá lịch sử 230.000; đặt trước lúc mẫu còn bán, sau đó mới ẩn.'),
    (N'DH261005001', 2, 6, 1, 340000, 1, 0, 0, NULL, N'Bánh sẵn đã giao cùng đơn.'),
    (N'DH261008001', 1, 3, 1, 320000, 0, 0, 0, N'2026-10-12T09:00:00', N'Đơn hủy; các cột phân bổ được trả về 0 theo mô tả đầu đơn.'),
    (N'DH261008001', 2, 7, 1, 230000, 0, 0, 0, N'2026-10-12T09:00:00', N'Đặt trước thời điểm ẩn mẫu; đã hủy và hoàn đủ cọc.'),
    (N'DH261012001', 1, 2, 1, 395000, 0, 0, 0, N'2026-10-16T09:00:00', N'Cọc bị từ chối; chưa từng giữ bánh.'),
    (N'DH261012001', 2, 10, 1, 90000, 0, 0, 0, N'2026-10-16T09:00:00', N'Đơn hủy trước khi được chấp nhận.'),
    (N'DH261018001', 1, 5, 2, 250000, 2, 0, 0, NULL, N'Đang giữ 2 bánh mousse xoài 14 cm.'),
    (N'DH261018001', 2, 4, 1, 430000, 0, 1, 0, N'2026-10-26T16:00:00', N'Hết hàng, cần làm 1 bánh; chưa cộng tồn.'),
    (N'DH261019001', 1, 2, 3, 395000, 1, 2, 1, N'2026-10-25T12:00:00', N'Giữ 1 bánh sẵn và 1 bánh mới làm; còn 1 bánh cần làm.'),
    (N'DH261019001', 2, 11, 2, 165000, 0, 2, 0, N'2026-10-25T12:00:00', N'Cần làm 2 hộp 12 cái, chưa làm xong hộp nào.'),
    (N'DH261021001', 1, 6, 2, 340000, 1, 1, 1, N'2026-10-24T10:00:00', N'Đã đủ 2 bánh, giữ cho đơn sẵn sàng giao.'),
    (N'DH261021001', 2, 9, 2, 220000, 2, 0, 0, NULL, N'Giữ 2 hộp 500 g; tồn còn 2 nhưng lượng nhận ngay là 0.'),
    (N'DH261022001', 1, 1, 2, 295000, 2, 0, 0, NULL, N'Đang giao; chưa hoàn tất nên vẫn được tính trong phần đã giữ.'),
    (N'DH261022001', 2, 10, 1, 90000, 1, 0, 0, NULL, N'Đang giữ 1 hộp 6 cái cho đơn đang giao.'),
    (N'DH261023001', 1, 3, 1, 320000, 0, 0, 0, N'2026-10-28T09:00:00', N'Chờ xác nhận cọc, mới dự kiến, chưa giữ bánh.'),
    (N'DH261023001', 2, 6, 1, 340000, 0, 0, 0, N'2026-10-28T09:00:00', N'Chờ xác nhận cọc, chưa phân bổ bánh.'),
    (N'DH261024001', 1, 2, 1, 395000, 0, 0, 0, N'2026-10-27T12:00:00', N'Chờ khách đồng ý thời gian và cọc.'),
    (N'DH261024001', 2, 8, 2, 120000, 0, 0, 0, N'2026-10-27T12:00:00', N'Chưa giữ bánh dù kho đang có hàng.'),
    (N'DH261024002', 1, 3, 1, 320000, 0, 0, 0, NULL, N'Đơn mới; chưa báo thời gian, chưa phân bổ.'),
    (N'DH261024002', 2, 11, 2, 165000, 0, 0, 0, NULL, N'Chỉ ghi yêu cầu mua; nhân viên sẽ xem khả năng làm.');

    /* 11. Thanh toán: chỉ SUCCESS mới tính thực thu.
       PENDING là khách đã báo chuyển nhưng chưa đối chiếu.
       REJECTED không tạo tiền thu; không được hoàn một khoản bị từ chối.
       Chèn toàn bộ PAYMENT trước, rồi REFUND trỏ về khoản gốc cùng đơn.
       Đơn DH261008001 thu 250.000 và hoàn 250.000: thực thu = 0.
       Phần tiền chưa thu của đơn đang giao chưa tạo giao dịch SUCCESS. */
    INSERT INTO dbo.ThanhToan
        (MaThanhToan, MaDonHang, MaThanhToanGoc, LoaiGiaoDich, SoTien, PhuongThuc, TrangThaiThanhToan, ThoiGianTao, ThoiGianXacNhan, MaNVXacNhan, DuongDanChungTu, GhiChu)
    VALUES
    (N'TT260915001', N'DH260915001', NULL, N'PAYMENT', 150000, N'BANK_TRANSFER', N'SUCCESS', N'2026-09-15T14:00:00', N'2026-09-15T15:00:00', 5, NULL, N'Cọc lần đầu; xác nhận trước khi chấp nhận đơn.'),
    (N'TT260918001', N'DH260915001', NULL, N'PAYMENT', 345000, N'CASH', N'SUCCESS', N'2026-09-18T10:40:00', N'2026-09-18T10:50:00', 5, NULL, N'Trả phần còn lại khi nhận bánh.'),
    (N'TT261001001', N'DH260930001', NULL, N'PAYMENT', 400000, N'CASH', N'SUCCESS', N'2026-10-01T09:30:00', N'2026-10-01T09:35:00', 5, NULL, N'Không cọc; thu đủ khi khách nhận tại tiệm.'),
    (N'TT261005001', N'DH261005001', NULL, N'PAYMENT', 300000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-05T12:00:00', N'2026-10-05T13:30:00', 5, NULL, N'Cọc riêng cho đơn, không áp tỷ lệ chung.'),
    (N'TT261008001', N'DH261005001', NULL, N'PAYMENT', 500000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-08T15:00:00', N'2026-10-08T15:30:00', 5, NULL, N'Thanh toán nốt trước khi giao.'),
    (N'TT261009001', N'DH261008001', NULL, N'PAYMENT', 250000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-09T09:00:00', N'2026-10-09T09:30:00', 5, NULL, N'Tiền cọc đã nhận; sau đó khách hủy.'),
    (N'TT261013001', N'DH261012001', NULL, N'PAYMENT', 200000, N'BANK_TRANSFER', N'REJECTED', N'2026-10-13T09:00:00', N'2026-10-13T10:00:00', 5, NULL, N'Từ chối do không khớp sao kê; không tính vào thực thu.'),
    (N'TT261018001', N'DH261018001', NULL, N'PAYMENT', 400000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-18T12:00:00', N'2026-10-18T14:00:00', 5, NULL, N'Cọc cho đơn hỗn hợp bánh sẵn và bánh cần làm.'),
    (N'TT261019001', N'DH261019001', NULL, N'PAYMENT', 600000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-19T10:00:00', N'2026-10-19T11:00:00', 5, NULL, N'Đã cọc, còn phải thu 905.000.'),
    (N'TT261021001', N'DH261021001', NULL, N'PAYMENT', 500000, N'BANK_TRANSFER', N'REJECTED', N'2026-10-21T11:00:00', N'2026-10-21T11:30:00', 5, NULL, N'Lần cọc đầu không khớp, không tính thực thu.'),
    (N'TT261021002', N'DH261021001', NULL, N'PAYMENT', 500000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-21T12:00:00', N'2026-10-21T12:30:00', 5, NULL, N'Lần cọc thứ hai đã đối chiếu thành công.'),
    (N'TT261022001', N'DH261022001', NULL, N'PAYMENT', 200000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-22T10:00:00', N'2026-10-22T10:30:00', 5, NULL, N'Đã cọc; thu nốt 505.000 khi giao, hiện chưa ghi nhận khoản đó.'),
    (N'TT261023001', N'DH261023001', NULL, N'PAYMENT', 300000, N'BANK_TRANSFER', N'PENDING', N'2026-10-23T15:00:00', NULL, NULL, N'/uploads/payments/tt261023001.jpg', N'Khách báo đã chuyển; chưa đối chiếu, chưa tính thực thu.');

    INSERT INTO dbo.ThanhToan
        (MaThanhToan, MaDonHang, MaThanhToanGoc, LoaiGiaoDich, SoTien, PhuongThuc, TrangThaiThanhToan, ThoiGianTao, ThoiGianXacNhan, MaNVXacNhan, DuongDanChungTu, GhiChu)
    VALUES
    (N'TT261012001', N'DH261008001', N'TT261009001', N'REFUND', 250000, N'BANK_TRANSFER', N'SUCCESS', N'2026-10-12T10:00:00', N'2026-10-12T10:30:00', 5, NULL, N'Hoàn đủ cọc khi hủy; giao dịch gốc thuộc cùng đơn.');


    /* =====================================================================
       ĐỐI CHIẾU TRƯỚC COMMIT
       Có lỗi bất kỳ thì rollback toàn bộ dữ liệu seed trong file.
       Đây là các truy vấn sẽ chạy khi người dùng thực thi script trên SQL Server.
       ===================================================================== */
    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Tổng tiền', d.MaDonHang, N'Tổng dòng chi tiết không khớp đầu đơn hoặc công thức phải trả sai.'
    FROM dbo.DonHang d
    OUTER APPLY
    (
        SELECT SUM(CONVERT(decimal(18,2), c.SoLuong) * c.DonGiaTaiThoiDiemDat) AS Tien
        FROM dbo.ChiTietDonHang c WHERE c.MaDonHang = d.MaDonHang
    ) t
    WHERE ISNULL(t.Tien, 0) <> d.TongTienHang
       OR d.TongTienPhaiTra <> d.TongTienHang + d.PhiGiaoHang - d.TienGiamGia
       OR d.TienCocYeuCau < 0 OR d.TienCocYeuCau > d.TongTienPhaiTra;

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Giữ bánh', CONVERT(nvarchar(100), b.MaBienThe),
           N'Tồn không hợp lệ hoặc số đã giữ không khớp các đơn hoạt động.'
    FROM dbo.BienTheBanh b
    OUTER APPLY
    (
        SELECT SUM(c.SoLuongCoSanDaGiu + c.SoLuongDaLam) AS Giu
        FROM dbo.ChiTietDonHang c
        JOIN dbo.DonHang d ON d.MaDonHang = c.MaDonHang
        WHERE c.MaBienThe = b.MaBienThe
          AND d.TrangThaiDonHang IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
    ) t
    WHERE b.SoLuongThucTe < 0 OR b.SoLuongDaGiu < 0
       OR b.SoLuongDaGiu > b.SoLuongThucTe OR b.SoLuongDaGiu <> ISNULL(t.Giu, 0);

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Phân bổ bánh', c.MaDonHang, N'Số bánh sẵn/cần làm/đã làm không phù hợp trạng thái đơn.'
    FROM dbo.ChiTietDonHang c JOIN dbo.DonHang d ON d.MaDonHang = c.MaDonHang
    WHERE
        (d.TrangThaiDonHang IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT')
         AND c.SoLuongCoSanDaGiu + c.SoLuongCanLam <> c.SoLuong)
        OR
        (d.TrangThaiDonHang IN ('SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT')
         AND c.SoLuongCoSanDaGiu + c.SoLuongDaLam <> c.SoLuong)
        OR
        (d.TrangThaiDonHang IN ('CHO_NHAN_VIEN_XU_LY','CHO_KHACH_XAC_NHAN','CHO_THANH_TOAN_COC','DA_HUY')
         AND (c.SoLuongCoSanDaGiu <> 0 OR c.SoLuongCanLam <> 0 OR c.SoLuongDaLam <> 0));

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Ngày đơn', d.MaDonHang, N'Nhân viên hoặc ngày giờ không khớp trạng thái đơn.'
    FROM dbo.DonHang d
    WHERE d.NgayDat > @NgayChot OR d.NgayXacNhan < d.NgayDat
       OR d.NgayXacNhan > @NgayChot OR d.NgayHoanTat > @NgayChot
       OR d.NgayHoanTat < d.NgayXacNhan
       OR (d.TrangThaiDonHang IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO','HOAN_TAT')
           AND (d.MaNVXacNhan IS NULL OR d.NgayXacNhan IS NULL))
       OR (d.TrangThaiDonHang = 'HOAN_TAT' AND (d.MaNVHoanTat IS NULL OR d.NgayHoanTat IS NULL))
       OR (d.TrangThaiDonHang <> 'HOAN_TAT' AND (d.MaNVHoanTat IS NOT NULL OR d.NgayHoanTat IS NOT NULL));

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Thanh toán', t.MaThanhToan, N'Ngày thu/xác nhận hoặc người xác nhận không hợp lệ.'
    FROM dbo.ThanhToan t JOIN dbo.DonHang d ON d.MaDonHang = t.MaDonHang
    WHERE t.ThoiGianTao < d.NgayDat OR t.ThoiGianTao > @NgayChot
       OR t.ThoiGianXacNhan < t.ThoiGianTao OR t.ThoiGianXacNhan > @NgayChot
       OR (t.TrangThaiThanhToan IN ('SUCCESS','REJECTED')
           AND (t.MaNVXacNhan IS NULL OR t.ThoiGianXacNhan IS NULL))
       OR (t.TrangThaiThanhToan = 'PENDING' AND (t.MaNVXacNhan IS NOT NULL OR t.ThoiGianXacNhan IS NOT NULL));

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Hoàn tiền', t.MaThanhToan, N'Hoàn tiền không trỏ tới khoản PAYMENT/SUCCESS cùng đơn hoặc thời gian sai.'
    FROM dbo.ThanhToan t
    LEFT JOIN dbo.ThanhToan g ON g.MaThanhToan = t.MaThanhToanGoc
    WHERE t.LoaiGiaoDich = 'REFUND'
      AND (g.MaThanhToan IS NULL OR g.LoaiGiaoDich <> 'PAYMENT'
           OR g.TrangThaiThanhToan <> 'SUCCESS' OR g.MaDonHang <> t.MaDonHang
           OR t.MaThanhToan = t.MaThanhToanGoc
           OR g.ThoiGianXacNhan IS NULL OR t.ThoiGianTao < g.ThoiGianXacNhan);

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Giới hạn hoàn', g.MaThanhToan, N'Tổng số đã hoàn vượt số tiền gốc.'
    FROM dbo.ThanhToan g
    JOIN dbo.ThanhToan r ON r.MaThanhToanGoc = g.MaThanhToan
    WHERE r.LoaiGiaoDich = 'REFUND' AND r.TrangThaiThanhToan = 'SUCCESS'
    GROUP BY g.MaThanhToan, g.SoTien HAVING SUM(r.SoTien) > g.SoTien;

    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Thực thu', d.MaDonHang, N'Thực thu/cọc không khớp trạng thái đơn.'
    FROM dbo.DonHang d
    OUTER APPLY
    (
        SELECT SUM(CASE WHEN t.LoaiGiaoDich = 'PAYMENT' THEN t.SoTien ELSE -t.SoTien END) AS Tien
        FROM dbo.ThanhToan t
        WHERE t.MaDonHang = d.MaDonHang AND t.TrangThaiThanhToan = 'SUCCESS'
    ) t
    WHERE ISNULL(t.Tien,0) < 0 OR ISNULL(t.Tien,0) > d.TongTienPhaiTra
       OR (d.TrangThaiDonHang = 'HOAN_TAT' AND ISNULL(t.Tien,0) <> d.TongTienPhaiTra)
       OR (d.TrangThaiDonHang = 'DA_HUY' AND ISNULL(t.Tien,0) <> 0)
       OR (d.TrangThaiDonHang IN ('DA_XAC_NHAN','DANG_CHUAN_BI','SAN_SANG_GIAO','DANG_GIAO')
           AND (d.TienCocYeuCau IS NULL OR ISNULL(t.Tien,0) < d.TienCocYeuCau));

    -- Phải đủ cọc ngay tại thời điểm nhận đơn, không dùng lần thu sau để bù.
    INSERT INTO @Loi (Nhom, DoiTuong, ChiTiet)
    SELECT N'Cọc lúc nhận đơn', d.MaDonHang, N'Chưa đủ cọc thành công trước NgayXacNhan.'
    FROM dbo.DonHang d
    OUTER APPLY
    (
        SELECT SUM(t.SoTien) AS Tien FROM dbo.ThanhToan t
        WHERE t.MaDonHang = d.MaDonHang AND t.LoaiGiaoDich = 'PAYMENT'
          AND t.TrangThaiThanhToan = 'SUCCESS' AND t.ThoiGianXacNhan <= d.NgayXacNhan
    ) t
    WHERE d.NgayXacNhan IS NOT NULL AND d.TienCocYeuCau > ISNULL(t.Tien,0);

    -- @MaCT có char(11) nên dùng RTRIM khi xét độ dài mã phiếu char(10).
    INSERT INTO @MaCT (Bang,Ma,MaNgay,NgayGio,Stt)
    SELECT N'DonHang',MaDonHang,CONVERT(char(6),NgayDat,12),NgayDat,CONVERT(int,RIGHT(MaDonHang,3)) FROM dbo.DonHang
    UNION ALL SELECT N'ThanhToan',MaThanhToan,CONVERT(char(6),ThoiGianTao,12),ThoiGianTao,CONVERT(int,RIGHT(MaThanhToan,3)) FROM dbo.ThanhToan;

    INSERT INTO @Loi (Nhom,DoiTuong,ChiTiet)
    SELECT N'Mã chứng từ',RTRIM(Ma),N'Ngày trong mã không khớp ngày nghiệp vụ.'
    FROM @MaCT
    WHERE SUBSTRING(Ma,3,6) <> MaNgay
       OR Stt NOT BETWEEN 1 AND 999;

    ;WITH SoThuTu AS
    (
        SELECT *, ROW_NUMBER() OVER (PARTITION BY Bang,MaNgay ORDER BY Stt) AS MongDoi
        FROM @MaCT
    )
    INSERT INTO @Loi (Nhom,DoiTuong,ChiTiet)
    SELECT N'Số thứ tự mã',RTRIM(Ma),N'Dãy số trong ngày không bắt đầu 001 hoặc bị gián đoạn.'
    FROM SoThuTu WHERE Stt<>MongDoi;

    -- Kiểm tra số dòng mong đợi, đồng thời bảo đảm không sót bảng chi tiết nào.
    IF (SELECT COUNT(*) FROM dbo.ChiTietDonHang)<>24
       OR (SELECT COUNT(*) FROM dbo.ThanhToan)<>14
        INSERT INTO @Loi VALUES (N'Số dòng',N'Chi tiết đơn hàng/thanh toán',N'Số dòng khác bộ seed dự kiến.');

    IF EXISTS (SELECT 1 FROM @Loi)
    BEGIN
        SELECT Nhom,DoiTuong,ChiTiet FROM @Loi ORDER BY Nhom,DoiTuong;
        THROW 50002,N'Dữ liệu mẫu không đạt kiểm tra nghiệp vụ. Toàn bộ seed được rollback.',1;
    END;

    COMMIT TRANSACTION;
    PRINT N'Đã chèn đủ 11 bảng và vượt qua các kiểm tra của script.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    -- THROW giữ nguyên số lỗi, thông báo và dòng lỗi để dễ tìm trong SSMS.
    THROW;
END CATCH;

/* Kết quả sau COMMIT: dùng để đọc và đối chiếu; không thay đổi dữ liệu. */
SELECT N'NhanVien' AS Bang,COUNT(*) AS SoDong FROM dbo.NhanVien
UNION ALL SELECT N'Quyen',COUNT(*) FROM dbo.Quyen
UNION ALL SELECT N'LoaiBanh',COUNT(*) FROM dbo.LoaiBanh
UNION ALL SELECT N'KhachHang',COUNT(*) FROM dbo.KhachHang
UNION ALL SELECT N'PhanQuyen',COUNT(*) FROM dbo.PhanQuyen
UNION ALL SELECT N'SanPham',COUNT(*) FROM dbo.SanPham
UNION ALL SELECT N'HinhAnhBanh',COUNT(*) FROM dbo.HinhAnhBanh
UNION ALL SELECT N'BienTheBanh',COUNT(*) FROM dbo.BienTheBanh
UNION ALL SELECT N'DonHang',COUNT(*) FROM dbo.DonHang
UNION ALL SELECT N'ChiTietDonHang',COUNT(*) FROM dbo.ChiTietDonHang
UNION ALL SELECT N'ThanhToan',COUNT(*) FROM dbo.ThanhToan;

SELECT d.MaDonHang,d.TrangThaiDonHang,d.TongTienHang,d.PhiGiaoHang,d.TienGiamGia,
       d.TongTienPhaiTra,d.TienCocYeuCau,ISNULL(t.ThucThu,0) AS ThucThu,
       CASE WHEN d.TrangThaiDonHang='DA_HUY' THEN 0
            ELSE d.TongTienPhaiTra-ISNULL(t.ThucThu,0) END AS ConPhaiTra
FROM dbo.DonHang d OUTER APPLY
(
    SELECT SUM(CASE WHEN LoaiGiaoDich='PAYMENT' THEN SoTien ELSE -SoTien END) AS ThucThu
    FROM dbo.ThanhToan WHERE MaDonHang=d.MaDonHang AND TrangThaiThanhToan='SUCCESS'
) t ORDER BY d.MaDonHang;

SELECT b.MaBienThe,s.TenSP,b.TenBienThe,b.SoLuongThucTe,b.SoLuongDaGiu,
       b.SoLuongThucTe-b.SoLuongDaGiu AS CoTheNhanNgay,b.TrangThaiBan
FROM dbo.BienTheBanh b JOIN dbo.SanPham s ON s.MaSP=b.MaSP ORDER BY b.MaBienThe;


SELECT d.MaDonHang,c.STT,s.TenSP,b.TenBienThe,c.SoLuong,c.DonGiaTaiThoiDiemDat,
       c.SoLuongCoSanDaGiu,c.SoLuongCanLam,c.SoLuongDaLam
FROM dbo.DonHang d JOIN dbo.ChiTietDonHang c ON c.MaDonHang=d.MaDonHang
JOIN dbo.BienTheBanh b ON b.MaBienThe=c.MaBienThe
JOIN dbo.SanPham s ON s.MaSP=b.MaSP ORDER BY d.MaDonHang,c.STT;
