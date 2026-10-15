# Kiểm tra và triển khai bản sửa

## 1. Kiểm tra offline

Các lệnh sau không chạy ứng dụng web và không kết nối database:

```powershell
dotnet build SmartGymManagement.slnx --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet run --project Tests/SmartGymManagement.Checks.csproj --no-build --no-restore
dotnet ef migrations has-pending-model-changes --no-build --context ApplicationDbContext
```

`DesignTimeDbContextFactory` hiện đọc cấu hình ứng dụng như runtime, không còn dùng database giả. Bộ checks offline tự cấu hình kết nối giả riêng và không mở kết nối. Factory chỉ tạo context, không chạy startup/seeder. Các lệnh làm thay đổi database cần xác định đúng đích và được cho phép trước khi chạy; xem [EF_DATABASE_CONFIGURATION_VI.md](EF_DATABASE_CONFIGURATION_VI.md).

## 2. Schema phải được cập nhật trước khi chạy bản sửa

Migration `20261008192434_HardenGymWorkflows` hiện chỉ là mã nguồn, **chưa áp dụng**.

Các thay đổi schema:

- Hợp nhất quan hệ ChatbotMessage–Member và bỏ `MemberId1` dư thừa.
- Thêm tên/giá/thời hạn gói tại thời điểm mua, cho phép null đối với lịch sử cũ.
- Thêm ngày hoàn tiền, cho phép null với giao dịch cũ.
- Unique index cho Attendance.BookingId, booking đang giữ chỗ, mã hóa đơn và mã giao dịch.
- Index phục vụ truy vấn lịch lớp, lịch sử fitness và hội thoại.
- Giữ nguyên độ dài khóa của các bảng Identity hiện tại.

Migration có kiểm tra trước và dừng nếu phát hiện bản ghi trùng hoặc quan hệ chatbot mâu thuẫn. Không tự xóa bản ghi trùng và không bịa dữ liệu giá mua trong quá khứ.

Chỉ áp dụng sau khi được cho phép và thực hiện đầy đủ:

1. Sao lưu database SQL Server thật và xác định đúng database đích.
2. Kiểm tra lịch sử migration thực tế phù hợp với nguồn; script thay đổi giả định database đã ở `20260709233645_AddChatbotMessages`.
3. Chạy file `PRE_MIGRATION_REVIEW.sql` để xem số lượng vấn đề. File này chỉ đọc dữ liệu; không được thực thi trong lượt sửa hiện tại.
4. Nếu có trùng/mâu thuẫn, xem xét nghiệp vụ và thống nhất cách xử lý riêng. Không bỏ qua guard để ép migration chạy.
5. Xem xét file `HardenGymWorkflows.review.sql` thay đổi schema, thử trên bản sao database và kiểm tra lại các luồng bên dưới.
6. Áp dụng script đã duyệt lên database đích trong thời gian bảo trì, sau đó mới khởi động ứng dụng mới.

Script đã duyệt có kiểm tra migration tiền nhiệm, `XACT_ABORT` và `TRY/CATCH` để rollback khi lỗi. Script không được chạy lại trên database đã có migration mới hơn. File SQL chỉ được sinh để xem xét, chưa được thực thi.

Không dùng `Down` để quay lui tùy tiện: quay lui sẽ mất các cột snapshot và ngày hoàn tiền mới. Khi cần phục hồi, dùng kế hoạch và bản sao lưu đã kiểm chứng.

Dữ liệu membership cũ được giữ nguyên ngày hiệu lực và trạng thái. Công thức thời hạn mới chỉ áp dụng cho các lần mua mới. Snapshot lịch sử cũ vẫn null; tên gói có thể hiển thị theo danh mục hiện tại nếu không có snapshot, giá dựa trên giao dịch được lưu.

## 3. Cấu hình không chứa bí mật

Mặc định Development:

- `SeedData:Enabled = false`.
- `Registration:Enabled = false`: Admin cấp tài khoản và liên kết đặt mật khẩu.
- `FitnessAI:Enabled = false`: chatbot dùng dữ liệu và câu trả lời cơ bản tiếng Việt.
- `Chatbot:HistoryLimit = 500`, giới hạn được chặn trong khoảng 20–2.000 cuộc trao đổi mỗi hội viên.

Không lưu mật khẩu, key hoặc cấu hình kết nối bí mật vào file được chia sẻ. Dùng User Secrets khi phát triển hoặc biến môi trường/kho bí mật khi triển khai.

### AI tùy chọn

Service hỗ trợ endpoint tương thích giao thức chat completions. Cần cấu hình:

- `FitnessAI:Enabled`.
- `FitnessAI:Endpoint`: URL đầy đủ của endpoint chat completions.
- `FitnessAI:Model`: tên model provider hỗ trợ.
- `FitnessAI:ApiKey`: chỉ lưu trong User Secrets hoặc biến môi trường, nếu provider yêu cầu.

Tên biến môi trường tương ứng dùng dấu gạch dưới kép, ví dụ `FitnessAI__ApiKey`. Không gửi key trong hội thoại hoặc đưa vào ảnh chụp màn hình.

Provider từ xa phải dùng HTTPS; HTTP chỉ được phép ở localhost. Endpoint chỉ do người triển khai cấu hình, không nhận từ người dùng website.

Khi AI bật, hệ thống gửi câu hỏi, tối đa sáu cuộc trao đổi gần nhất và một phần assessment. Không tự gửi tên/email/ID hồ sơ, nhưng câu hỏi và lịch sử do người dùng nhập có thể chứa thông tin riêng. Cần thông báo chính sách dữ liệu và chọn provider phù hợp trước khi bật. Khi provider lỗi hoặc chưa cấu hình, dùng fallback cơ bản; không coi fallback là phản hồi từ mô hình AI.

### Seed dữ liệu mẫu

Chỉ bật trong Development, trên database dành riêng cho demo. Mật khẩu seed phải được cấu hình bằng bí mật riêng (`SeedData:AdminPassword`, `SeedData:MemberPassword`). Seeder vẫn tạo dữ liệu mẫu khi được bật; không bật trên database cần bảo toàn dữ liệu.

### Đăng ký/tài khoản

- Admin tạo hội viên không có mật khẩu dùng chung; chuyển link một lần qua kênh bảo mật.
- Có thể cấp lại link ở Details khi tài khoản chưa có mật khẩu. Cấp lại sẽ vô hiệu hóa link cũ.
- Link không sử dụng được sau khi tài khoản đã có mật khẩu; token reset tuân theo thời hạn của Identity.
- Nếu bật tự đăng ký, form tạo cả Identity user, role Member và hồ sơ Member. Quy tắc email `@smartgym.com` được giữ như luồng Admin hiện có.
- Xác nhận email chưa bắt buộc; chưa cấu hình dịch vụ email. Không bật yêu cầu xác nhận email khi chưa hoàn thiện luồng gửi thư.
- Lưu giữ Data Protection keys ổn định khi triển khai nhiều instance để link đặt mật khẩu và QR hoạt động thống nhất.

## 4. Kiểm thử trên database thử nghiệm sau khi được phép

| Luồng | Ca cần xác nhận |
|---|---|
| Tài khoản | Tạo hội viên; đặt mật khẩu; dùng lại link; cấp lại link; khóa tài khoản đang có phiên; xác nhận phiên bị chặn |
| Gói tập | Tạo/sửa/xem/ngừng cung cấp; giá và thời hạn không mất; lịch sử mua không đổi khi sửa danh mục |
| Membership | Gói một ngày/30 ngày; tiếp giáp cùng ngày bị từ chối, ngày kế tiếp hợp lệ; Pending không đặt lớp được; xác nhận biên nhận và hủy/hoàn tiền |
| Booking | Hai tài khoản đặt chỗ cuối cùng đồng thời; gửi lặp cùng booking; khôi phục booking khi lớp đầy; lịch trùng và gói không đủ hạn |
| Điểm danh | Hai request check-in cùng booking; trước/sau cửa sổ; trạng thái Attended không sửa trực tiếp; NoShow chỉ sau hạn |
| QR | Chủ booking tạo QR; Admin quét/dán; QR hết hạn hoặc sửa nội dung bị chặn; QR booking khác bị chặn; dùng lại sau check-in bị chặn |
| Lịch lớp | Giảm capacity; đổi giờ gây trùng/ngoài hạn; hủy lớp có booking tạo thông báo; không hủy lớp có điểm danh; sửa trạng thái lớp quá khứ |
| Phòng/Trainer | Không ngừng hoạt động khi còn lớp tương lai; không giảm sức chứa phòng dưới lớp đã lên lịch |
| BMI | Hội viên khác không xem/xóa được; thiếu hồ sơ thấy lỗi; số đo thiếu/bất hợp lý; xóa chỉ bản ghi của mình |
| Chatbot | Việt/Anh/không dấu; giảm cân không trả BMI; dữ liệu riêng đúng người; 429; hết phiên; provider lỗi; lịch sử không vượt giới hạn |
| Báo cáo | Đối chiếu thu vào/hoàn tiền/ròng; booking quá hạn chưa gán NoShow vẫn vào mẫu số; lọc ngày; ngày biên và múi giờ |
| Excel | Summary và năm sheet chi tiết, kỳ ngày khớp; giá lịch sử; ngày và tiền; giới hạn dòng; mở file bằng Excel |

Nút **Cập nhật gói hết hạn và booking vắng mặt** ở Reports là POST của Admin. Nó cập nhật trạng thái khi Admin chủ động bấm; không có tác vụ nền tự ghi dữ liệu khi khởi động hoặc đọc báo cáo.

Gym dùng múi giờ `Asia/Ho_Chi_Minh`. Ngày giờ lịch sử không được tự chuyển đổi; cần kiểm tra múi giờ của dữ liệu cũ trước khi triển khai ở máy khác.

Thanh toán điện tử và hoàn tiền hiện là ghi nhận thủ công. Chỉ bấm xác nhận sau khi đã nhận/hoàn tiền thật. Tích hợp callback và chữ ký của cổng thanh toán cần thông tin merchant/provider riêng; chưa được giả lập là thành công.
