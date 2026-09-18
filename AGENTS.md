# Quy ước Mây Nhỏ

- Đọc PLAN_May_Nho_Web_App.md, docs/adr và docs/agent-progress.md trước khi sửa.
- Làm theo M0 → M7. Không đánh dấu milestone hoàn thành khi thiếu bằng chứng.
- Giữ React 19/TypeScript/Vite, ASP.NET Core 10, PostgreSQL 17 và worker độc lập.
- Dữ liệu cá nhân đi qua API/database; không dùng localStorage hoặc mock làm persistence.
- Không tự viết auth/crypto; không bỏ CSRF, ownership hoặc concurrency để test xanh.
- Schema thay đổi cần EF migration và đánh giá rollback. Không EnsureCreated production.
- API client generate từ OpenAPI, không sửa tay.
- Pin dependency và lockfile sau kiểm thử. Không secrets trong git/log/artifact.
- Mỗi ticket cập nhật acceptance criteria, test thực chạy, blocker và bằng chứng.
- Không mua tài nguyên hay deploy production khi chưa có quyền và cấu hình thật.
- Giao diện tiếng Việt, responsive, accessibility, loading/error/empty; không giả vờ lưu offline.
