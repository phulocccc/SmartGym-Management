# Checklist kiểm thử SmartGymManagement

Ngày soạn: 09/10/2026. Phạm vi: các chức năng đang có trong mã nguồn, gồm trang công khai, Identity, Admin và hội viên. Quản lý huấn luyện viên là chức năng Admin; hiện không có cổng Trainer riêng.

## 1. Cách sử dụng

Ghi vào cột **KQ**: `PASS`, `FAIL`, `BLOCKED` hoặc `N/A`. Các cột đang để `—` là **chưa kiểm thử**, không phải đã đạt. Với ca FAIL, ghi mã ca, bước gây lỗi, dữ liệu, kết quả thực tế và ảnh/log vào mẫu cuối file. Chạy từng ca trên dữ liệu riêng để việc hủy, khóa hoặc điểm danh không ảnh hưởng ca tiếp theo.

Chạy trên database thử nghiệm hoặc bản sao có schema phù hợp với phiên bản mã nguồn. Không thực hiện các ca hủy, hoàn tiền, khóa tài khoản trên dữ liệu vận hành. Ba bản sửa lần này không thêm migration. File hướng dẫn triển khai cũ [VERIFY_AND_DEPLOY_VI.md](VERIFY_AND_DEPLOY_VI.md) có thông tin migration từ thời điểm soạn; kiểm tra trạng thái thực tế của database trước khi dùng, không coi ghi chú cũ là trạng thái hiện tại.

Thông tin lần chạy:

| Nội dung | Điền khi kiểm thử |
|---|---|
| Người kiểm thử / ngày | |
| Phiên bản / thời gian build | |
| URL ứng dụng / trình duyệt | |
| Tên database thử nghiệm | |
| Migration cuối đã áp dụng | |
| Múi giờ | Asia/Ho_Chi_Minh (UTC+7) |
| Registration:Enabled / FitnessAI:Enabled | |
| PASS / FAIL / BLOCKED / N/A | |

## 2. Chuẩn bị dữ liệu và chạy kiểm tra tự động

Dùng Admin đã có; không có mật khẩu Admin mặc định được cung cấp trong checklist. Tạo hai hội viên thử nghiệm **M1**, **M2**, email khác nhau thuộc `@smartgym.com`, điện thoại khác nhau. M1 có ngày sinh đủ 18 tuổi và giới tính Male/Female. Chuẩn bị thêm một hồ sơ thiếu ngày sinh và một tài khoản bị khóa.

Tạo phòng **R1**, **R2** sức chứa 2; huấn luyện viên **T1**, **T2** hoạt động; gói **P1** một ngày giá 100.000 và **P30** 30 ngày giá 300.000. Cho M1/M2 mua gói đã thanh toán bao phủ ngày học. Dùng tên có tiền tố `TEST_` để dễ nhận biết.

Tạo các lớp riêng cho từng nhóm ca:

- **C1**: lớp tương lai, sức chứa 1, dùng kiểm tra chỗ cuối.
- **C2/C3**: dùng R1/T1 và R2/T2; ban đầu không trùng giờ; dùng kiểm tra lịch hội viên.
- **C4**: bắt đầu trong khoảng 30 phút tới; dùng điểm danh sớm và thử đổi lịch.
- **C5**: kết thúc chưa quá 2 giờ, còn một booking Booked chưa điểm danh; dùng hồi quy Completed.
- **C6**: đã kết thúc hơn 2 giờ, booking chưa điểm danh; dùng NoShow.

Tạo lớp quá khứ bằng cách lập lịch trước rồi chờ tới giờ, hoặc dùng dữ liệu lịch sử trên database thử nghiệm. Giao diện tạo lớp không cho tạo giờ quá khứ. Ca đúng biên thời gian nên đối chiếu bằng kiểm tra tự động vì việc bấm trình duyệt có độ trễ. Không đổi giờ hệ thống của máy đang vận hành.

Mở các cửa sổ trình duyệt/profiles riêng cho Admin, M1 và M2. Sau khi sửa mã, dừng phiên ứng dụng cũ và khởi động lại bản mới để kiểm thử giao diện. Nếu chỉ chạy kiểm tra tự động khi ứng dụng vẫn mở, dùng thư mục artifacts riêng:

```powershell
cd C:\Users\Admin\source\repos\SmartGymManagement
dotnet build SmartGymManagement.csproj --artifacts-path "$env:TEMP\SmartGymReviewArtifacts" -p:UseSharedCompilation=false
dotnet run --project Tests\SmartGymManagement.Checks.csproj --no-restore --artifacts-path "$env:TEMP\SmartGymReviewArtifacts" -p:UseSharedCompilation=false
```

Bộ checks không mở kết nối SQL và không khởi động ứng dụng web. Nó kiểm tra tính toán, quy tắc nghiệp vụ, model EF, khả năng dịch truy vấn SQL, QR và AI fallback; không thay thế kiểm thử giao diện/database. Cảnh báo NU1900 nghĩa là chưa lấy được dữ liệu kiểm tra lỗ hổng từ NuGet; ghi nhận riêng với lỗi biên dịch.

## 3. Trang công khai, đăng nhập và phân quyền

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| AUTH-01 | Chưa đăng nhập, mở `/`, `/Home/Privacy`. | Trang hiển thị, menu/liên kết hoạt động. | — |
| AUTH-02 | Đăng nhập Admin, sau đó M1 trong profile khác. | Đăng nhập được; menu phù hợp từng vai trò. | — |
| AUTH-03 | Nhập sai mật khẩu hoặc email không tồn tại. | Không đăng nhập; hiển thị thông báo, không lỗi 500. | — |
| AUTH-04 | Đăng xuất rồi mở lại URL yêu cầu đăng nhập. | Yêu cầu đăng nhập lại; không tiếp tục thao tác với phiên đã thoát. | — |
| AUTH-05 | Chưa đăng nhập mở `/Admin/Reports`, `/MemberDashboard`. | Chuyển đến đăng nhập; không lộ dữ liệu. | — |
| AUTH-06 | M1 nhập URL `/Admin/Members`, `/Admin/Reports/ExportAllReports`. | Bị từ chối; không tải báo cáo hoặc sửa dữ liệu Admin. | — |
| AUTH-07 | M1 đang đăng nhập; Admin khóa M1; M1 tải lại trang và thử đặt lớp. | Phiên bị chặn, không tạo booking mới; đăng nhập lại cũng bị chặn. | — |
| AUTH-08 | Gửi POST sửa dữ liệu thiếu/sai anti-forgery token bằng DevTools hoặc công cụ HTTP. | Bị từ chối; dữ liệu không đổi. | — |
| AUTH-09 | Truy cập Details/Edit với ID không tồn tại hoặc URL ID khác ID form. | 404/thông báo phù hợp; không sửa bản ghi khác. | — |
| AUTH-10 | Gửi hơn 30 request/phút tới Login/Register/AccountSetup từ cùng IP trên môi trường test. | Có HTTP 429 khi vượt giới hạn; có thể dùng lại sau cửa sổ giới hạn. | — |

Không coi Forgot Password/Confirm Email là luồng đã có email vận hành: dự án chưa cấu hình dịch vụ gửi email. Nếu thử các trang Identity mặc định này, ghi rõ giới hạn đó; không chờ nhận thư để đánh PASS.

## 4. Quản lý hội viên và thiết lập mật khẩu

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| MEM-01 | Admin tạo M1 tại `/Admin/Members/Create`, mở danh sách và Details. | Tạo một user + hồ sơ hội viên, thông tin đúng; có link đặt mật khẩu. | — |
| MEM-02 | Tạo hội viên với email trùng, ngoài `@smartgym.com`, điện thoại trùng; thử từng lỗi riêng. | Từ chối tương ứng; không tạo hồ sơ dở dang. | — |
| MEM-03 | Bỏ tên/email bắt buộc; nhập ngày sinh tương lai. | Báo lỗi nhập liệu; không lưu. | — |
| MEM-04 | Mở link setup, nhập mật khẩu ít nhất 10 ký tự có chữ thường và số, xác nhận đúng. | Đặt mật khẩu thành công; đăng nhập được. | — |
| MEM-05 | Setup với mật khẩu ngắn/thiếu số, xác nhận không khớp hoặc token sửa một ký tự. | Báo lỗi; không đặt mật khẩu. | — |
| MEM-06 | Dùng lại link sau khi đã đặt mật khẩu. | Bị từ chối; mật khẩu hiện tại không đổi. | — |
| MEM-07 | Với user chưa có mật khẩu, Admin cấp lại link; thử link cũ rồi link mới. | Link cũ vô hiệu; link mới hoạt động. | — |
| MEM-08 | Admin sửa tên/email/điện thoại/ngày sinh/giới tính/địa chỉ, mở lại Details. | Lưu đúng; kiểm tra trùng dữ liệu vẫn hoạt động. | — |
| MEM-09 | Admin chọn Delete/khóa một hội viên thử nghiệm. | Hội viên và tài khoản ngừng hoạt động; lịch sử vẫn còn. | — |
| MEM-10 | Với `Registration:Enabled=false`, mở và POST Register. | 404; không tạo tài khoản. | — |
| MEM-11 | Chỉ trên môi trường test, bật Registration; đăng ký email hợp lệ rồi đăng nhập. | Có user, role Member và hồ sơ; sai email/mật khẩu/trùng email bị từ chối. | — |

## 5. Phòng, huấn luyện viên và gói tập

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| CAT-01 | Tạo R1/R2, mở Details, sửa tên và sức chứa hợp lệ. | Danh sách/Details phản ánh dữ liệu mới. | — |
| CAT-02 | Phòng tên trùng; sức chứa 0 hoặc 31. | Từ chối, không tạo/sửa. | — |
| CAT-03 | Phòng có lớp Scheduled chưa kết thúc: tắt IsActive hoặc Delete. | Bị chặn; yêu cầu chuyển/hủy lớp trước. | — |
| CAT-04 | Phòng có lớp sức chứa 2: giảm phòng xuống 1. | Bị chặn nếu còn lớp Scheduled sức chứa lớn hơn. | — |
| CAT-05 | Phòng không còn lớp đang/sắp diễn ra: Delete. | Ngừng hoạt động; lịch sử không bị xóa; không dùng được cho lớp mới. | — |
| CAT-06 | Tạo T1/T2, mở Details, sửa chuyên môn/kinh nghiệm/liên hệ. | Lưu và hiển thị đúng. | — |
| CAT-07 | Trainer email/điện thoại trùng, email sai domain, kinh nghiệm -1 hoặc 101. | Báo lỗi; không lưu. | — |
| CAT-08 | Trainer có lớp Scheduled chưa kết thúc: đổi sang Inactive hoặc Delete. | Bị chặn; sau khi chuyển/hủy lớp thì ngừng hoạt động được. | — |
| CAT-09 | Tạo P1/P30, Details, sửa mô tả/giá/thời hạn. | Danh mục cập nhật đúng. | — |
| CAT-10 | Bỏ trống tên gói, thời hạn 0/3651, giá dưới 1.000; thử từng lỗi riêng. | Báo lỗi; không lưu. | — |
| CAT-11 | Ngừng cung cấp gói đã có người mua. | Không mua mới được; membership và giá mua lịch sử vẫn còn. | — |

## 6. Mua gói, thanh toán, hủy và hoàn tiền

Các nút xác nhận thanh toán/hoàn tiền ghi nhận thủ công; không tự chuyển tiền qua ngân hàng. Dùng biên nhận giả có tiền tố TEST trên database thử nghiệm.

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| PAY-01 | Mua P1, bắt đầu ngày D, tiền mặt đã xác nhận. | Active; ngày hết hạn = D; payment Paid, invoice Issued. | — |
| PAY-02 | Mua P30, bắt đầu D, tiền mặt đã xác nhận. | Hết hạn D+29 ngày; giá/tên/thời hạn mua được lưu. | — |
| PAY-03 | Mua chưa xác nhận hoặc thanh toán điện tử. | Membership/payment Pending, invoice Draft; chưa đặt lớp được. | — |
| PAY-04 | Khi mua bằng điện tử, cố đánh dấu PaymentConfirmed. | Bị từ chối; yêu cầu đối soát ở Details. | — |
| PAY-05 | Xác nhận payment Pending với mã biên nhận hợp lệ; gửi lại lần hai. | Lần đầu Paid, Active nếu còn hạn, invoice Issued; lần hai không thu/lưu lặp. | — |
| PAY-06 | Xác nhận với mã trống hoặc hơn 100 ký tự; hội viên đã khóa. | Bị từ chối; trạng thái không đổi. | — |
| PAY-07 | Mua gói thứ hai bắt đầu đúng ngày kết thúc gói Active/Pending hiện tại. | Bị chặn trùng ngày; bắt đầu ngày kế tiếp hợp lệ. | — |
| PAY-08 | Chọn ngày hôm qua hoặc quá 5 năm tới; chọn hội viên/gói ngừng hoạt động. | Báo lỗi tương ứng; không có giao dịch/hóa đơn mới. | — |
| PAY-09 | Sau khi mua P30, đổi tên/giá/thời hạn danh mục P30. | Details membership và Excel hiển thị snapshot mua cũ; payment Amount không đổi. | — |
| PAY-10 | Hủy membership Pending. | Cancelled; invoice Cancelled; payment Pending thành Failed. | — |
| PAY-11 | Hủy gói Active có booking tương lai không còn gói khác bao phủ. | Booking bị hủy; M1 nhận thông báo; payment đã thu vẫn Paid tới khi hoàn tiền. | — |
| PAY-12 | Hủy gói A; có booking nằm trong gói B Active khác của cùng hội viên. | Booking được gói B bao phủ vẫn giữ nguyên. | — |
| PAY-13 | ConfirmRefund cho gói đã hủy và có payment Paid, mã hợp lệ. | Payment Refunded, có RefundedAt; phản ánh ở báo cáo; không tự chuyển tiền. | — |
| PAY-14 | Hoàn gói chưa hủy, mã trống, hoặc gửi hoàn lần hai. | Bị từ chối; không ghi hoàn tiền lặp. | — |

## 7. Lịch lớp và ba ca đổi lịch quan trọng

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| CLS-01 | Admin tạo lớp tương lai với phòng/trainer hoạt động, mở Details rồi Edit. | Lưu đúng tên, giờ, phòng, trainer, capacity. | — |
| CLS-02 | Giờ kết thúc <= bắt đầu, dài hơn 3 giờ, bắt đầu quá khứ. | Từ chối tương ứng. | — |
| CLS-03 | Capacity 0/31 hoặc lớn hơn phòng; không chọn phòng; trainer inactive. | Báo lỗi; không lưu lớp. | — |
| CLS-04 | Tạo/sửa hai lớp trùng giờ cùng trainer hoặc cùng phòng; kiểm tra dropdown tài nguyên theo giờ. | Không cho trùng; tài nguyên bận không có trong danh sách khả dụng. | — |
| CLS-05 | Hai lớp tiếp giáp, lớp sau bắt đầu đúng giờ lớp trước kết thúc. | Cho phép nếu các điều kiện khác hợp lệ. | — |
| CLS-06 | Lớp giữ 2 chỗ, giảm capacity xuống 1. | Bị chặn; số chỗ giữ không vượt sức chứa. | — |
| CLS-07 | M1 đã đặt C2; đổi C2 sang giờ ngoài thời hạn gói của M1. | Bị chặn; lịch cũ không đổi. | — |
| CLS-08 | M1 có booking Booked C3; đổi C2 sang giờ trùng C3, dùng phòng/trainer khác nhau. | Bị chặn do lịch hội viên, không chỉ vì tài nguyên. | — |
| FIX-02 | M1 đặt C2 tương lai, đặt C4 sắp bắt đầu và điểm danh sớm C4 trong cửa sổ 30 phút. Đổi C2 sang một giờ còn ở tương lai nhưng trùng C4, dùng phòng/trainer khác. | **Bị chặn dù C4 đã Attended**; C2 giữ giờ cũ. Đổi C2 bắt đầu đúng EndTime C4 thì được nếu các điều kiện khác hợp lệ. | — |
| CLS-09 | Đổi giờ lớp đã có attendance. | Bị chặn, lịch sử giờ học không đổi. | — |
| CLS-10 | Hủy lớp có booking Booked qua Edit/Cancelled và qua Delete trên hai lớp riêng. | Class Cancelled; booking Cancelled; tạo thông báo cho hội viên. | — |
| CLS-11 | Hủy lớp đã có attendance; đánh Completed cho lớp chưa kết thúc. | Cả hai thao tác bị chặn. | — |

## 8. Booking Admin và hội viên

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| BKG-01 | M1 mở AvailableClasses, Book lớp đủ điều kiện, xem MyBookings và dashboard. | Tạo một booking; giảm chỗ trống; hiển thị đúng lớp và thời gian. | — |
| BKG-02 | Admin tạo booking cho M2, xem danh sách/Details và sửa Note. | Lưu đúng hội viên/lớp/ghi chú. | — |
| BKG-03 | Đặt khi lớp đầy, gói Pending/hết hạn/không phủ ngày lớp hoặc tài khoản inactive. | Bị chặn; có lý do; không thêm booking. | — |
| BKG-04 | Gửi lặp Book cùng lớp; đặt một lớp khác trùng giờ. | Không có hai booking giữ chỗ cùng lớp; không có lịch hội viên trùng giờ. | — |
| BKG-05 | M1 Cancel lớp tương lai, rồi đặt lại. | Booking cũ Cancelled; đặt lại được nếu còn chỗ và đủ điều kiện. | — |
| BKG-06 | M1 Cancel lớp đã bắt đầu hoặc booking đã Attended. | Bị chặn; trạng thái không đổi. | — |
| BKG-07 | M2 gửi Cancel/Qr với bookingId của M1. | Không truy cập/hủy/tạo QR của M1. | — |
| BKG-08 | Admin Cancel booking Booked; cố đổi Attended bằng Edit/Cancel. | Booked được hủy; Attended không được đổi trực tiếp. | — |
| BKG-09 | Admin phục hồi Cancelled/NoShow sang Booked khi lớp đầy/quá giờ/gói hết hạn; sau đó thử lớp hợp lệ. | Các ca không hợp lệ bị chặn; hợp lệ được phục hồi. | — |
| BKG-10 | Admin đổi Booked sang NoShow trước và sau EndTime+2 giờ. | Trước/đúng hạn bị chặn; chỉ sau hạn mới cho NoShow. | — |
| BKG-11 | M1/M2 đồng thời đặt C1 chỉ còn một chỗ bằng hai profiles. | Chính xác một người thành công; người còn lại bị từ chối; không vượt capacity. | — |

## 9. Điểm danh thủ công và QR

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| ATT-01 | Admin chọn booking, Method Manual, check-in trong cửa sổ; mở Details/list. | Một Attendance đúng hội viên/lớp; booking Attended; thời gian và phương thức đúng. | — |
| ATT-02 | Check-in trước StartTime-30 phút hoặc sau EndTime+2 giờ. | Bị chặn; không có attendance. | — |
| ATT-03 | Check-in booking Cancelled/NoShow, lớp Cancelled, hội viên khóa hoặc không còn gói phủ ngày lớp. | Bị chặn. | — |
| ATT-04 | Check-in lại cùng booking, kể cả hai request đồng thời từ hai tab Admin. | Chỉ một attendance; request còn lại không tạo lặp. | — |
| QR-01 | M1 mở Qr từ MyBookings cho booking của mình. | Có ảnh QR và mã dự phòng SGYM; không dùng dịch vụ QR bên ngoài. | — |
| QR-02 | Admin chọn Method QR, Booking = Select Booking (0), dán mã hợp lệ rồi submit. | Tự xác định booking từ token; check-in thành công trong cửa sổ. | — |
| QR-03 | Quét QR bằng camera trên trình duyệt hỗ trợ và HTTPS/localhost; từ chối quyền camera trên lần khác. | Quét điền token/phương thức, bấm Check In mới lưu; từ chối quyền có thông báo và có thể dán mã. | — |
| QR-04 | Sửa một ký tự token; dùng mã `12:34`; chọn booking khác với token. | Bị từ chối; không có attendance. | — |
| QR-05 | Chờ QR cũ hơn 10 phút, thử dùng; tải lại trang Qr lấy token mới. | Token cũ hết hạn; token mới hợp lệ nếu booking còn đủ điều kiện. | — |
| QR-06 | Dùng lại QR sau khi điểm danh; thử Qr của booking đã hủy. | Không điểm danh lặp; không cấp QR cho booking đã hủy. | — |
| FIX-01 | C5 có booking Booked chưa điểm danh, EndTime cách hiện tại <2 giờ. Admin chuyển C5 sang Completed. M1 lấy QR; Admin check-in. Lặp bằng Manual trên booking riêng. | **Completed vẫn cấp QR và chấp nhận Manual/QR trong hạn**. Sau EndTime+2 giờ, cả cấp QR và check-in bị chặn. | — |

## 10. Dashboard, hồ sơ và thông báo hội viên

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| PRO-01 | M1 mở `/MemberDashboard`, so sánh với membership, booking và BMI riêng của M1. | Thông tin/tóm tắt/liên kết đúng; không có dữ liệu M2. | — |
| PRO-02 | M1 sửa tên, điện thoại, ngày sinh, giới tính, địa chỉ và liên hệ khẩn cấp tại `/MemberProfile`. | Lưu đúng; mở lại còn dữ liệu; dashboard cập nhật. | — |
| PRO-03 | Ngày sinh tương lai, điện thoại trùng M2/sai định dạng, giới tính sai, tên trống. | Từ chối tương ứng; không lưu dữ liệu lỗi. | — |
| PRO-04 | Admin hủy lớp/gói làm hủy booking M1; M1 mở `/MemberNotifications`. | Thông báo đúng M1 và nội dung; M2 không thấy thông báo đó. | — |

## 11. BMI và báo cáo fitness

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| BMI-01 | Hồ sơ thiếu ngày sinh/giới tính hoặc chưa đủ 18 tuổi: mở Create. | Yêu cầu cập nhật hồ sơ hoặc báo giới hạn tuổi; không tạo assessment. | — |
| BMI-02 | Nhập cao 170 cm, nặng 70 kg, giới tính hợp lệ, mức hoạt động và mục tiêu; lưu và xem Details. | BMI 24,22; phân loại Normal; có các chỉ số và hướng dẫn phù hợp dữ liệu. | — |
| BMI-03 | Với nam 30 tuổi, 170/70 và ModeratelyActive/MaintainWeight. | BMR 1.617,50; TDEE và calories mục tiêu 2.507,13 (hiển thị có thể làm tròn). | — |
| BMI-04 | Nhập cao 99/251, nặng 19/301; vòng cổ/eo/mông ngoài khoảng cho phép; thử từng lỗi. | Báo lỗi; không lưu. | — |
| BMI-05 | Để trống toàn bộ số đo vòng; sau đó thử số đo thiếu hoặc chênh lệch gây công thức không hợp lệ. | Dữ liệu cơ bản vẫn dùng được khi số đo tùy chọn thiếu; không có NaN/Infinity/lỗi 500; đầu vào sai bị báo hoặc chỉ số không tính được để trống. | — |
| BMI-06 | Thử LoseWeight/GainMuscle, các mức hoạt động và hồ sơ nữ có số đo hợp lệ. | Kết quả thay đổi theo lựa chọn; macro tương ứng calories; số liệu lưu và xem lại đúng. | — |
| BMI-07 | M1 tạo nhiều bản ghi, xem lịch sử, Details, xác nhận Delete một bản ghi. | Sắp theo thời gian; xóa đúng bản ghi được chọn; các bản ghi khác còn. | — |
| BMI-08 | M2 mở Details/Delete và gửi POST Delete với ID bản ghi M1. | Bị từ chối/404; dữ liệu M1 không bị xem hoặc xóa. | — |

## 12. Chatbot và widget

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| CHAT-01 | M1 mở trang chatbot và widget; gửi câu hỏi, tải lại. | Có phản hồi, lịch sử lưu đúng tài khoản; widget hoạt động. | — |
| CHAT-02 | Hỏi “Tôi muốn giảm cân”, “TOI MUON GIAM CAN”, “How can I lose weight?”. | Trả lời kiến thức fitness; không nhầm thành yêu cầu BMI cá nhân. | — |
| CHAT-03 | Hỏi gói còn bao nhiêu ngày, lớp sắp tới, BMI mới nhất và mục tiêu protein; thử tài khoản chưa có dữ liệu. | Dùng đúng dữ liệu cá nhân; khi thiếu dữ liệu nói rõ, không bịa chỉ số. | — |
| CHAT-04 | Gửi rỗng hoặc hơn 1.000 ký tự; bấm gửi lặp nhanh. | Đầu vào không hợp lệ được xử lý; không lỗi giao diện hoặc lưu nội dung vượt giới hạn. | — |
| CHAT-05 | Gửi hơn 10 câu/phút; sau cửa sổ 1 phút thử lại. | Request vượt giới hạn trả 429, UI báo phù hợp; sau đó gửi lại được. | — |
| CHAT-06 | Xóa lịch sử M1, tải lại; kiểm tra M2. | M1 hết lịch sử; M2 không bị xóa. | — |
| CHAT-07 | AI tắt hoặc provider lỗi/timeout/429 trên môi trường test. | Có fallback; không làm hỏng luồng chat; không hiển thị key/lỗi nội bộ. | — |
| CHAT-08 | Nếu có cấu hình AI test được phép dùng: bật và hỏi câu hợp lệ; kiểm tra không trộn lịch sử M1/M2. | Phản hồi provider hoặc fallback phù hợp; chỉ dùng lịch sử đúng hội viên. Nếu không cấu hình, ghi N/A. | — |
| CHAT-09 | Cấu hình HistoryLimit=20 trên test; gửi 21 câu hợp lệ, chia thành nhiều phút để tránh rate limit. | Chỉ giữ tối đa 20 cuộc trao đổi của M1; M2 không bị ảnh hưởng. | — |

## 13. Báo cáo, cập nhật trạng thái và Excel

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| RPT-01 | Admin mở Reports; chọn khoảng ngày có dữ liệu và không có dữ liệu. | Số liệu/biểu đồ/top lớp/top gói tương ứng; kỳ rỗng không lỗi chia cho 0. | — |
| RPT-02 | Đối chiếu payment Paid/Refunded, booking và attendance trong kỳ bằng dữ liệu thử đã ghi lại. | Thu vào tính theo PaymentDate; hoàn theo RefundedAt (dữ liệu cũ null dùng PaymentDate); ròng = thu - hoàn. | — |
| RPT-03 | Đặt from > to, to tương lai, khoảng quá 5 năm; thử ngày biên đầu và cuối kỳ. | Khoảng được chuẩn hóa; from bao gồm 00:00, hết ngày to được tính, 00:00 hôm sau bị loại. | — |
| RPT-04 | Có booking Booked quá hạn điểm danh nhưng chưa chạy UpdateStatuses; xem AttendanceRate. | Booking đó được tính trong mẫu số; không phải mở report là tự chuyển NoShow. | — |
| RPT-05 | Có membership Active hết hạn và C6 quá hạn; bấm UpdateStatuses. | Membership Expired, booking chưa attendance thành NoShow; không đổi booking đã Attended/Cancelled. | — |
| RPT-06 | Chỉ tải lại Reports hoặc khởi động ứng dụng với seed tắt, không bấm cập nhật. | Không tự chạy thao tác đổi trạng thái dữ liệu. | — |
| XLS-01 | ExportAllReports với kỳ đã chọn, mở bằng Excel. | Có Summary, Members, Memberships, Bookings, Attendances, Payments; mở được, ngày/tiền rõ ràng. | — |
| XLS-02 | Đối chiếu Summary với Reports cùng kỳ; kiểm tra các sheet chi tiết. | Số liệu khớp. Members lọc JoinDate, Memberships lọc CreatedAt, Bookings lọc BookingDate, Attendances lọc CheckInTime. | — |
| FIX-03 | Database test có payment thu tháng trước, hoàn tháng này. Export kỳ tháng này; rồi kỳ tháng trước. | Kỳ hoàn có payment trong Payments, Received In Period=0, Refunded In Period=Amount. Kỳ thu có Received In Period=Amount, Refunded In Period=0. Refunded At đúng. | — |
| XLS-03 | Thu và hoàn cùng kỳ; đối chiếu hai cột Received In Period / Refunded In Period với Summary. | Một dòng payment, hai cột bằng Amount; tổng hai cột lần lượt bằng thu vào/hoàn tiền của Summary. | — |
| XLS-04 | Payment Refunded cũ có RefundedAt null và PaymentDate trong kỳ. | Dòng vẫn xuất; cột Refunded At trống, khoản hoàn tính theo PaymentDate để khớp Summary. | — |
| XLS-05 | Đổi tên/giá gói sau lần mua mới rồi xuất Excel. | Memberships và Payments dùng tên snapshot mua; giá mua không bị ghi đè bởi danh mục mới. | — |
| XLS-06 | Nếu có dataset test >50.000 dòng tổng các sheet: export, sau đó thu hẹp kỳ. | Kỳ quá lớn bị từ chối; kỳ nhỏ xuất được. Giao dịch chỉ có ngày hoàn trong kỳ cũng được tính vào giới hạn. | — |

Để chuẩn bị FIX-03, dùng giao dịch lịch sử của bản sao database và hoàn qua giao diện; nếu chưa có, nhờ người quản lý dữ liệu chuẩn bị fixture trên database test. Không sửa trực tiếp ngày giao dịch của database vận hành để tạo ca thử.

## 14. Tìm kiếm, lọc, phân trang và giao diện chung

| Mã | Thao tác / dữ liệu | Kết quả mong đợi | KQ |
|---|---|---|---|
| UI-01 | Với mỗi danh sách Admin có ô search/status: tìm tên/email theo ô hỗ trợ, lọc trạng thái, xóa lọc. | Kết quả đúng điều kiện; bỏ lọc trở lại danh sách. | — |
| UI-02 | Chuẩn bị >20 bản ghi cho từng danh sách có phân trang; đi trang 1/2/cuối; dùng `page=0`, số rất lớn, chuỗi. | Phân trang hoạt động; giá trị trang được chuẩn hóa; không lỗi 500. | — |
| UI-03 | Đổi trang khi đang search/status; kiểm tra MyBookings, AvailableClasses, BMI, Notifications. | Giữ bộ lọc áp dụng; dữ liệu đúng hội viên và không mất bản ghi bất thường. | — |
| UI-04 | Mở trên desktop và màn hình điện thoại; menu, form, bảng, QR, chatbot, biểu đồ. | Có thể đọc/thao tác, không bị che nút; bảng rộng cuộn được nếu cần. | — |
| UI-05 | Nhập tên/Note có dấu tiếng Việt và chuỗi `<script>alert(1)</script>`. | Tiếng Việt hiển thị đúng; nội dung được hiển thị như văn bản, không chạy script. | — |
| UI-06 | Mở form sai dữ liệu, sửa rồi submit; kiểm tra thông báo thành công/lỗi và nút Back. | Lỗi dễ hiểu, dữ liệu nhập còn để sửa; Back về đúng luồng. | — |

## 15. Mẫu ghi lỗi và điều kiện hoàn tất

```text
Mã ca:
Phiên bản / thời gian build:
URL / trình duyệt:
Tài khoản / vai trò (không ghi mật khẩu):
Dữ liệu thử / ID liên quan:
Bước tái hiện:
1.
2.
Kết quả mong đợi:
Kết quả thực tế:
HTTP status / thông báo / log:
Ảnh hoặc đường dẫn bằng chứng:
Mức độ: chặn sử dụng / sai dữ liệu / giao diện
Trạng thái sau khi kiểm tra lại:
```

Hoàn tất một lượt test khi đã ghi kết quả cho tất cả các ca, xử lý các FAIL chặn sử dụng/sai dữ liệu, và chạy lại FIX-01, FIX-02, FIX-03 cùng các ca bị ảnh hưởng. Ghi rõ N/A/BLOCKED và lý do, không tính thành PASS. Các ca yêu cầu database, camera, nhiều người dùng hoặc provider AI phải được thực hiện trên môi trường tương ứng; kết quả bộ checks offline không chứng minh các ca đó đã đạt.
