import { useState, type FormEvent } from "react";
import { authApi } from "../api/client";

interface VerifyEmailFormProps {
  initialEmail?: string;
  initialToken?: string;
  onSuccess: () => void;
  onBackToLogin: () => void;
}

export function VerifyEmailForm({
  initialEmail = "",
  initialToken = "",
  onSuccess,
  onBackToLogin
}: VerifyEmailFormProps) {
  const [email, setEmail] = useState(initialEmail);
  const [token, setToken] = useState(initialToken);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await authApi.verifyEmail({ email, token });
      setSuccess(res.message || "Xác minh email thành công!");
      setTimeout(onSuccess, 1500);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Xác minh email thất bại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2 className="form-title">Xác minh tài khoản Email</h2>
      <p className="form-desc">Kích hoạt email để nhận thông báo nhắc việc đúng hẹn.</p>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="form-group">
        <label htmlFor="verify-email">Email</label>
        <input
          id="verify-email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={loading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="verify-token">Mã xác nhận (Token)</label>
        <input
          id="verify-token"
          type="text"
          required
          value={token}
          onChange={(e) => setToken(e.target.value)}
          disabled={loading}
        />
      </div>

      <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
        {loading ? "Đang xác minh..." : "Xác minh ngay"}
      </button>

      <div className="form-footer">
        <button type="button" className="link-btn" onClick={onBackToLogin}>
          ← Quay lại Đăng nhập
        </button>
      </div>
    </form>
  );
}
