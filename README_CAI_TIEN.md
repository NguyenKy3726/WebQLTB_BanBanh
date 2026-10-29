# WebQLTB — project bán bánh mới

Đây là gói cài mới từ database trống, tập trung vào cửa hàng bánh, đơn hàng và thanh toán.

## Luồng hiện có

- Khách có thể xem cửa hàng và biến thể bánh, đăng ký/đăng nhập, dùng giỏ hàng, đặt bánh và xem các đơn của chính mình.
- Nhân viên xử lý đơn, sản phẩm bánh và thanh toán theo quyền `ADMIN` hoặc `SALES`.
- SQL xác minh sản phẩm, biến thể, giá và nội dung giỏ hàng khi tạo đơn; đơn cùng các dòng bánh được ghi trong một transaction.
- Đơn được xử lý bằng hành động theo trạng thái; nhân viên không sửa trạng thái trực tiếp trong form chung.
- Các bước được kiểm tra theo trạng thái nguồn trong SQL: tiếp nhận, ghi nhận khách đồng ý, xác nhận sau cọc, bắt đầu chuẩn bị, sẵn sàng giao, bắt đầu giao, hoàn tất hoặc hủy.
- Lịch sử trạng thái ghi nhân viên, thời điểm từ SQL Server và lý do hủy; đơn hoàn tất/hủy không thể mở lại.

## Cài database mới

Tạo database `DbQLTB` trống. Trong SSMS, chạy theo thứ tự:

1. `Project/SQL_Query/Table.sql`
2. `Proc_Create.sql`
3. `Proc_Edit.sql`
4. `Proc_Delete.sql`
5. `Proc_Details.sql`
6. `Proc_Index.sql`
7. `Proc_Login.sql`
8. `Proc_Improvements.sql`
9. `Insert.sql` nếu muốn tạo dữ liệu mẫu; chỉ chạy khi database còn trống.

Cập nhật `DbQLTBConnection` trong `Project/WebQLTB/WebQLTB/appsettings.json` cho SQL Server của bạn. Script dữ liệu mẫu dùng mật khẩu học tập `123456`.

## Quyền

Chỉ có hai quyền ứng dụng: `ADMIN` và `SALES`. `ADMIN` có quyền quản trị; `SALES` xử lý khách hàng, đơn hàng, tiến độ bánh và thanh toán. Vai trò được cấp qua `Quyen` và `PhanQuyen`.

## Kiểm chứng

Schema mới có 12 bảng phục vụ nhân viên, quyền, khách hàng, loại bánh, sản phẩm, ảnh, biến thể, đơn hàng, lịch sử trạng thái, chi tiết đơn và thanh toán. Source MVC và stored procedures được rà soát để không chứa phân hệ nhập/xuất nguyên liệu. Cài database cũ cần chạy `Migration_QuyTrinhTrangThaiDonHang.sql` trước khi cập nhật procedure; cài mới thì `Table.sql` đã tạo sẵn bảng nhật ký.

Môi trường đóng gói hiện không có .NET CLI hoặc SQL Server nên chưa thể biên dịch ứng dụng hay chạy các stored procedure tại đây. Sau khi cập nhật SQL, hãy chạy `dotnet build` và cài migration trên database phát triển trước khi demo.
