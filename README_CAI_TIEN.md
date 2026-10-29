# WebQLTB — project bán bánh mới

Đây là gói cài mới từ database trống, tập trung vào cửa hàng bánh, đơn hàng và thanh toán.

## Luồng hiện có

- Khách có thể xem cửa hàng và biến thể bánh, đăng ký/đăng nhập, dùng giỏ hàng, đặt bánh và xem các đơn của chính mình.
- Nhân viên xử lý đơn, sản phẩm bánh và thanh toán theo quyền `ADMIN` hoặc `SALES`.
- SQL xác minh sản phẩm, biến thể, giá và nội dung giỏ hàng khi tạo đơn; đơn cùng các dòng bánh được ghi trong một transaction.

## Cài database mới

Tạo database `DbQLTB` trống. Trong SSMS, chạy theo thứ tự:

1. `Project/SQL_Query/Table.sql`
2. `Proc_Create.sql`
3. `Proc_Edit.sql`
4. `Proc_Delete.sql`
5. `Proc_Details.sql`
6. `Proc_Index.sql`
7. `Proc_Login.sql`
8. `Insert.sql` nếu muốn tạo dữ liệu mẫu; chỉ chạy khi database còn trống.
9. `Proc_Improvements.sql`

Cập nhật `DbQLTBConnection` trong `Project/WebQLTB/WebQLTB/appsettings.json` cho SQL Server của bạn. Script dữ liệu mẫu dùng mật khẩu học tập `123456`.

## Quyền

Chỉ có hai quyền ứng dụng: `ADMIN` và `SALES`. `ADMIN` có quyền quản trị; `SALES` xử lý khách hàng, đơn hàng, tiến độ bánh và thanh toán. Vai trò được cấp qua `Quyen` và `PhanQuyen`.

## Kiểm chứng

Schema mới có 11 bảng phục vụ nhân viên, quyền, khách hàng, loại bánh, sản phẩm, ảnh, biến thể, đơn hàng, chi tiết đơn và thanh toán. Source MVC và stored procedures được rà soát để không chứa phân hệ nhập/xuất nguyên liệu. Môi trường hiện tại không có SQL Server hoặc .NET SDK nên database và ứng dụng cần được chạy trong môi trường phát triển của bạn.
