import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";

interface ForgotPasswordFormProps {
  onBackToLogin: () => void;
}

export function ForgotPasswordForm({ onBackToLogin }: ForgotPasswordFormProps) {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.forgotPassword({ email });
      setMessage(res.message || "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi tới hộp thư.");
    } catch {
      setMessage("Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi tới hộp thư.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2 className="form-title">Khôi phục mật khẩu</h2>
      <p className="form-desc">Nhập địa chỉ email của bạn để nhận liên kết đặt lại mật khẩu.</p>

      {message && (
        <div className="alert alert-info" role="alert">
          {message}
        </div>
      )}

      <div className="form-group">
        <label htmlFor="forgot-email">Địa chỉ Email</label>
        <input
          id="forgot-email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="ten@example.com"
          disabled={loading}
        />
      </div>

      <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
        {loading ? "Đang gửi..." : "Gửi liên kết đặt lại"}
      </button>

      <div className="form-footer">
        <button type="button" className="link-btn" onClick={onBackToLogin}>
          ← Quay lại Đăng nhập
        </button>
      </div>
    </form>
  );
}
