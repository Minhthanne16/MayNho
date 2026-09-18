import { CloudLogo } from "./CloudLogo";
import type { UserDto } from "../types";

interface DashboardProps {
  user: UserDto;
  onOpenSettings: () => void;
}

export function Dashboard({ user, onOpenSettings }: DashboardProps) {
  const displayName = user.profile?.displayName || (user.profile as unknown as { DisplayName?: string })?.DisplayName || user.email;
  const timezone = user.profile?.timezone || (user.profile as unknown as { Timezone?: string })?.Timezone || "Asia/Ho_Chi_Minh";

  return (
    <div className="dashboard-container">
      {!user.emailConfirmed && (
        <div className="alert alert-warning" role="status">
          <div className="alert-content">
            <strong>Địa chỉ email chưa được xác minh: </strong>
            Email của bạn cần được xác minh để nhận thông báo nhắc việc. Vui lòng kiểm tra hộp thư (Mailpit ở local: <code>http://localhost:8025</code>).
          </div>
        </div>
      )}

      <section className="welcome-card card">
        <div className="welcome-header">
          <CloudLogo size={44} />
          <div>
            <h2 className="welcome-title">Chào {displayName},</h2>
            <p className="welcome-subtitle">Hôm nay mình làm từng chút nhé.</p>
          </div>
        </div>
      </section>

      <div className="grid-2">
        <div className="card info-card">
          <h3 className="card-title">Tài khoản & Múi giờ</h3>
          <ul className="info-list">
            <li>
              <span className="info-label">Email:</span>
              <span className="info-value">{user.email}</span>
            </li>
            <li>
              <span className="info-label">Trạng thái email:</span>
              <span className={`badge ${user.emailConfirmed ? "badge-mint" : "badge-rose"}`}>
                {user.emailConfirmed ? "Đã xác minh" : "Chưa xác minh"}
              </span>
            </li>
            <li>
              <span className="info-label">Múi giờ hoạt động:</span>
              <span className="info-value">{timezone}</span>
            </li>
          </ul>
          <button type="button" className="btn btn-outline btn-sm mt-4" onClick={onOpenSettings}>
            Chỉnh sửa cài đặt
          </button>
        </div>

        <div className="card info-card">
          <h3 className="card-title">Nhắc việc & Giờ yên lặng</h3>
          <ul className="info-list">
            <li>
              <span className="info-label">Thông báo email:</span>
              <span className="info-value">
                {user.preferences?.emailNotificationsEnabled ? "Đang bật" : "Tạm tắt"}
              </span>
            </li>
            <li>
              <span className="info-label">Giờ yên lặng:</span>
              <span className="info-value">
                {user.preferences?.quietHoursEnabled
                  ? `${user.preferences.quietStart || "22:00"} — ${user.preferences.quietEnd || "07:00"}`
                  : "Không áp dụng"}
              </span>
            </li>
          </ul>
          <button type="button" className="btn btn-outline btn-sm mt-4" onClick={onOpenSettings}>
            Thiết lập giờ yên lặng
          </button>
        </div>
      </div>

      <section className="card next-milestones-card">
        <h3 className="card-title">Tính năng sắp tới (M2 & M3)</h3>
        <p className="text-muted">
          Giai đoạn M1 Identity & Design đã sẵn sàng. Các module quản lý công việc (To-do), ghi chú Markdown và lịch cá nhân đang được tiếp tục xây dựng!
        </p>
      </section>
    </div>
  );
}
