import { CloudLogo } from "./CloudLogo";
import type { UserDto } from "../types";

interface HeaderProps {
  user: UserDto | null;
  activeTab: "dashboard" | "settings";
  onTabChange: (tab: "dashboard" | "settings") => void;
  onLogout: () => void;
}

export function Header({ user, activeTab, onTabChange, onLogout }: HeaderProps) {
  return (
    <header className="header">
      <div className="header-inner">
        <div className="brand">
          <CloudLogo size={36} />
          <div>
            <span className="brand-name">Mây Nhỏ</span>
            <span className="brand-tagline">Nhắc việc nhẹ nhàng</span>
          </div>
        </div>

        {user && (
          <div className="header-actions">
            <nav className="header-nav" aria-label="Điều hướng chính">
              <button
                type="button"
                className={`nav-link ${activeTab === "dashboard" ? "active" : ""}`}
                onClick={() => onTabChange("dashboard")}
              >
                Tổng quan
              </button>
              <button
                type="button"
                className={`nav-link ${activeTab === "settings" ? "active" : ""}`}
                onClick={() => onTabChange("settings")}
              >
                Cài đặt & Phiên
              </button>
            </nav>

            <div className="user-profile-badge">
              <div className="user-meta">
                <span className="user-name">
                  {user.profile.displayName || (user.profile as unknown as { DisplayName?: string }).DisplayName || user.email}
                </span>
                <span className={`badge ${user.emailConfirmed ? "badge-mint" : "badge-rose"}`}>
                  {user.emailConfirmed ? "Đã xác minh" : "Chưa xác minh"}
                </span>
              </div>
              <button
                type="button"
                className="btn btn-outline btn-sm"
                onClick={onLogout}
                aria-label="Đăng xuất khỏi hệ thống"
              >
                Đăng xuất
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}
