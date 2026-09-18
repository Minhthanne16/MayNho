import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";
import type { UserDto } from "../types";

interface PreferencesFormProps {
  user: UserDto;
  onUserUpdated: (user: UserDto) => void;
  onSuccess: (msg: string) => void;
}

export function PreferencesForm({ user, onUserUpdated, onSuccess }: PreferencesFormProps) {
  const [emailNotifications, setEmailNotifications] = useState(user.preferences?.emailNotificationsEnabled ?? true);
  const [quietHoursEnabled, setQuietHoursEnabled] = useState(user.preferences?.quietHoursEnabled ?? false);
  const [quietStart, setQuietStart] = useState(user.preferences?.quietStart || "22:00");
  const [quietEnd, setQuietEnd] = useState(user.preferences?.quietEnd || "07:00");
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await authApi.updatePreferences({
        emailNotificationsEnabled: emailNotifications,
        quietHoursEnabled,
        quietStart: quietHoursEnabled ? quietStart : null,
        quietEnd: quietHoursEnabled ? quietEnd : null,
        quietTimezone: user.profile?.timezone || "Asia/Ho_Chi_Minh"
      });
      onUserUpdated({ ...user, preferences: updated });
      onSuccess("Đã lưu tùy chọn thông báo và giờ yên lặng!");
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Cập nhật tùy chọn thất bại.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="card settings-card">
      <h2 className="section-title">Thông báo & Giờ yên lặng</h2>
      <form onSubmit={handleSubmit} className="settings-form">
        <div className="form-checkbox">
          <input
            id="pref-email"
            type="checkbox"
            checked={emailNotifications}
            onChange={(e) => setEmailNotifications(e.target.checked)}
          />
          <label htmlFor="pref-email">Nhận email nhắc việc khi có việc đến hạn hoặc sự kiện</label>
        </div>

        <div className="form-checkbox">
          <input
            id="pref-quiet"
            type="checkbox"
            checked={quietHoursEnabled}
            onChange={(e) => setQuietHoursEnabled(e.target.checked)}
          />
          <label htmlFor="pref-quiet">Bật Giờ yên lặng (không gửi email làm phiền trong khoảng này)</label>
        </div>

        {quietHoursEnabled && (
          <div className="grid-2 quiet-hours-inputs">
            <div className="form-group">
              <label htmlFor="quiet-start">Bắt đầu từ</label>
              <input
                id="quiet-start"
                type="time"
                value={quietStart}
                onChange={(e) => setQuietStart(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label htmlFor="quiet-end">Đến</label>
              <input
                id="quiet-end"
                type="time"
                value={quietEnd}
                onChange={(e) => setQuietEnd(e.target.value)}
              />
            </div>
          </div>
        )}

        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? "Đang lưu..." : "Lưu tùy chọn"}
        </button>
      </form>
    </section>
  );
}
