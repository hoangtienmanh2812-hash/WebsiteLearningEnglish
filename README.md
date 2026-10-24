# Dự án TTCSN - Nhóm 9 (Website Learning English)

Dự án website học tiếng Anh với các công nghệ:
- **Frontend**: React + TypeScript + Vite + Tailwind CSS
- **Backend**: C# ASP.NET Core 8.0
- **Database**: SQL Server (Entity Framework Core)

## 🏗 Kiến trúc Dự án

Dự án đã được tái cấu trúc lại từ đầu để đảm bảo tính mở rộng và dễ bảo trì cho ứng dụng lớn:
- **Backend**: Sử dụng **Clean Architecture**.
- **Frontend**: Sử dụng **Feature-Based Architecture**.

---

## 🛠 Quá trình Khởi tạo & Cấu hình (Setup Guide)

Dưới đây là chi tiết các bước đã thực hiện để khởi tạo dự án. Bạn có thể tham khảo để hiểu rõ dự án được tạo ra và cấu hình như thế nào.

### 1. Khởi tạo Backend (Clean Architecture)

Backend được chia thành 4 lớp (layers) chính, tuân thủ chặt chẽ nguyên tắc Dependency Inversion:
- **Domain**: Chứa Entities, Enums, Value Objects. (Tuyệt đối KHÔNG phụ thuộc vào bất kỳ layer nào khác, kể cả EF Core hay ASP.NET).
- **Application**: Chứa Use Cases, Interfaces (IRepository), DTOs. (Chỉ phụ thuộc vào `Domain`).
- **Infrastructure**: Chứa cấu hình EF Core, DbContext, Authentication (JWT). (Phụ thuộc vào `Application` và `Domain`).
- **API**: Chứa Controllers, Dependency Injection, Middleware. (Phụ thuộc vào `Application` và `Infrastructure`).

**Các lệnh cấu hình đã thực hiện (bằng .NET CLI):**
```powershell
# 1. Tạo Solution
dotnet new sln -n EnglishLearning

# 2. Khởi tạo các project (Web API & Class Library)
cd backend/src
dotnet new webapi -n EnglishLearning.API -f net8.0
dotnet new classlib -n EnglishLearning.Application -f net8.0
dotnet new classlib -n EnglishLearning.Domain -f net8.0
dotnet new classlib -n EnglishLearning.Infrastructure -f net8.0

# 3. Khởi tạo các project Test
cd ../../tests
dotnet new xunit -n EnglishLearning.UnitTests -f net8.0
dotnet new xunit -n EnglishLearning.IntegrationTests -f net8.0

# 4. Gom các project vào chung Solution
cd ../backend
dotnet sln add "src\EnglishLearning.API\EnglishLearning.API.csproj"
dotnet sln add "src\EnglishLearning.Application\EnglishLearning.Application.csproj"
dotnet sln add "src\EnglishLearning.Domain\EnglishLearning.Domain.csproj"
dotnet sln add "src\EnglishLearning.Infrastructure\EnglishLearning.Infrastructure.csproj"
dotnet sln add "..\tests\EnglishLearning.UnitTests\EnglishLearning.UnitTests.csproj"
dotnet sln add "..\tests\EnglishLearning.IntegrationTests\EnglishLearning.IntegrationTests.csproj"

# 5. Cấu hình luồng Dependency (References)
cd src
dotnet add "EnglishLearning.API\EnglishLearning.API.csproj" reference "EnglishLearning.Application\EnglishLearning.Application.csproj" "EnglishLearning.Infrastructure\EnglishLearning.Infrastructure.csproj"
dotnet add "EnglishLearning.Application\EnglishLearning.Application.csproj" reference "EnglishLearning.Domain\EnglishLearning.Domain.csproj"
dotnet add "EnglishLearning.Infrastructure\EnglishLearning.Infrastructure.csproj" reference "EnglishLearning.Application\EnglishLearning.Application.csproj" "EnglishLearning.Domain\EnglishLearning.Domain.csproj"
```

### 2. Khởi tạo Frontend (Feature-Based Architecture)

Frontend sử dụng **Vite** để build nhanh hơn thay vì CRA cũ. Thay vì nhóm các file theo loại kĩ thuật (tất cả component để chung 1 chỗ), dự án nhóm theo **Feature** (Tính năng) để dễ scale.

**Các lệnh cấu hình đã thực hiện:**
```bash
# 1. Khởi tạo Vite React với TypeScript
npm create vite@latest frontend -- --template react-ts
cd frontend

# 2. Cài đặt các thư viện thiết yếu
npm install
npm install react-router-dom axios

# 3. Cài đặt Tailwind CSS v4 mới nhất cùng Vite Plugin
npm install -D tailwindcss @tailwindcss/vite postcss autoprefixer
```

**Cấu trúc thư mục mới:**
- `src/features/`: Chứa module các tính năng chính của app. (VD: `auth/`, `lessons/`, `vocabulary/`, `exercises/`, `progress/`, `gamification/`, `leaderboard/`, `chatbot/`, `profile/`). Mỗi feature sẽ độc lập, chứa các components, hooks và API riêng của nó.
- `src/components/ui/`: Các UI component cơ bản/dumb component (Button, Input, Card).
- `src/components/common/`: Các component dùng chung có logic nhẹ (Header, Footer, Sidebar).
- `src/layouts/`: Các layout bao bọc trang (MainLayout, AuthLayout).
- `src/services/`, `src/hooks/`, `src/contexts/`, `src/utils/`, `src/constants/`, `src/config/`: Chứa code logic dùng chung toàn cục.

---

## 🚀 Hướng dẫn chạy dự án local

### Chạy Backend
Mở Terminal, di chuyển vào thư mục `backend` và chạy:
```bash
cd backend
dotnet restore
dotnet run --project src/EnglishLearning.API/EnglishLearning.API.csproj
```
*(Backend API thường sẽ chạy ở cổng `http://localhost:5xxx` hoặc `https://localhost:7xxx`)*

#### Cấp quyền quản trị nội dung Google Drive

Đặt `Jwt__Secret` thành chuỗi ngẫu nhiên riêng tư có ít nhất 32 byte bằng user-secrets hoặc biến môi trường; không ghi secret vào Git và giữ nguyên giá trị qua các lần khởi động để JWT hiện tại còn hiệu lực. Đăng ký tài khoản người dùng bình thường trước, sau đó thêm email đó vào `GoogleDrive:AdminEmails` trong cấu hình backend (ví dụ: `"AdminEmails": ["admin@example.com"]`) hoặc đặt biến môi trường `GoogleDrive__AdminEmails__0`. API quản trị kiểm tra role trong JWT.

Google Drive được truy cập bằng OAuth của tài khoản quản trị, không dùng Service Account (Service Account không có dung lượng Drive cá nhân). Trong Google Cloud Console, bật Google Drive API, cấu hình OAuth consent screen và tạo OAuth Client ID loại **Desktop app**. Nếu ứng dụng đang ở chế độ Testing, thêm `hoangtienmanh2812@gmail.com` vào danh sách Test users. Tải file client credentials về máy chạy backend tại đường dẫn `C:\secrets\google-oauth-client.json`, hoặc đặt `GoogleDrive__OAuthCredentialsPath` trỏ đến file đó. Không commit file credentials hoặc token vào Git.

Khởi động backend và mở trang quản trị `/admin/lessons`; lần đầu truy cập Drive, cửa sổ trình duyệt sẽ yêu cầu đăng nhập và cấp quyền Google Drive cho tài khoản Google. Đăng nhập bằng tài khoản có quyền trên thư mục gốc đã cấu hình. OAuth token được lưu cục bộ trong `%LOCALAPPDATA%\EnglishLearning\GoogleDriveTokens`.

Thông tin `Title`, `DriveUrl` và `LessonType` được lưu trong SQL Server; nội dung JSON hiện có tiếp tục nằm trong các file Drive tại chỗ. Các thao tác chỉ áp dụng với file JSON bên trong thư mục gốc Google Drive đã cấu hình.

#### Quản lý người dùng

Admin có thể truy cập `/admin/users` để tìm kiếm/lọc người dùng, xem XP, streak và tiến trình bài học, chỉnh sửa tên/email/vai trò và đặt mật khẩu mới (để trống mật khẩu nếu không muốn thay đổi). Mật khẩu mới được lưu dưới dạng BCrypt hash, cần ít nhất 8 ký tự và tối đa 72 byte UTF-8. Migration `AddUserAdminRole` bổ sung cột `Users.IsAdmin` nếu chưa có; chạy `dotnet ef database update --project src/EnglishLearning.Infrastructure --startup-project src/EnglishLearning.API` từ thư mục `backend` để áp dụng migration. Admin không thể tự hạ quyền tài khoản đang đăng nhập. Chức năng khóa/mở khóa tài khoản chưa được triển khai.

#### Quản lý hệ thống dành cho Admin

Admin đã được cấp quyền qua `Users.IsAdmin` hoặc `GoogleDrive:AdminEmails` có thể truy cập `/admin/system` để chỉnh XP từ vựng, hoàn thành bài học, trả lời đúng và điểm danh streak (mỗi quy tắc từ 0–10000), xem nhật ký thay đổi cấu hình/sao lưu và lỗi máy chủ chưa được xử lý, và tải bản sao SQL Server gốc được đóng gói thành ZIP. Quy tắc được lưu trong `SystemSettings` (`Idsetting`, `SettingKey`, `SettingValue`, `UpdatedAt`, `UsersId`) và được backend đọc khi cộng XP. Migration `MoveGamificationToSystemSettings` chuyển giá trị từ bảng `GamificationSettings` cũ sang key `Gamification.VocabularyLearnedXp`, tạo các key còn lại và bảng chống nhận XP câu hỏi lặp lại. Chạy `dotnet ef database update --project src/EnglishLearning.Infrastructure --startup-project src/EnglishLearning.API` từ thư mục `backend` để áp dụng migration. EXP mỗi ngày hiện không bị giới hạn.

Hoàn thành bài học chỉ được thưởng XP một lần cho mỗi người dùng/bài học; câu trả lời được kiểm tra ở backend dựa trên JSON của bài học trong Google Drive và chỉ được thưởng một lần cho mỗi câu hỏi. Điểm danh streak được gọi khi mở dashboard và chỉ thưởng một lần mỗi ngày. API chỉnh Gamification chỉ dành cho Admin; các API cộng XP yêu cầu JWT hợp lệ.

Backend xuất schema và dữ liệu thành tệp BACPAC qua kết nối SQL Server, sau đó đóng gói BACPAC thành ZIP trong thư mục tạm của backend. Cách này không yêu cầu dịch vụ SQL Server có quyền ghi vào thư mục của người dùng/backend. Tài khoản SQL cấu hình trong connection string cần quyền xem schema và đọc toàn bộ dữ liệu. BACPAC là bản sao lưu mức database (schema và dữ liệu), không chứa transaction log hoặc cấu hình cấp server như login/job; để khôi phục, giải nén ZIP rồi import tệp `.bacpac` bằng SSMS hoặc SqlPackage. Tệp tải xuống chứa dữ liệu database nhạy cảm; chỉ lưu ở nơi bảo mật.

### Chạy Frontend
Mở Terminal mới, di chuyển vào thư mục `frontend` và chạy:
```bash
cd frontend
npm install
npm run dev
```
*(Frontend thường sẽ tự động chạy tại cổng `http://localhost:5173`)*
