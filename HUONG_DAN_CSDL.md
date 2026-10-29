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

Nhân viên mở chi tiết đơn và dùng các nút hành động thay cho form sửa trạng thái. Tiếp nhận yêu cầu ngày giao dự kiến, phí giao, giảm giá và tiền cọc. Nhân viên ghi nhận khách đồng ý; nếu đơn có cọc thì xác nhận đơn sau khi giao dịch cọc đã được xác nhận. Sau đó đơn đi qua chuẩn bị, sẵn sàng giao, đang giao và hoàn tất. Có thể hủy đơn đang hoạt động nhưng phải nhập lý do. Việc cập nhật số bánh đã làm trong chi tiết đơn không tự chuyển trạng thái; trạng thái chỉ đổi bằng hành động tương ứng.

Mỗi lần đổi trạng thái được kiểm tra trong SQL transaction; nhân viên lấy từ session đăng nhập, thời điểm lấy từ SQL Server. Lịch sử lưu trạng thái trước/sau, người thực hiện, thời điểm và lý do hủy. Không cho mở lại hoặc sửa đơn đã hoàn tất/hủy. Database dùng quy trình hiện tại chưa có màn hình khách tự xác nhận; nhân viên ghi nhận việc khách đã đồng ý.

## Quyền nhân viên

`Quyen` có hai quyền ứng dụng: `ADMIN` và `SALES`. Gán quyền cho nhân viên bằng bản ghi trong `PhanQuyen`. Tài khoản quản trị đầu tiên có thể được cấp `ADMIN` trực tiếp trong SSMS sau khi tạo nhân viên.

## Phạm vi schema

Database mới gồm các bảng nhân viên, quyền, loại bánh, khách hàng, phân quyền, sản phẩm, ảnh bánh, biến thể bánh, đơn hàng, nhật ký trạng thái đơn, chi tiết đơn hàng và thanh toán. Các khóa nhân viên ở sản phẩm, đơn hàng, thanh toán và nhật ký phục vụ nghiệp vụ bán bánh.
