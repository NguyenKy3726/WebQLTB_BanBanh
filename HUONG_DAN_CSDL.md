# Hướng dẫn cài đặt database WebQLTB

Gói này được thiết kế để cài mới trên database `DbQLTB` trống.

## `Proc_Create.sql` dùng làm gì?

`Project/SQL_Query/Proc_Create.sql` tạo hoặc cập nhật các stored procedure thêm nhân viên, quyền, loại bánh, khách hàng, phân quyền, sản phẩm, ảnh bánh, biến thể, đơn hàng, chi tiết đơn và thanh toán. Khi chạy file, SQL Server chỉ lưu định nghĩa procedure; dữ liệu chỉ được ghi khi ứng dụng hoặc câu lệnh SQL gọi procedure.

File này không tạo bảng và không nạp dữ liệu mẫu. `Table.sql` tạo bảng; `Insert.sql` là dữ liệu mẫu tùy chọn.

## Thứ tự cài đặt

1. Trong SSMS, tạo database `DbQLTB` nếu chưa tồn tại.
2. Chạy `Project/SQL_Query/Table.sql` để tạo 11 bảng.
3. Chạy lần lượt `Proc_Create.sql`, `Proc_Edit.sql`, `Proc_Delete.sql`, `Proc_Details.sql`, `Proc_Index.sql`, `Proc_Login.sql`.
4. Nếu cần dữ liệu mẫu, chạy `Insert.sql` khi các bảng còn trống. Tài khoản mẫu dùng mật khẩu `123456`, chỉ dành cho học tập.
5. Chạy `Proc_Improvements.sql` để thêm ràng buộc tên đăng nhập dùng chung, quyền ứng dụng và xử lý đơn website.
6. Trong `Project/WebQLTB/WebQLTB/appsettings.json`, sửa `DbQLTBConnection` cho đúng SQL Server và xác thực đang dùng.
7. Mở `Project/WebQLTB/WebQLTB.slnx` trong Visual Studio và chạy ứng dụng.

Chạy riêng từng file và dừng khi phát sinh lỗi. Không chạy `Insert.sql` lần nữa sau khi đã có dữ liệu.

## Quyền nhân viên

`Quyen` có hai quyền ứng dụng: `ADMIN` và `SALES`. Gán quyền cho nhân viên bằng bản ghi trong `PhanQuyen`. Tài khoản quản trị đầu tiên có thể được cấp `ADMIN` trực tiếp trong SSMS sau khi tạo nhân viên.

## Phạm vi schema

Database mới gồm các bảng nhân viên, quyền, loại bánh, khách hàng, phân quyền, sản phẩm, ảnh bánh, biến thể bánh, đơn hàng, chi tiết đơn hàng và thanh toán. Các khóa nhân viên ở sản phẩm, đơn hàng và thanh toán phục vụ nghiệp vụ bán bánh.
