import { useState, useEffect } from "react";
import { CloudLogo } from "./CloudLogo";
import { LoginForm } from "./LoginForm";
import { RegisterForm } from "./RegisterForm";
import { ForgotPasswordForm } from "./ForgotPasswordForm";
import { ResetPasswordForm } from "./ResetPasswordForm";
import { VerifyEmailForm } from "./VerifyEmailForm";
import { authApi } from "../api/client";
import type { UserDto } from "../types";

interface AuthContainerProps {
  onAuthSuccess: (user: UserDto) => void;
}

type Mode = "login" | "register" | "forgot" | "reset" | "verify";

export function AuthContainer({ onAuthSuccess }: AuthContainerProps) {
  const [mode, setMode] = useState<Mode>("login");
  const [urlToken, setUrlToken] = useState("");
  const [urlEmail, setUrlEmail] = useState("");

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const tokenParam = params.get("token");
    const emailParam = params.get("email");

    if (tokenParam) setUrlToken(tokenParam);
    if (emailParam) setUrlEmail(emailParam);

    if (window.location.pathname.includes("/reset-password") || (tokenParam && emailParam && window.location.search.includes("reset"))) {
      setMode("reset");
    } else if (window.location.pathname.includes("/verify-email") || tokenParam) {
      setMode("verify");
    }
  }, []);

  const handleGoogleLogin = async () => {
    const promptEmail = window.prompt("Nhập địa chỉ Gmail để kiểm thử Google login:", "nguoidung@gmail.com");
    if (!promptEmail) return;

    try {
      const user = await authApi.googleLogin({
        subject: `google-user-${promptEmail.replace(/[^a-zA-Z0-9]/g, "-")}`,
        email: promptEmail,
        emailVerified: true,
        name: promptEmail.split("@")[0]
      });
      onAuthSuccess(user);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Đăng nhập Google thất bại.");
    }
  };

  return (
    <div className="auth-container">
      <div className="card auth-card">
        <div className="auth-header">
          <CloudLogo size={52} />
          <h1 className="auth-title">Mây Nhỏ</h1>
          <p className="auth-subtitle">Nhắc việc nhẹ nhàng • Hôm nay mình làm từng chút nhé</p>
        </div>

        {(mode === "login" || mode === "register") && (
          <div className="auth-tabs" role="tablist">
            <button
              type="button"
              role="tab"
              aria-selected={mode === "login"}
              className={`tab-btn ${mode === "login" ? "active" : ""}`}
              onClick={() => setMode("login")}
            >
              Đăng nhập
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mode === "register"}
              className={`tab-btn ${mode === "register" ? "active" : ""}`}
              onClick={() => setMode("register")}
            >
              Tạo tài khoản
            </button>
          </div>
        )}

        {mode === "login" && (
          <LoginForm
            onSuccess={onAuthSuccess}
            onForgotPassword={() => setMode("forgot")}
            onGoogleLogin={handleGoogleLogin}
          />
        )}

        {mode === "register" && (
          <RegisterForm
            onSuccess={onAuthSuccess}
            onGoogleLogin={handleGoogleLogin}
          />
        )}

        {mode === "forgot" && (
          <ForgotPasswordForm onBackToLogin={() => setMode("login")} />
        )}

        {mode === "reset" && (
          <ResetPasswordForm
            initialEmail={urlEmail}
            initialToken={urlToken}
            onSuccess={() => setMode("login")}
            onBackToLogin={() => setMode("login")}
          />
        )}

        {mode === "verify" && (
          <VerifyEmailForm
            initialEmail={urlEmail}
            initialToken={urlToken}
            onSuccess={() => setMode("login")}
            onBackToLogin={() => setMode("login")}
          />
        )}
      </div>
    </div>
  );
}
