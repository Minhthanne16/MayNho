# ☁️ Mây Nhỏ — Ứng dụng cá nhân nhắc việc nhẹ nhàng

> **Mây Nhỏ** là web app cá nhân hóa hỗ trợ quản lý việc cần làm (To-Do), ghi chú (Notes), lịch biểu cá nhân (Calendar) và tự động gửi email nhắc việc đến tài khoản Gmail đã xác minh. Ứng dụng hướng tới triết lý thiết kế pastel dịu nhẹ, không phán xét, hỗ trợ chia nhỏ mục tiêu cho những ngày năng lượng thấp.

---

## 📑 Mục lục
1. [Yêu cầu hệ thống](#1-yêu-cầu-hệ-thống)
2. [Cấu trúc dự án](#2-cấu-trúc-dự-án)
3. [Hướng dẫn cài đặt & Thiết lập ban đầu](#3-hướng-dẫn-cài-đặt--thiết-lập-ban-đầu)
4. [Hướng dẫn chạy ứng dụng](#4-hướng-dẫn-chạy-ứng-dụng)
5. [Hướng dẫn chạy kiểm thử (Testing)](#5-hướng-dẫn-chạy-kiểm-thử-testing)
6. [Danh sách API & Tính năng đã hoàn thành (M1)](#6-danh-sách-api--tính-năng-đã-hoàn-thành-m1)
7. [Bảo mật & Quy ước hệ thống](#7-bảo-mật--quy-ước-hệ-thống)

---

## 1. Yêu cầu hệ thống

Trước khi bắt đầu, máy tính của bạn cần cài đặt:

- **Hệ điều hành:** Windows 10/11, macOS, hoặc Linux.
- **Docker & Docker Compose:** Để chạy PostgreSQL 17 và Mailpit ở môi trường local.
- **Node.js:** Phiên bản `>= 22.0.0` (Khuyến nghị Node `24.x` LTS theo `.node-version`).
- **pnpm:** Phiên bản `>= 10.x` hoặc `12.4.2` (`npm install -g pnpm@12.4.2`).
- **.NET SDK:** .NET 10 SDK (Dùng `dotnet` toàn cục hoặc SDK portable tại `.tools/dotnet/dotnet.exe`).

---

## 2. Cấu trúc dự án

Dự án được tổ chức theo mô hình Clean Architecture kết hợp Monorepo:

```text
MayNho/
├── apps/
│   └── web/                     # Frontend React 19 + TypeScript + Vite + Tailwind/CSS
├── src/
│   ├── MayNho.Domain/           # Entities, Value Objects, Domain Exceptions
│   ├── MayNho.Application/      # DTOs, Interfaces, Logic xác thực & phiên làm việc
│   ├── MayNho.Infrastructure/   # EF Core DbContext, ASP.NET Identity, Email Service
│   ├── MayNho.Api/              # ASP.NET Core Minimal APIs, Auth Handlers, Static Web Hosting
│   ├── MayNho.Worker/           # Background Worker xử lý quét nhắc việc ngầm
│   └── MayNho.Migrator/         # Công cụ tự động áp dụng database migrations
├── tests/
│   ├── Unit/                    # Unit tests & API foundation tests (Domain, Password, Title)
│   └── Integration/             # Integration tests tương tác DB PostgreSQL thật
├── compose.yaml                 # Cấu hình Docker cho PostgreSQL 17 và Mailpit
├── .env.example                 # File mẫu cấu hình biến môi trường
└── MayNho.slnx                  # Solution file cho .NET
```

---

## 3. Hướng dẫn cài đặt & Thiết lập ban đầu

### Bước 1: Khởi động cơ sở dữ liệu & Mailpit qua Docker

```bash
docker compose up -d postgres mailpit
```
*Kiểm tra trạng thái:* `docker compose ps`. Cả hai container đều ở trạng thái `Up`.

### Bước 2: Cấu hình biến môi trường

Sao chép file mẫu:
```bash
cp .env.example .env
```
Các thông số mặc định đã trỏ đúng tới PostgreSQL (cổng `5433`) và Mailpit (SMTP `1025`, Web UI `8025`).

### Bước 3: Cài đặt thư viện Frontend

```bash
pnpm install --frozen-lockfile
```

### Bước 4: Áp dụng Migration vào Cơ sở dữ liệu

```bash
dotnet run --project src/MayNho.Migrator
# Hoặc: .\.tools\dotnet\dotnet.exe run --project src/MayNho.Migrator
```

---

## 4. Hướng dẫn chạy ứng dụng

### Cách 1: Chế độ Development (Khuyến nghị cho lập trình viên)

Chế độ này hỗ trợ Hot-Reload cả C# backend lẫn React frontend:

1. **Khởi động Backend API (Terminal 1):**
   ```bash
   dotnet run --project src/MayNho.Api
   # Hoặc: .\.tools\dotnet\dotnet.exe run --project src/MayNho.Api
   ```
   Backend API sẽ lắng nghe tại: `http://localhost:5000`

2. **Khởi động Frontend Vite Dev Server (Terminal 2):**
   ```bash
   pnpm --dir apps/web dev
   ```
   Frontend Vite sẽ mở tại: `http://localhost:5173` (Tự động proxy `/api` và `/health` sang `http://localhost:5000`)

3. **Mở trình duyệt:**
   - **Giao diện Web:** [http://localhost:5173](http://localhost:5173)
   - **Hộp thư thử nghiệm (Mailpit Web UI):** [http://localhost:8025](http://localhost:8025)
   - **OpenAPI Spec:** [http://localhost:5000/openapi/v1.json](http://localhost:5000/openapi/v1.json)
   - **Health Checks:** [http://localhost:5000/health/live](http://localhost:5000/health/live) & [http://localhost:5000/health/ready](http://localhost:5000/health/ready)

### Cách 2: Chế độ Đơn khối Sản xuất (Monolith / Single Origin)

1. **Build Frontend Bundle:**
   ```bash
   pnpm --dir apps/web build
   ```
2. **Chạy Backend API:**
   ```bash
   dotnet run --project src/MayNho.Api
   ```
3. Truy cập trực tiếp tại: [http://localhost:5000](http://localhost:5000).

### Chạy Background Worker (Nhắc việc ngầm)
```bash
dotnet run --project src/MayNho.Worker
# Hoặc: .\.tools\dotnet\dotnet.exe run --project src/MayNho.Worker
```

---

## 5. Hướng dẫn chạy kiểm thử (Testing)

Dự án tuân thủ tiêu chuẩn chất lượng nghiêm ngặt với thiết lập cảnh báo coi như lỗi (`TreatWarningsAsErrors = true`).

### 1. Chạy toàn bộ Test Suite (.NET)
Bao gồm cả Unit Tests và Integration Tests trên PostgreSQL thật:

```bash
dotnet test MayNho.slnx
# Hoặc: .\.tools\dotnet\dotnet.exe test MayNho.slnx
```
*Kết quả:* **38/38 tests ĐẠT 100% (30 Unit Tests, 8 Integration Tests).**

### 2. Kiểm tra kiểu tĩnh Frontend (TypeScript)
```bash
pnpm --dir apps/web typecheck
```

---

## 6. Danh sách API & Tính năng đã hoàn thành (M1)

### Xác thực & Phiên làm việc (`/api/v1/auth`)
| Phương thức | Endpoint | Mô tả |
|---|---|---|
| `POST` | `/api/v1/auth/register` | Đăng ký nguyên tử (Identity + Profile + Preference), mật khẩu ≥ 12 ký tự |
| `POST` | `/api/v1/auth/login` | Đăng nhập an toàn, sinh cookie `mn_session`, chống brute-force |
| `POST` | `/api/v1/auth/logout` | Đăng xuất và thu hồi phiên làm việc hiện tại |
| `GET` | `/api/v1/auth/me` | Lấy thông tin tài khoản, hồ sơ và tùy chọn của người dùng đăng nhập |
| `GET` | `/api/v1/auth/csrf` | Cấp Anti-Forgery Token đính kèm vào header `X-CSRF-TOKEN` |
| `POST` | `/api/v1/auth/forgot-password` | Yêu cầu đổi mật khẩu (Token 30 phút, chống lộ email) |
| `POST` | `/api/v1/auth/reset-password` | Xác thực token và đặt mật khẩu mới |
| `POST` | `/api/v1/auth/verify-email` | Xác minh tài khoản email qua token (Token 24 giờ) |
| `GET` | `/api/v1/auth/sessions` | Lấy danh sách các phiên đăng nhập (thiết bị, IP, hạn dùng) |
| `DELETE` | `/api/v1/auth/sessions/{id}` | Thu hồi phiên đăng nhập cụ thể |
| `DELETE` | `/api/v1/auth/sessions/other` | Thu hồi tất cả các phiên đăng nhập khác ngoại trừ phiên hiện tại |
| `GET` | `/api/v1/auth/google/url` | Sinh URL chuyển hướng đăng nhập Google OAuth 2.0 |
| `POST` | `/api/v1/auth/google/callback` | Tiếp nhận mã xác thực từ Google OIDC (tuân thủ AC02) |
| `POST` | `/api/v1/auth/google/link` | Liên kết tài khoản Google với tài khoản local đã đăng nhập |

### Quản lý hồ sơ & Thiết lập
| Phương thức | Endpoint | Mô tả |
|---|---|---|
| `PUT` | `/api/v1/auth/profile` | Cập nhật tên hiển thị, múi giờ (IANA chuẩn), ngôn ngữ (`vi-VN`/`en-US`), theme |
| `PUT` | `/api/v1/auth/preferences` | Cài đặt bật/tắt email nhắc việc, thiết lập khung giờ yên lặng |

---

## 7. Bảo mật & Quy ước hệ thống

1. **Cookie-based Session:** Sử dụng Cookie `mn_session` với các cờ `HttpOnly`, `SameSite=Lax`, `Secure` (production) và cơ chế gia hạn trượt (sliding renewal 7 ngày).
2. **CSRF Protection:** Tất cả các endpoint thay đổi trạng thái (`POST`, `PUT`, `PATCH`, `DELETE`) bắt buộc phải có header `X-CSRF-TOKEN` hợp lệ.
3. **Chính sách mật khẩu:** Tối thiểu 12 ký tự, bắt buộc kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt.
4. **Google Account Conflict Guard (AC02):** Nếu người dùng đăng nhập bằng Google với email đã tồn tại dưới dạng tài khoản mật khẩu local, hệ thống ngăn chặn tự động liên kết và yêu cầu đăng nhập mật khẩu trước nhằm chống tấn công chiếm đoạt tài khoản (account takeover).
5. **Anti-enumeration:** Endpoint quên mật khẩu luôn phản hồi thành công (200 OK) kể cả khi email chưa đăng ký hoặc dịch vụ gửi email gặp sự cố.
6. **Rate Limiting:** Sử dụng ASP.NET Core RateLimiter trên các endpoint xác thực nhạy cảm nhằm ngăn chặn tấn công từ chối dịch vụ hoặc dò mật khẩu.
7. **Bảo vệ mã nguồn & Secrets:** Không bao giờ lưu trữ mật khẩu, API key hoặc connection string thật trong mã nguồn Git. Mọi giá trị nhạy cảm được cấu hình qua biến môi trường hoặc Secret Manager.

