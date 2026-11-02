# Hướng dẫn cài đặt database WebQLTB

Gói này được thiết kế để cài mới trên database `DbQLTB` trống.

## `Proc_Create.sql` dùng làm gì?

`Project/SQL_Query/Proc_Create.sql` tạo hoặc cập nhật các stored procedure thêm nhân viên, quyền, loại bánh, khách hàng, phân quyền, sản phẩm, ảnh bánh, biến thể, đơn hàng, chi tiết đơn và thanh toán. Khi chạy file, SQL Server chỉ lưu định nghĩa procedure; dữ liệu chỉ được ghi khi ứng dụng hoặc câu lệnh SQL gọi procedure.

File này không tạo bảng và không nạp dữ liệu mẫu. `Table.sql` tạo bảng; `Insert.sql` là dữ liệu mẫu tùy chọn.

## Thứ tự cài đặt

1. Trong SSMS, tạo database `DbQLTB` nếu chưa tồn tại.
2. Chạy `Project/SQL_Query/Table.sql` để tạo 12 bảng, gồm bảng nhật ký trạng thái đơn hàng.
3. Chạy lần lượt `Proc_Create.sql`, `Proc_Edit.sql`, `Proc_Delete.sql`, `Proc_Details.sql`, `Proc_Index.sql`, `Proc_Login.sql`.
4. Chạy `Proc_Improvements.sql` để thêm ràng buộc tên đăng nhập dùng chung, quyền ứng dụng và xử lý đơn website.
5. Nếu cần dữ liệu mẫu, chạy `Insert.sql` khi các bảng còn trống. Tài khoản mẫu dùng mật khẩu `123456`, chỉ dành cho học tập.
6. Trong `Project/WebQLTB/WebQLTB/appsettings.json`, sửa `DbQLTBConnection` cho đúng SQL Server và xác thực đang dùng.
7. Mở `Project/WebQLTB/WebQLTB.slnx` trong Visual Studio và chạy ứng dụng.

Chạy riêng từng file và dừng khi phát sinh lỗi. Không chạy `Insert.sql` lần nữa sau khi đã có dữ liệu.

## Cập nhật database đã cài trước đó

Không chạy lại `Table.sql` trên database đang có dữ liệu. Chạy `Migration_QuyTrinhTrangThaiDonHang.sql` một lần để tạo bảng nhật ký và tạo mốc lịch sử ban đầu; sau đó chạy lại `Proc_Create.sql`, `Proc_Edit.sql`, `Proc_Details.sql` và `Proc_Improvements.sql` để cập nhật stored procedure.

## Luồng thao tác đơn hàng

Nhân viên mở chi tiết đơn và dùng các nút hành động thay cho form sửa trạng thái. Tiếp nhận yêu cầu ngày giao dự kiến, phí giao, giảm giá và tiền cọc. Khách mở Đơn hàng của tôi → Xem và xác nhận để đồng ý giá, phí giao hàng, cọc và lịch giao. Nhân viên vẫn có thể ghi nhận khách đồng ý khi chốt qua điện thoại; nếu đơn có cọc thì xác nhận đơn sau khi giao dịch cọc đã được xác nhận. Sau đó đơn đi qua chuẩn bị, sẵn sàng giao, đang giao và hoàn tất. Có thể hủy đơn đang hoạt động nhưng phải nhập lý do. Việc cập nhật số bánh đã làm trong chi tiết đơn không tự chuyển trạng thái; trạng thái chỉ đổi bằng hành động tương ứng.

Mỗi lần đổi trạng thái được kiểm tra trong SQL transaction; nhân viên lấy từ session đăng nhập, thời điểm lấy từ SQL Server. Lịch sử lưu trạng thái trước/sau, người thực hiện, thời điểm và lý do hủy. Không cho mở lại hoặc sửa đơn đã hoàn tất/hủy. Khách tự xác nhận được ghi là KHACH_HANG trong lịch sử. Đơn không cọc được xác nhận và phân bổ bánh qua procedure chung, theo nhân viên đã tiếp nhận báo giá. Đơn có cọc chỉ chuyển sang chờ thanh toán cọc; không tự ghi nhận đã thu tiền.

## Quyền nhân viên

`Quyen` có hai quyền ứng dụng: `ADMIN` và `SALES`. Gán quyền cho nhân viên bằng bản ghi trong `PhanQuyen`. Tài khoản quản trị đầu tiên có thể được cấp `ADMIN` trực tiếp trong SSMS sau khi tạo nhân viên.

## Phạm vi schema

Database mới gồm các bảng nhân viên, quyền, loại bánh, khách hàng, phân quyền, sản phẩm, ảnh bánh, biến thể bánh, đơn hàng, nhật ký trạng thái đơn, chi tiết đơn hàng và thanh toán. Các khóa nhân viên ở sản phẩm, đơn hàng, thanh toán và nhật ký phục vụ nghiệp vụ bán bánh.

## Bổ sung xác nhận từ phía khách (02/11/2026)

Với database đang dùng, chạy `Project/Database/Migration_KhachXacNhanDonHang.sql` sau khi đã cài bảng lịch sử qua `Migration_QuyTrinhTrangThaiDonHang.sql`. Không cần chạy lại Table.sql hoặc Insert.sql. Database mới chạy Proc_Edit.sql đã có đủ procedure.

Luồng: nhân viên tiếp nhận và chốt báo giá → khách xem và đồng ý → nếu có cọc, nhân viên ghi nhận khoản thu rồi xác nhận giao dịch thành công → xác nhận đơn khi đủ cọc → chuẩn bị/giao/hoàn tất như trước. Không cọc: khách đồng ý thì đơn được xác nhận, giữ bánh theo cùng quy tắc đang dùng. Khách chỉ xác nhận được đơn của mình; SQL khóa và kiểm tra lại trạng thái, tổng tiền, phí giao, giảm giá, cọc và lịch giao. Hai lần bấm đồng thời chỉ một lần được chấp nhận.

Màn hình nhân viên hiển thị riêng điện thoại/địa chỉ thường dùng của người đặt và điện thoại/địa chỉ giao hàng lưu trên đơn. Địa chỉ hồ sơ hiện tại có thể khác địa chỉ lúc đặt đơn; giao hàng theo địa chỉ trên đơn.

### Ảnh chuyển khoản

Ảnh chưa được đính kèm trong yêu cầu nên chưa có ảnh ngân hàng thật trong repository. Khi nhận được ảnh, lưu vào `Project/WebQLTB/WebQLTB/wwwroot/img/payments/whiskful-transfer.png` và đặt `Payment:QrImagePath` trong appsettings.json thành `~/img/payments/whiskful-transfer.png`. Để trống thì trang hướng dẫn liên hệ cửa hàng lấy thông tin tài khoản. Không dùng ảnh/tài khoản giả.

Hướng dẫn xuất hiện sau khi chốt báo giá và khi đơn còn số dư cần trả; ẩn với đơn hủy/hoàn tất. Khi đang chờ khách đồng ý, hướng dẫn nhắc xác nhận trước khi chuyển tiền. Nội dung sao chép chỉ chứa MaDonHang, không thêm tên hoặc lời nhắn. HTTP/LAN không có Clipboard API sẽ chọn sẵn mã và hướng dẫn Ctrl+C.

### Kiểm tra trên máy có .NET 10 và SQL Server

1. Build solution và chạy migration trên database thử nghiệm đã có schema hiện tại.
2. Tạo đơn bằng khách A; nhân viên tiếp nhận với phí giao 30.000, cọc 100.000 và lịch giao hợp lệ. Khách thấy đầy đủ báo giá, nút đồng ý và nội dung chuyển khoản đúng mã đơn.
3. Khách đồng ý: đơn thành CHO_THANH_TOAN_COC, lịch sử ghi KHACH_HANG; không có giao dịch thu tiền tự sinh. Nhân viên thu/xác nhận đủ cọc rồi xác nhận đơn, kiểm tra phân bổ bánh và tiếp tục giao/hoàn tất.
4. Tạo đơn khác có cọc 0: khách đồng ý thì DA_XAC_NHAN, ngày xác nhận và phân bổ bánh được cập nhật; lịch sử ghi KHACH_HANG. Đơn rỗng/bánh ngừng bán phải bị chặn và rollback.
5. Khách B gửi yêu cầu xác nhận mã đơn A: không được thay đổi đơn. Thiếu anti-forgery token: yêu cầu bị từ chối. Đăng xuất: chuyển đăng nhập, không đổi trạng thái.
6. Bấm hai lần hoặc gửi lại POST: chỉ có một lần đổi trạng thái/lịch sử. Hủy hoặc sửa báo giá/lịch giao từ tab nhân viên trước khi khách bấm: yêu cầu cũ bị chặn, khách phải xem lại giá mới.
7. Người đặt khác người nhận: nhân viên thấy hai số điện thoại và hai địa chỉ được ghi nhãn riêng. Khách thiếu hồ sơ hiển thị Chưa cung cấp.
8. Sao chép qua HTTPS và HTTP/LAN; kiểm tra đúng mã đơn, không có khoảng trắng hoặc lời nhắn. Sau khi thêm ảnh thật, kiểm tra ảnh trên desktop/mobile.

Kiểm tra JavaScript tự động: `node --test tests/order-confirmation.test.mjs`.
