import { useEffect, useState } from "react";
import { Header } from "./components/Header";
import { AuthContainer } from "./components/AuthContainer";
import { Dashboard } from "./components/Dashboard";
import { SettingsView } from "./components/SettingsView";
import { CloudLogo } from "./components/CloudLogo";
import { authApi } from "./api/client";
import type { UserDto } from "./types";

export default function App() {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"dashboard" | "settings">("dashboard");

  useEffect(() => {
    // Check current session
    authApi
      .getMe()
      .then((data) => setUser(data))
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore
    } finally {
      setUser(null);
      setActiveTab("dashboard");
    }
  };

  if (loading) {
    return (
      <div className="loading-screen" role="status" aria-live="polite">
        <CloudLogo size={56} className="pulse-animation" />
        <p className="loading-text">Mây Nhỏ đang chuẩn bị...</p>
      </div>
    );
  }

  return (
    <div className="app-layout">
      <Header
        user={user}
        activeTab={activeTab}
        onTabChange={setActiveTab}
        onLogout={handleLogout}
      />

      <main className="main-content">
        {!user ? (
          <AuthContainer onAuthSuccess={(loggedInUser) => setUser(loggedInUser)} />
        ) : activeTab === "dashboard" ? (
          <Dashboard user={user} onOpenSettings={() => setActiveTab("settings")} />
        ) : (
          <SettingsView user={user} onUserUpdated={(updated) => setUser(updated)} />
        )}
      </main>

      <footer className="footer">
        <p>Mây Nhỏ • Web app cá nhân nhắc việc nhẹ nhàng</p>
      </footer>
    </div>
  );
}

