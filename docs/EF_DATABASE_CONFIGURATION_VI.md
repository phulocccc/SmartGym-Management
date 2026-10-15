# Cấu hình database EF Core — 09/10/2026

## Nguyên nhân và bản sửa

Factory cũ `DesignTimeDbContextFactory` hard-code `localhost` / `SmartGymDesignTimeOnly`. EF Tools ưu tiên factory khi có `IDesignTimeDbContextFactory<ApplicationDbContext>`, nên không lấy cấu hình context từ đăng ký DI trong Program. Giá trị hiển thị ở `dotnet ef dbcontext info` là đích được cấu hình, không chứng minh database giả đã tồn tại hay được tạo.

Runtime trong `Program.cs` vốn dùng `ConnectionStrings:DefaultConnection`. `appsettings.json` đã chỉ định đúng server `(localdb)\MSSQLLocalDB`, database `SmartGymDb`; `appsettings.Development.json` không ghi đè khóa này. `ApplicationDbContext` chỉ nhận options trong constructor, không có `OnConfiguring` đổi provider/kết nối. Launch profiles http/https chỉ đặt môi trường Development và URL, không đặt connection string.

Bản sửa cho factory dùng `WebApplication.CreateBuilder` để đọc các provider cấu hình mặc định giống runtime, và dùng tên assembly ứng dụng để nạp đúng User Secrets. Factory không gọi `Build`/`Run` hay chạy seeder. Program và factory đều dùng `DatabaseConfiguration.Configure` để chọn SQL Server với cùng khóa cấu hình; khi thiếu giá trị, báo tên khóa cần cấu hình mà không in connection string. JSON hiện có được giữ nguyên. Kết nối giả chỉ còn trong tests offline.

## Thứ tự ghi đè

Với cùng môi trường, thư mục cấu hình và tham số, thứ tự ưu tiên từ cao xuống thấp:

1. Tham số dòng lệnh.
2. Biến môi trường, ví dụ `ConnectionStrings__DefaultConnection`.
3. User Secrets khi Development.
4. `appsettings.{Environment}.json`.
5. `appsettings.json`.

Ở lần kiểm tra này, không phát hiện các override kết nối liên quan trong scope Process/User/Machine và không có file User Secrets của dự án. Không kiểm tra được môi trường riêng đang được kế thừa bởi tiến trình Visual Studio; nếu đã thay đổi biến môi trường bên ngoài VS, cần khởi động lại VS để tiến trình nhận giá trị mới. Không dùng `dotnet user-secrets list` hoặc dump toàn bộ biến môi trường trong báo cáo vì có thể lộ bí mật.

`launchSettings.json` đặt Development cho lần chạy theo launch profile; không nên dựa vào file này để mặc định môi trường cho tất cả lệnh EF. Truyền môi trường rõ ràng khi kiểm tra, đặc biệt nếu sau này thêm connection override trong Development/User Secrets.

## Kiểm tra an toàn

Chạy từ thư mục `C:\Users\Admin\source\repos\SmartGymManagement`:

```powershell
dotnet build SmartGymManagement.csproj --no-restore -m:1 -p:UseSharedCompilation=false
dotnet ef dbcontext info --no-build --context ApplicationDbContext -- --environment Development
dotnet ef dbcontext info --no-build --context ApplicationDbContext -- --environment Production
dotnet run --project Tests\SmartGymManagement.Checks.csproj --no-restore -p:UseSharedCompilation=false
```

Hai lệnh info đã được kiểm tra và đều trả:

```text
Provider name: Microsoft.EntityFrameworkCore.SqlServer
Database name: SmartGymDb
Data source: (localdb)\MSSQLLocalDB
```

`--no-build` chỉ dùng assembly đã build: sau khi sửa factory cần build lại trước, nếu không có thể thấy kết quả cũ. Tests kiểm tra context giả riêng, override qua environment/CLI, lỗi thiếu cấu hình, và trạng thái kết nối vẫn đóng.

Không chạy `database update`, `database drop`, `EnsureCreated`, `EnsureDeleted`, `Migrate`, seeder hoặc SQL thay đổi dữ liệu trong lần sửa này. Không có migration/model/schema mới. Chọn đúng đích trong cấu hình không tự tạo, thay thế hoặc nâng cấp database. Kết quả info cũng không xác nhận Windows credentials, quyền LocalDB hoặc schema thực tế đã phù hợp; lỗi kết nối phải được kiểm tra riêng bằng thao tác chỉ đọc.

## Tài liệu tham khảo

- [EF Core: Design-time DbContext creation](https://learn.microsoft.com/ef/core/miscellaneous/cli/dbcontext-creation)
- [ASP.NET Core: Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
