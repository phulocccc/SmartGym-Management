$ErrorActionPreference = 'Stop'
$cases = @(Import-Csv -LiteralPath 'docs\TESTCASE_VI.csv' -Encoding UTF8)
$http = Get-Content -LiteralPath 'docs\TEST_RUN_HTTP.json' -Raw -Encoding UTF8 | ConvertFrom-Json
$httpRows = @($http | ForEach-Object { $_ })
if ($cases.Count -ne 120 -or $httpRows.Count -ne 33 -or @($httpRows | Where-Object { !$_.Pass }).Count -ne 0) {
    throw 'Unexpected testcase count or HTTP evidence. Do not publish results.'
}
$offline = Get-Content -LiteralPath 'docs\TEST_RUN_AUTOMATED.log' -Raw -Encoding UTF8
if (!$offline.Contains('78 checks passed.')) { throw 'Automated evidence is incomplete.' }
$reference = Get-Content -LiteralPath 'docs\TEST_RUN_TESTCASE_REFERENCE.log' -Raw -Encoding UTF8
if (!$reference.Contains('TDEE=2507.12') -or !$reference.Contains('matches=False')) { throw 'BMI reference evidence missing.' }
$pass = @{
    'AUTH-01' = 'HTTP GET / và /Home/Privacy trả 200, nội dung SmartGym/Privacy đúng; CSS và JS trả 200. Chưa đánh giá bố cục bằng trình duyệt.'
    'AUTH-05' = '9 trang Admin, 7 trang hội viên, export và 3 POST bảo vệ đều trả 302 tới /Identity/Account/Login khi chưa đăng nhập.'
    'AUTH-08' = 'POST /AccountSetup thiếu token và token sai đều trả 400. Request không vào bước sửa dữ liệu.'
    'AUTH-10' = '35 GET liên tiếp tới AccountSetup: 24 trả 200, 11 trả 429 (6 request account đã dùng quota trước đó). Sau hơn 60 giây request lại trả 200.'
    'MEM-10' = 'Registration:Enabled=false: GET và POST Register có anti-forgery token hợp lệ đều trả 404.'
}
$partial = @{
    'PAY-01' = 'Offline: quy tắc gói một ngày có ngày kết thúc bằng ngày bắt đầu đạt.'
    'PAY-02' = 'Offline: quy tắc 30 ngày kết thúc D+29 đạt.'
    'PAY-07' = 'Offline: cùng ngày biên trùng; ngày kế tiếp không trùng đạt.'
    'CLS-05' = 'Offline: khoảng thời gian tiếp giáp không bị coi trùng đạt.'
    'CLS-08' = 'Offline: booking Booked giữ khoảng thời gian trùng đạt.'
    'FIX-02' = 'Offline: phát hiện booking Attended trùng giờ, loại chính lớp đang sửa và hội viên khác; truy vấn dịch được sang SQL.'
    'BKG-08' = 'Offline: quy tắc không đổi Attended/booking đã có attendance đạt.'
    'BKG-10' = 'Offline: NoShow trước/đúng EndTime+2 giờ bị chặn, sau hạn được phép đạt.'
    'ATT-02' = 'Offline: biên StartTime-30 phút và EndTime+2 giờ được phép; ngoài biên bị chặn đạt.'
    'ATT-04' = 'Offline: EF model có unique index trên Attendance.BookingId. Chưa chạy hai request SQL đồng thời.'
    'QR-01' = 'Offline: QR tạo PNG tại chỗ đúng signature, token chứa đúng booking và hội viên đạt.'
    'QR-04' = 'Offline: token sửa một ký tự và mã không ký bị từ chối đạt. Chưa thử form chọn booking khác.'
    'QR-05' = 'Offline: token hết hạn bị từ chối; token hợp lệ đọc được đạt. Chưa chạy luồng trình duyệt chờ 10 phút.'
    'FIX-01' = 'Offline: Completed được điểm danh tới đúng EndTime+2 giờ; quá hạn bị chặn, Cancelled/unknown bị chặn. Chưa check-in lưu SQL hoặc mở QR bằng trình duyệt.'
    'BMI-02' = 'Offline: 170/70 trả BMI 24,22; phân loại và biên BMI đạt. Chưa lưu/xem bản ghi.'
    'BMI-04' = 'Offline: chiều cao 0 bị từ chối bởi hàm tính. Chưa kiểm tra toàn bộ validation form và việc không lưu.'
    'BMI-05' = 'Offline: số đo không đầy đủ/đầu vào logarithm không hợp lệ và body fat ngoài giới hạn trả null đạt.'
    'BMI-06' = 'Offline: BMR nam/nữ, TDEE, calories GainMuscle, macro và giới hạn calories đạt; chưa kiểm tra toàn bộ lựa chọn UI.'
    'CHAT-02' = 'Offline: bộ phân loại câu giảm cân Việt/Anh/không dấu trả Knowledge đạt; chưa kiểm thử phản hồi và lịch sử qua HTTP có đăng nhập.'
    'CHAT-03' = 'Offline: phân loại intent membership, BMI, bookings, protein đạt; chưa xác minh dữ liệu riêng đúng người từ SQL.'
    'CHAT-07' = 'Offline: AI tắt không gọi provider; provider 429/malformed trả null cho fallback đạt. Chưa kiểm tra UI/timeout thực tế.'
    'RPT-03' = 'Offline: khoảng from > to được chuẩn hóa; export dùng khoảng nửa mở và biên cuối ngày đạt. Chưa đối chiếu toàn bộ báo cáo với SQL.'
    'FIX-03' = 'Offline: export lấy giao dịch thu ngoài kỳ/hoàn trong kỳ; predicate dịch được sang SQL. Chưa xuất/mở workbook đối chiếu Summary.'
    'XLS-03' = 'Offline: thu và hoàn cùng kỳ chỉ lấy một giao dịch đạt. Chưa kiểm tra ô Excel và tổng các cột.'
    'XLS-04' = 'Offline: RefundedAt null dùng PaymentDate; truy vấn COALESCE dịch được sang SQL đạt.'
}
$day = Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'
foreach ($case in $cases) {
    $id = $case.'Mã testcase'
    $case.'Người test' = 'Codex'
    $case.'Ngày test' = $day
    $case.'Trạng thái' = 'BLOCKED'
    $case.'Mã lỗi / bằng chứng' = 'ENV-DB; xem TEST_RUN_RESULTS_VI.md'
    $case.'Kết quả thực tế' = 'Chưa thực hiện toàn bộ ca: kết nối SmartGymDb LocalDB thất bại. Không có tài khoản/fixture thử nghiệm độc lập để thực hiện luồng đăng nhập và ghi dữ liệu.'
    $case.'Ghi chú' = 'Không coi build hoặc kiểm tra offline là PASS cho luồng giao diện/database.'
    if ($id -like 'UI-*' -or $id -eq 'QR-03') {
        $case.'Mã lỗi / bằng chứng' = 'ENV-BROWSER; ENV-DB'
        $case.'Kết quả thực tế' = 'Không có browser/app surface để kiểm tra giao diện, responsive hoặc camera; database cũng không kết nối được.'
    }
    if ($id -eq 'CHAT-08') {
        $case.'Mã lỗi / bằng chứng' = 'ENV-AI; ENV-DB'
        $case.'Kết quả thực tế' = 'Phiên test tắt AI; chưa có cấu hình provider thử nghiệm và dữ liệu hội viên. Chưa gọi provider thật.'
    }
    if ($partial.ContainsKey($id)) {
        $case.'Kết quả thực tế' = $partial[$id] + ' Toàn bộ testcase còn BLOCKED do thiếu điều kiện chạy thực tế.'
        $case.'Mã lỗi / bằng chứng' = 'TEST_RUN_AUTOMATED.log; ENV-DB'
    }
    if ($pass.ContainsKey($id)) {
        $case.'Trạng thái' = 'PASS'
        $case.'Kết quả thực tế' = $pass[$id]
        $case.'Mã lỗi / bằng chứng' = 'TEST_RUN_HTTP.json'
        $case.'Ghi chú' = 'Phạm vi PASS: HTTP; chưa kiểm tra trình bày bằng trình duyệt.'
    }
    if ($id -eq 'BMI-03') {
        $case.'Trạng thái' = 'FAIL'
        $case.'Kết quả thực tế' = 'Chạy trực tiếp FitnessCalculationService: BMR=1617,50; TDEE=2507,12; target=2507,12, khác expected 2507,13 trong testcase.'
        $case.'Mã lỗi / bằng chứng' = 'TC-EXPECTED-01; TEST_RUN_TESTCASE_REFERENCE.log'
        $case.'Ghi chú' = 'Lỗi số mong đợi trong testcase đã soạn: Math.Round(decimal,2) dùng ToEven. Chưa kết luận lỗi ứng dụng; UI/database vẫn chưa kiểm thử.'
    }
}
$cases | Export-Csv -LiteralPath 'docs\TESTCASE_RESULTS_VI.csv' -NoTypeInformation -Encoding UTF8
$saved = @(Import-Csv -LiteralPath 'docs\TESTCASE_RESULTS_VI.csv' -Encoding UTF8)
if ($saved.Count -ne 120 -or @($saved | Where-Object { !$_.'Kết quả thực tế' }).Count -gt 0) { throw 'Missing results.' }
$saved | Group-Object 'Trạng thái' | Select-Object Name,Count | Format-Table
