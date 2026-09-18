import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";
import type { UserDto } from "../types";

interface ProfileFormProps {
  user: UserDto;
  onUserUpdated: (user: UserDto) => void;
  onSuccess: (msg: string) => void;
}

export function ProfileForm({ user, onUserUpdated, onSuccess }: ProfileFormProps) {
  const [displayName, setDisplayName] = useState(user.profile?.displayName || "");
  const [timezone, setTimezone] = useState(user.profile?.timezone || "Asia/Ho_Chi_Minh");
  const [locale, setLocale] = useState(user.profile?.locale || "vi-VN");
  const [theme, setTheme] = useState(user.profile?.theme || "light");
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await authApi.updateProfile({ displayName, timezone, locale, theme });
      onUserUpdated({ ...user, profile: updated });
      onSuccess("Đã lưu thông tin hồ sơ cá nhân!");
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Cập nhật hồ sơ thất bại.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="card settings-card">
      <h2 className="section-title">Hồ sơ cá nhân</h2>
      <form onSubmit={handleSubmit} className="settings-form">
        <div className="form-group">
          <label htmlFor="set-name">Tên hiển thị</label>
          <input
            id="set-name"
            type="text"
            required
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
          />
        </div>

        <div className="form-group">
          <label htmlFor="set-timezone">Múi giờ</label>
          <select
            id="set-timezone"
            value={timezone}
            onChange={(e) => setTimezone(e.target.value)}
          >
            <option value="Asia/Ho_Chi_Minh">Việt Nam (GMT+7)</option>
            <option value="Asia/Tokyo">Nhật Bản (GMT+9)</option>
            <option value="Asia/Seoul">Hàn Quốc (GMT+9)</option>
            <option value="Asia/Singapore">Singapore (GMT+8)</option>
            <option value="UTC">UTC chuẩn</option>
          </select>
        </div>

        <div className="grid-2">
          <div className="form-group">
            <label htmlFor="set-locale">Ngôn ngữ</label>
            <select id="set-locale" value={locale} onChange={(e) => setLocale(e.target.value)}>
              <option value="vi-VN">Tiếng Việt</option>
              <option value="en-US">English</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="set-theme">Giao diện</label>
            <select id="set-theme" value={theme} onChange={(e) => setTheme(e.target.value)}>
              <option value="light">Sáng (Mây Nhỏ)</option>
              <option value="dark">Tối</option>
              <option value="system">Theo hệ thống</option>
            </select>
          </div>
        </div>

        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? "Đang lưu..." : "Lưu thay đổi"}
        </button>
      </form>
    </section>
  );
}
