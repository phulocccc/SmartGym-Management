# Xóa dữ liệu, giữ cấu trúc SmartGymDb

Script dành cho `(localdb)\MSSQLLocalDB`, database `SmartGymDb` trong cấu hình dự án. Script chưa được thực thi từ cuộc trò chuyện do môi trường không kết nối được LocalDB.

1. Dừng ứng dụng trong Visual Studio hoặc terminal đang chạy để tránh ghi thêm dữ liệu trong lúc xóa.
2. Mở PowerShell trên máy bằng tài khoản Windows đang sở hữu LocalDB. Chạy:

```powershell
cd C:\Users\Admin\source\repos\SmartGymManagement
& 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE' -S '(localdb)\MSSQLLocalDB' -d SmartGymDb -E -C -b -i '.\docs\CLEAR_ALL_DATA_KEEP_SCHEMA.sql'
```

Có thể mở script trong SSMS, kết nối đúng instance trên rồi Execute. Không chạy trên database khác.

Script sao lưu vào `DatabaseBackups`, kiểm tra bản sao lưu rồi mới xóa. Phần xóa chạy trong transaction, rollback nếu lỗi. Kết quả thành công phải có `RemainingRows = 0` cho mọi bảng dữ liệu và danh sách migration còn nguyên. Giữ lại tên/cấu trúc bảng, index, foreign key và lịch sử migration. Bộ đếm identity không được đặt lại; ID của dữ liệu mới có thể tiếp tục từ số cũ.

Tất cả tài khoản Admin/hội viên, role, gói, lớp, booking, thanh toán, hóa đơn, điểm danh, BMI, thông báo và chat đều bị xóa. Script không xóa file `app.db` hay các file backup khác. Sau khi xóa, chưa đăng nhập được cho tới khi cấp lại Admin và role. Seeder hiện tạo cả dữ liệu demo, vì vậy bật seeder sẽ làm database có dữ liệu mẫu, không còn trống.

Chỉ khởi động lại ứng dụng khi đã chuẩn bị tài khoản/dữ liệu thử phù hợp. Dùng [TESTCASE_VI.csv](TESTCASE_VI.csv) để điền kết quả 120 ca và [TEST_ALL_FUNCTIONS_VI.md](TEST_ALL_FUNCTIONS_VI.md) để xem cách chuẩn bị dữ liệu. Các ca cần dữ liệu lịch sử phải tạo fixture riêng trên database thử nghiệm.
