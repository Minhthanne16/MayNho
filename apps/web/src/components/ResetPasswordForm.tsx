import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";

interface ResetPasswordFormProps {
  initialEmail?: string;
  initialToken?: string;
  onSuccess: () => void;
  onBackToLogin: () => void;
}

export function ResetPasswordForm({
  initialEmail = "",
  initialToken = "",
  onSuccess,
  onBackToLogin
}: ResetPasswordFormProps) {
  const [email, setEmail] = useState(initialEmail);
  const [token, setToken] = useState(initialToken);
  const [newPassword, setNewPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (newPassword.length < 12) {
      setError("Mật khẩu mới phải có tối thiểu 12 ký tự.");
      return;
    }

    setLoading(true);
    try {
      const res = await authApi.resetPassword({ email, token, newPassword });
      setSuccess(res.message || "Đặt lại mật khẩu thành công! Đang chuyển hướng...");
      setTimeout(onSuccess, 1500);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Đặt lại mật khẩu thất bại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2 className="form-title">Đặt lại mật khẩu mới</h2>
      <p className="form-desc">Mật khẩu mới yêu cầu tối thiểu 12 ký tự.</p>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="form-group">
        <label htmlFor="reset-email">Email</label>
        <input
          id="reset-email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={loading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="reset-token">Mã Token từ email</label>
        <input
          id="reset-token"
          type="text"
          required
          value={token}
          onChange={(e) => setToken(e.target.value)}
          placeholder="Mã token"
          disabled={loading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="reset-new-password">Mật khẩu mới (≥12 ký tự)</label>
        <input
          id="reset-new-password"
          type="password"
          required
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          placeholder="Tối thiểu 12 ký tự"
          disabled={loading}
        />
      </div>

      <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
        {loading ? "Đang cập nhật..." : "Cập nhật mật khẩu"}
      </button>

      <div className="form-footer">
        <button type="button" className="link-btn" onClick={onBackToLogin}>
          ← Quay lại Đăng nhập
        </button>
      </div>
    </form>
  );
}
