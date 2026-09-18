import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";
import type { UserDto } from "../types";

interface RegisterFormProps {
  onSuccess: (user: UserDto) => void;
  onGoogleLogin: () => void;
}

export function RegisterForm({ onSuccess, onGoogleLogin }: RegisterFormProps) {
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [timezone, setTimezone] = useState(() => {
    try {
      return Intl.DateTimeFormat().resolvedOptions().timeZone || "Asia/Ho_Chi_Minh";
    } catch {
      return "Asia/Ho_Chi_Minh";
    }
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password.length < 12) {
      setError("Mật khẩu phải có tối thiểu 12 ký tự theo chính sách bảo mật.");
      return;
    }

    setLoading(true);
    try {
      await authApi.register({
        email,
        password,
        displayName: displayName.trim() || email.split("@")[0],
        timezone
      });

      // Auto login after registration
      const loggedIn = await authApi.login({ email, password });
      onSuccess(loggedIn);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Đăng ký tài khoản thất bại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      <div className="form-group">
        <label htmlFor="reg-name">Tên hiển thị</label>
        <input
          id="reg-name"
          type="text"
          required
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          placeholder="Ví dụ: Minh Thư"
          disabled={loading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="reg-email">Địa chỉ Email</label>
        <input
          id="reg-email"
          type="email"
          required
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="ten@example.com"
          disabled={loading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="reg-password">
          Mật khẩu <span className="label-hint">(tối thiểu 12 ký tự)</span>
        </label>
        <input
          id="reg-password"
          type="password"
          required
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Tối thiểu 12 ký tự hoặc cụm từ dài"
          disabled={loading}
        />
        <span className={`pwd-length-hint ${password.length >= 12 ? "valid" : ""}`}>
          {password.length >= 12 ? "✓ Độ dài mật khẩu đạt yêu cầu" : `${password.length}/12 ký tự`}
        </span>
      </div>

      <div className="form-group">
        <label htmlFor="reg-timezone">Múi giờ</label>
        <select
          id="reg-timezone"
          value={timezone}
          onChange={(e) => setTimezone(e.target.value)}
          disabled={loading}
        >
          <option value="Asia/Ho_Chi_Minh">Việt Nam (GMT+7 - Asia/Ho_Chi_Minh)</option>
          <option value="Asia/Tokyo">Nhật Bản (GMT+9 - Asia/Tokyo)</option>
          <option value="Asia/Seoul">Hàn Quốc (GMT+9 - Asia/Seoul)</option>
          <option value="Asia/Singapore">Singapore (GMT+8 - Asia/Singapore)</option>
          <option value="Europe/Paris">Châu Âu (GMT+1 - Europe/Paris)</option>
          <option value="America/New_York">Bắc Mỹ (GMT-5 - America/New_York)</option>
          <option value="UTC">UTC chuẩn</option>
        </select>
      </div>

      <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
        {loading ? "Đang tạo tài khoản..." : "Đăng ký tài khoản"}
      </button>

      <div className="divider">
        <span>hoặc</span>
      </div>

      <button
        type="button"
        className="btn btn-google btn-block"
        onClick={onGoogleLogin}
        disabled={loading}
      >
        Tiếp tục với Google
      </button>
    </form>
  );
}
