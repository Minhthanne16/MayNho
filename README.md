# Mây Nhỏ — Ứng dụng cá nhân nhắc việc nhẹ nhàng

Web app quản lý việc cần làm, ghi chú và lịch cá nhân, gửi email nhắc việc đến Gmail đã xác minh của người dùng.

## 1. Yêu cầu môi trường

- Node.js 24.19.0 (hoặc Node 24 LTS)
- pnpm 12.4.2 (`npm install -g pnpm@12.4.2`)
- .NET SDK 10.0.401 (tự động dùng trong `.tools/dotnet` hoặc cài trên máy)
- Docker Desktop (hỗ trợ PostgreSQL 17 và Mailpit ở local)

## 2. Khởi chạy môi trường local

```bash
# Bật DB và Mailpit
docker compose up -d postgres mailpit

# Cài đặt dependency frontend
pnpm install --frozen-lockfile

# Chạy kiểm thử đơn vị & API foundation
./.tools/dotnet/dotnet.exe test MayNho.slnx

# Chạy build frontend
pnpm --dir apps/web build

# Chạy backend API (phục vụ cả frontend và endpoint API tại cùng origin)
./.tools/dotnet/dotnet.exe run --project src/MayNho.Api
```

Frontend dev server độc lập (có proxy sang API):
```bash
pnpm --dir apps/web dev
```

## 3. Kiến trúc

- **Frontend:** React 19, TypeScript strict, Vite.
- **Backend:** ASP.NET Core .NET 10 LTS, REST API, kiến trúc chia theo module nghiệp vụ.
- **Database:** PostgreSQL 17, EF Core.
- **Email:** Resend cho production/staging; Mailpit cho local development.
- **Worker:** .NET BackgroundService độc lập, polling DB với lease token và `SKIP LOCKED`.
