# Các thay đổi Smart Gym Management

## Phạm vi và trạng thái

Đã sửa mã nguồn và chuẩn bị migration `20261008192434_HardenGymWorkflows`. Không chạy ứng dụng web, không mở kết nối SQL Server, không áp dụng migration và không thay đổi bản ghi trong quá trình sửa.

Thư mục ban đầu không có Git. Bản sao mã nguồn trước khi sửa nằm tại:

`C:\Users\Admin\AppData\Local\Temp\SmartGymManagement-source-20261009-020154`

Bản sao này là mã nguồn, không phải bản sao lưu SQL Server. Không chia sẻ hoặc đưa bản sao cấu hình cũ vào repository công khai.

## Chức năng đã sửa/bổ sung

| Nhóm | Thay đổi |
|---|---|
| Gói tập | Sửa model/route của Details và Delete; thêm giá/thời hạn và validation ở Edit; giữ ngày tạo; ngừng cung cấp thay vì xóa lịch sử |
| BMI | Bổ sung Delete kiểm tra quyền sở hữu; hiển thị lỗi; tách công thức vào service; phân loại trước khi làm tròn; từ chối body fat bất hợp lý; bảo vệ lean mass; cho phép dùng riêng vòng eo |
| Tài khoản | Bỏ mật khẩu chung; Admin cấp/cấp lại liên kết đặt mật khẩu; yêu cầu mật khẩu mới từ 10 ký tự; kiểm tra tài khoản hoạt động khi đăng nhập và trên request; tạo tài khoản/hồ sơ trong transaction |
| Đăng ký | Mặc định chỉ Admin tạo tài khoản. Có luồng tự đăng ký tạo Member và role đầy đủ khi bật cấu hình; giữ quy tắc email `@smartgym.com` hiện có |
| Booking | Dùng validation chung; giao dịch và application lock SQL Server cho các thao tác liên quan giữ chỗ; chống chuyển trạng thái bỏ qua nghiệp vụ; kiểm tra lại khi khôi phục booking |
| Điểm danh | Giữ thao tác tạo Attendance và đổi trạng thái trong transaction; unique constraint theo Booking; chỉ cho đánh dấu NoShow sau hạn check-in |
| QR | QR được tạo cục bộ, chứa token ký có thời hạn 10 phút; chỉ chủ booking lấy được mã; Admin xác nhận điểm danh; chống mã giả/hết hạn/sai hội viên và điểm danh lặp |
| Lịch lớp | Kiểm tra Trainer đang hoạt động, số chỗ đã giữ, gói và lịch hội viên khi đổi giờ; hủy booking và tạo thông báo khi hủy lớp; không hủy lớp đã có điểm danh |
| Phòng/Trainer | Gán rõ các trường được phép; validation không lỗi null; ngừng hoạt động thay vì xóa; yêu cầu xử lý các lớp tương lai trước khi ngừng hoạt động; bảo vệ sức chứa phòng |
| Membership | Thời hạn tính cả ngày đầu/ngày cuối; chặn trùng ngày biên; trạng thái Pending; lưu tên/giá/thời hạn khi mua; hủy booking tương lai không còn gói hợp lệ khi hủy membership |
| Thanh toán | Tiền mặt cần xác nhận đã thu; thanh toán điện tử chờ đối soát; Admin xác nhận bằng biên nhận; hoàn toàn bộ tiền được ghi nhận riêng và có ngày hoàn tiền; không tự chuyển tiền |
| Dashboard | Loại lớp hủy khỏi lịch sắp tới; ngày còn lại tính cả hôm nay; dùng tên gói tại thời điểm mua nếu có |
| Hồ sơ/thông báo | Hội viên sửa hồ sơ của mình, cập nhật ngày sinh/giới tính cho assessment; đọc thông báo lớp/gói đã hủy |
| Chatbot | Nhận diện ý định Việt/Anh, không dấu; sửa lỗi câu hỏi giảm cân bị nhận thành BMI; tránh khớp một phần từ; trả lời cơ bản tiếng Việt; AI tùy cấu hình, có fallback |
| Báo cáo | Lọc kỳ ngày; tổng hợp truy vấn theo tháng; gộp lớp theo tên; tỷ lệ tham gia dựa trên Attendance và booking quá hạn; phân biệt thu vào/hoàn tiền/doanh thu ròng |
| Excel | Áp dụng kỳ ngày, bổ sung Summary, dùng giá mua/lịch sử thanh toán; giới hạn tổng 50.000 dòng của năm bảng chi tiết để tránh xuất quá lớn |
| Giao diện | Phân trang 20 dòng; tìm kiếm/lọc trạng thái trong các danh sách quản trị; validation summary; xử lý lỗi HTTP/phiên đăng nhập trên chatbot |
| Vận hành | Seed mặc định tắt, chỉ Development; bỏ gọi lặp và reset tài khoản mẫu; múi giờ gym thống nhất; audit thao tác Admin qua logging; phản hồi 409 cho xung đột dữ liệu |
| Phụ thuộc | Bỏ SQLite provider không sử dụng và việc sao chép app.db khi build; ghim Chart.js 4.4.8; thêm QRCoder 1.8.0 |

QR dùng [QRCoder](https://github.com/Shane32/QRCoder), không gửi token đến dịch vụ tạo QR bên ngoài.

## Kiểm tra đã thực hiện

- Build toàn solution: 0 lỗi, 0 cảnh báo tại lần kiểm tra cuối.
- Bộ kiểm tra console `Tests/SmartGymManagement.Checks.csproj`: 59 trường hợp, gồm công thức fitness, ngưỡng, thời hạn, chuyển trạng thái, nhận diện chatbot, quan hệ/index EF và AI/QR với dữ liệu giả.
- Kiểm tra model và sinh SQL ở chế độ thiết kế không mở database.
- Không chạy `dotnet run` cho ứng dụng web. Lệnh chạy Tests chỉ khởi động chương trình kiểm tra, không gọi Program của ứng dụng.

## Những việc chưa thể xác nhận ở chế độ offline

- Chưa kiểm tra concurrency thực tế, quyền `sp_getapplock`, các truy vấn báo cáo và CRUD với SQL Server đang sử dụng.
- Chưa kiểm tra camera QR trên thiết bị thật. Quét camera dùng BarcodeDetector; trình duyệt không hỗ trợ vẫn có thể dán token.
- AI chưa được bật và chưa gọi provider thật; cần endpoint/model và key riêng nếu provider yêu cầu.
- Chưa có kết nối thanh toán VNPay/Visa tự động hoặc chuyển tiền hoàn trả. Các nút xác nhận chỉ ghi nhận kết quả Admin đã đối soát thực tế.
- Không có email SMTP được cấu hình. Liên kết thiết lập mật khẩu được Admin chuyển riêng; không được đưa vào kênh công khai.
- Audit hiện ghi qua `ILogger`; cần cấu hình nơi lưu log bền vững khi triển khai.
- Role Trainer vẫn được khai báo trong Identity; module quản trị Trainer không tự cấp tài khoản và chưa có khu vực làm việc riêng cho Trainer.
- Không thực hiện kiểm thử giao diện end-to-end vì ứng dụng cần schema SQL Server mới.

Xem [hướng dẫn kiểm tra và triển khai](VERIFY_AND_DEPLOY_VI.md) trước khi chạy bản sửa.
