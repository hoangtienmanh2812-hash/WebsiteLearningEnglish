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

### Chạy Frontend
Mở Terminal mới, di chuyển vào thư mục `frontend` và chạy:
```bash
cd frontend
npm install
npm run dev
```
*(Frontend thường sẽ tự động chạy tại cổng `http://localhost:5173`)*
