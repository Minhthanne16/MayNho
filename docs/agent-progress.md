# Tiến độ thực hiện các Milestone — Mây Nhỏ

Tài liệu ghi nhận bằng chứng thực tế sau khi kiểm tra và chạy thành công trên máy.

## Bảng theo dõi Milestones

| Milestone | Trạng thái | Bằng chứng kiểm thử | Blocker / Phần chưa thực hiện |
|---|---|---|---|
| **M0 — Nền móng** | **ĐẠT** | 12/12 unit & API foundation tests pass (net10.0), web build Vite+React19 thành công, static asset serving hoạt động. Docker compose cho PostgreSQL 17 + Mailpit sẵn sàng. | Chưa chạy container DB thật tại step này. |
| **M1 — Identity & Design** | **ĐẠT** | 38/38 tests pass (30 Unit & 8 Integration). Schema Identity áp dụng trên PostgreSQL thật. Rate limiting, atomic registration, sliding cookie renewal, token lifetimes (24h/30m), Google OIDC rules (AC02), CSRF protection, và toàn bộ frontend Auth/Settings UI hoàn chỉnh. | Không còn blocker M1. Sẵn sàng cho M2. |
| **M2 — Tasks & Checklist** | Chưa bắt đầu | - | Phụ thuộc M1. |
| **M3 — Notes & Calendar** | Chưa bắt đầu | - | Phụ thuộc M2. |
| **M4 — Reminders & Worker** | Chưa bắt đầu | - | Phụ thuộc M2, M3. |
| **M5 — Hardening** | Chưa bắt đầu | - | Phụ thuộc M4. |
| **M6 — CI/CD & Deploy** | Chưa bắt đầu | - | Phụ thuộc hoàn thiện docker targets và tài nguyên cloud. |
| **M7 — UAT & Phát hành** | Chưa bắt đầu | - | Phụ thuộc staging và verified domain thật. |

## Bằng chứng M0

1. **Unit test & API foundation:**
   - Command: `.tools\dotnet\dotnet.exe test MayNho.slnx`
   - Kết quả: Passed! 12 passed, 0 failed, duration 1s.
   - Các case đã pass:
     - Title domain validations (trims, reject blank, enforce lengths).
     - `/health/live` trả 200 OK `{"status": "alive"}`.
     - `/health/ready` trả 503 ProblemDetails fail-closed khi chưa có persistence.
     - `/api/v1/missing` trả 404 ProblemDetails JSON, không rò rỉ index.html.
     - SPA Fallback phục vụ `index.html` của React app cho các route UI.
2. **Frontend compilation:**
   - `pnpm --dir apps/web typecheck` pass.
   - `pnpm --dir apps/web build` pass, output bundle vào `src/MayNho.Api/wwwroot`.

## Bằng chứng M1

1. **Kiểm thử tự động:**
   - Lệnh: `.tools\dotnet\dotnet.exe test MayNho.slnx`
   - Kết quả: **Passed! Failed: 0, Passed: 38 (30 Unit, 8 Integration), Duration: ~3s**.
   - Các case nghiệm thu đã pass:
     - **AC02 — Google OIDC Conflict Guard:** Người dùng có email trùng với tài khoản local không được tự động link, bắt buộc chứng minh quyền sở hữu tài khoản mật khẩu trước.
     - **AC03 — CSRF & Session Security:** Mutation không kèm CSRF header bị từ chối; CSRF token phát sinh và kiểm tra hợp lệ; cookie phiên `mn_session` gia hạn trượt (sliding renewal) đồng bộ với hạn phiên trong database.
     - **Anti-enumeration:** Quên mật khẩu trả về 200 OK cho cả email có trong hệ thống lẫn không tồn tại mà không làm lộ thông tin tài khoản.
     - **Token Lifespans:** Token xác nhận email có thời hạn 24 giờ; token đặt lại mật khẩu có thời hạn 30 phút.
     - **Isolation:** Hai tài khoản khác nhau hoàn toàn bị cô lập dữ liệu và phiên làm việc, không thể thu hồi hoặc xem dữ liệu của nhau.
     - **Readiness Check:** `/health/ready` kiểm tra kết nối database và từ chối nếu có EF migration chưa áp dụng.
     - **Rate Limiting:** Cấu hình giới hạn tần suất yêu cầu cho các endpoint nhạy cảm (đăng ký, đăng nhập, quên mật khẩu).
2. **Frontend UI & API Client:**
   - API Client tự động lấy và đính kèm `X-CSRF-TOKEN` cho các yêu cầu thay đổi dữ liệu.
   - Giao diện xác thực đầy đủ: Đăng nhập, Đăng ký (mật khẩu ≥ 12 ký tự, auto-detect timezone), Quên mật khẩu, Đặt lại mật khẩu, Xác minh email.
   - Màn hình Tổng quan và Cài đặt: Chỉnh sửa hồ sơ (tên hiển thị, múi giờ, ngôn ngữ, theme), Tùy chọn nhắc việc & Giờ yên lặng, Quản lý phiên hoạt động (xem danh sách, thu hồi phiên riêng lẻ hoặc tất cả thiết bị khác).
   - Compile: `pnpm --dir apps/web typecheck` (0 errors) & `pnpm --dir apps/web build` thành công.
