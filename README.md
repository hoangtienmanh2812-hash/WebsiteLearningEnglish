# Website Learning English

Đồ án xây dựng website hỗ trợ người dùng học và ôn luyện tiếng Anh. Người dùng có thể làm bài học/bài kiểm tra, theo dõi tiến độ và điểm kinh nghiệm (XP), thực hiện nhiệm vụ hằng ngày và xem bảng xếp hạng. Ứng dụng có các trang quản trị để quản lý bài học, người dùng và một số thiết lập hệ thống.

## Tổng quan kiến trúc

Ứng dụng gồm frontend SPA, backend Web API và cơ sở dữ liệu SQL Server:

```text
frontend (React SPA)
        │ HTTP / JSON
        ▼
backend/src/EnglishLearning.API
        │
        ├── EnglishLearning.Application
        │       └── EnglishLearning.Domain
        └── EnglishLearning.Infrastructure
                ├── Entity Framework Core / SQL Server
                └── Google Drive API
```

- **Backend — Clean Architecture**
  - `EnglishLearning.Domain`: các thực thể và quy tắc miền nghiệp vụ; không phụ thuộc vào các lớp bên ngoài.
  - `EnglishLearning.Application`: các chức năng ứng dụng, dịch vụ và abstraction; phụ thuộc vào Domain.
  - `EnglishLearning.Infrastructure`: triển khai truy cập dữ liệu, repository, xác thực JWT và tích hợp Google Drive; phụ thuộc vào Application và Domain.
  - `EnglishLearning.API`: ASP.NET Core Web API, controllers, cấu hình DI, CORS và middleware; kết nối Application với Infrastructure.
- **Frontend — Feature-based architecture**: mã React được tổ chức theo tính năng trong `frontend/src/features/`, gồm xác thực, dashboard, quiz, bài kiểm tra, nhiệm vụ, bảng xếp hạng, tài khoản và quản trị. `components/` và `layouts/` chứa các thành phần giao diện dùng chung.
- **Database**: SQL Server, truy cập qua Entity Framework Core; các thay đổi schema được quản lý bằng migrations.
- **Tích hợp ngoài**: Google Drive API dùng cho nội dung bài học do quản trị viên quản lý.

### Ngôn ngữ và công nghệ

- **TypeScript, HTML, CSS** — giao diện người dùng.
- **C#** — backend và các project kiểm thử.
- **React 19**, **React Router 7**, **Vite 8**, **Tailwind CSS 4** — frontend.
- **.NET 8 / ASP.NET Core 8**, **Entity Framework Core 8**, **SQL Server** — backend và dữ liệu.
- **xUnit** — unit tests và integration tests.

## Packages và công cụ cần có

### Công cụ cài trên máy

- **.NET 8 SDK**
- **Node.js 20.19+ hoặc 22.12+** và npm (theo yêu cầu của Vite 8)
- **SQL Server** — cài local hoặc sử dụng một SQL Server có thể kết nối được
- Git (nếu clone dự án từ repository)

### Các package của dự án

Frontend khai báo các package trong `frontend/package.json`; backend và tests khai báo NuGet packages trong các file `.csproj`. Các lệnh `npm install` và `dotnet restore` bên dưới sẽ tự tải đúng phiên bản đã khai báo.

- **Frontend:** React, React DOM, React Router, Axios, Vite, TypeScript, Tailwind CSS và Vite plugin, PostCSS, Autoprefixer, Oxlint.
- **Backend:** Google APIs Auth/Drive, ASP.NET Core JWT Bearer và OpenAPI, Entity Framework Core SQL Server/Design, Swashbuckle, BCrypt.Net và Microsoft SQL Server DAC Fx.
- **Kiểm thử .NET:** xUnit, Microsoft.NET.Test.Sdk và coverlet.collector.

## Cài đặt và chạy dự án

Thực hiện lần lượt các bước dưới đây. Mở PowerShell tại thư mục gốc repository (`WebsiteLearningEnglish`).

### 1. Tải các package backend và test

Các lệnh `dotnet restore` tải NuGet packages đã khai báo trong từng project. Chạy từ thư mục gốc:

```powershell
cd backend
dotnet restore src\EnglishLearning.API\EnglishLearning.API.csproj
dotnet restore ..\tests\EnglishLearning.UnitTests\EnglishLearning.UnitTests.csproj --configfile nuget.config
dotnet restore ..\tests\EnglishLearning.IntegrationTests\EnglishLearning.IntegrationTests.csproj --configfile nuget.config
```

### 2. Tải các package frontend

Mở PowerShell mới tại thư mục gốc repository:

```powershell
cd frontend
npm install
```

Lệnh này cài các package được liệt kê trong `frontend/package.json`.

### 3. Cài công cụ Entity Framework CLI

Chỉ cần cài một lần trên máy để chạy lệnh cập nhật database. Nếu `dotnet-ef` phiên bản 8.0.8 đã được cài, bỏ qua bước này:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.8
```

### 4. Cấu hình SQL Server và JWT

Mặc định, cấu hình Development trong `backend/src/EnglishLearning.API/appsettings.Development.json` dùng Windows Authentication tới SQL Server local:

```text
Server=localhost;Database=EnglishLearningDb;Trusted_Connection=True;TrustServerCertificate=True;
```

Nếu cần, cập nhật `ConnectionStrings:DefaultConnection` trong cấu hình Development hoặc đặt biến môi trường `ConnectionStrings__DefaultConnection`. Không đưa mật khẩu hoặc thông tin nhạy cảm vào Git.

Backend yêu cầu `Jwt:Secret` dài tối thiểu 32 byte. Mở PowerShell tại thư mục gốc repository rồi chạy nguyên lần lượt các lệnh sau để khởi tạo User Secrets, tự tạo secret ngẫu nhiên và lưu vào cấu hình local:

```powershell
dotnet user-secrets init --project backend\src\EnglishLearning.API

$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:Secret" $secret --project backend\src\EnglishLearning.API

$rng.Dispose()
Remove-Variable secret, bytes, rng
```

Các lệnh trên chạy trong PowerShell. Nếu User Secrets đã được khởi tạo cho project trước đó, bỏ qua lệnh `dotnet user-secrets init` và chạy các lệnh còn lại để tạo secret mới. Secret chỉ được lưu trong User Secrets trên máy hiện tại, không được commit vào repository. Nếu không dùng User Secrets, có thể tự đặt biến môi trường `Jwt__Secret`.

### 5. Tạo hoặc cập nhật database

Đảm bảo SQL Server đang chạy, sau đó chạy tại thư mục gốc repository:

```powershell
dotnet ef database update --project backend\src\EnglishLearning.Infrastructure\EnglishLearning.Infrastructure.csproj --startup-project backend\src\EnglishLearning.API\EnglishLearning.API.csproj
```

### 6. Khởi động backend

Mở PowerShell mới tại thư mục gốc repository:

```powershell
cd backend
dotnet run --project src\EnglishLearning.API\EnglishLearning.API.csproj
```

Profile mặc định chạy môi trường Development. API có địa chỉ `http://localhost:5219` và `https://localhost:7200`; Swagger UI được mở tại `/swagger` trong môi trường Development.

### 7. Khởi động frontend

Giữ backend đang chạy. Mở PowerShell khác tại thư mục gốc repository:

```powershell
cd frontend
npm run dev
```

Mở `http://localhost:5173`. Backend hiện cho phép CORS từ địa chỉ này. Các lệnh frontend khác (chạy trong thư mục `frontend`):

```powershell
npm run build
npm run lint
npm run preview
```

### Chạy kiểm thử .NET

Từ thư mục gốc repository:

```powershell
dotnet test tests\EnglishLearning.UnitTests\EnglishLearning.UnitTests.csproj --no-restore
dotnet test tests\EnglishLearning.IntegrationTests\EnglishLearning.IntegrationTests.csproj --no-restore
```

## Cấu hình tùy chọn: quản trị nội dung Google Drive

Chỉ cần cấu hình phần này nếu sử dụng trang quản trị bài học với Google Drive:

1. Tạo OAuth Client ID loại **Desktop app** trong Google Cloud Console và bật Google Drive API.
2. Lưu file OAuth client credentials ngoài repository. Mặc định, backend tìm file tại `C:\secrets\google-oauth-client.json`; có thể đổi bằng biến môi trường `GoogleDrive__OAuthCredentialsPath`.
3. Cấu hình `GoogleDrive:RootFolderId` trỏ tới thư mục Drive chứa nội dung và cấp quyền truy cập cho tài khoản Google quản trị. Nếu OAuth consent screen đang ở chế độ Testing, thêm tài khoản đó vào danh sách Test users.
4. Thêm email quản trị vào `GoogleDrive:AdminEmails` hoặc đặt biến môi trường `GoogleDrive__AdminEmails__0`. Token OAuth được lưu cục bộ trên máy chạy backend.

Không commit credentials hoặc OAuth token vào Git. Nội dung JSON bài học được đọc từ Google Drive; thông tin quản lý bài học được lưu trong SQL Server.

## Cấu trúc repository

```text
backend/
  EnglishLearning.slnx
  src/
    EnglishLearning.API/
    EnglishLearning.Application/
    EnglishLearning.Domain/
    EnglishLearning.Infrastructure/
frontend/
  src/
    components/
    features/
    layouts/
tests/
  EnglishLearning.UnitTests/
  EnglishLearning.IntegrationTests/
```
