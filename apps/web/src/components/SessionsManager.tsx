import { useState, useEffect } from "react";
import { authApi } from "../api/client";
import type { UserSessionDto } from "../types";

export function SessionsManager() {
  const [sessions, setSessions] = useState<UserSessionDto[]>([]);
  const [loading, setLoading] = useState(false);

  const loadSessions = async () => {
    setLoading(true);
    try {
      const data = await authApi.getSessions();
      setSessions(data);
    } catch {
      // Ignored
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSessions();
  }, []);

  const handleRevoke = async (id: string) => {
    if (!confirm("Bạn có chắc muốn thu hồi phiên đăng nhập này?")) return;
    try {
      await authApi.revokeSession(id);
      await loadSessions();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Thu hồi thất bại.");
    }
  };

  const handleRevokeOthers = async () => {
    if (!confirm("Bạn có chắc muốn đăng xuất khỏi tất cả thiết bị khác?")) return;
    try {
      await authApi.revokeAllSessions(true);
      await loadSessions();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Thu hồi thất bại.");
    }
  };

  return (
    <section className="card settings-card">
      <div className="section-header-flex">
        <h2 className="section-title">Phiên đăng nhập đang hoạt động</h2>
        {sessions.length > 1 && (
          <button
            type="button"
            className="btn btn-outline btn-sm"
            onClick={handleRevokeOthers}
          >
            Đăng xuất khỏi thiết bị khác
          </button>
        )}
      </div>

      {loading ? (
        <p className="text-muted">Đang tải danh sách phiên...</p>
      ) : (
        <div className="sessions-list">
          {sessions.map((sess) => (
            <div key={sess.id} className={`session-item ${sess.isCurrent ? "current" : ""}`}>
              <div className="session-info">
                <div className="session-title">
                  <strong>{sess.deviceInfo || "Trình duyệt Web"}</strong>
                  {sess.isCurrent && <span className="badge badge-mint">Phiên hiện tại</span>}
                </div>
                <div className="session-meta">
                  <span>IP: {sess.ipAddress || "Không xác định"}</span> •{" "}
                  <span>Tạo lúc: {new Date(sess.createdAt).toLocaleString("vi-VN")}</span>
                </div>
              </div>

              {!sess.isCurrent && (
                <button
                  type="button"
                  className="btn btn-danger btn-sm"
                  onClick={() => handleRevoke(sess.id)}
                >
                  Thu hồi
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
