import type {
  CsrfTokenResponse,
  ProblemDetails,
  UserDto,
  UserProfileDto,
  UserPreferenceDto,
  UserSessionDto
} from "../types";

let cachedCsrf: CsrfTokenResponse | null = null;

export async function fetchCsrfToken(): Promise<CsrfTokenResponse> {
  const res = await fetch("/api/v1/auth/csrf", {
    method: "GET",
    headers: { Accept: "application/json" }
  });
  if (!res.ok) {
    throw new Error("Không thể lấy mã bảo mật CSRF.");
  }
  const data: CsrfTokenResponse = await res.json();
  cachedCsrf = data;
  return data;
}

export async function apiFetch<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method || "GET").toUpperCase();
  const headers: Record<string, string> = {
    Accept: "application/json",
    ...(options.headers as Record<string, string> || {})
  };

  if (["POST", "PUT", "PATCH", "DELETE"].includes(method)) {
    if (!cachedCsrf) {
      await fetchCsrfToken();
    }
    if (cachedCsrf) {
      headers[cachedCsrf.headerName] = cachedCsrf.token;
    }
    if (options.body && typeof options.body === "string" && !headers["Content-Type"]) {
      headers["Content-Type"] = "application/json";
    }
  }

  const res = await fetch(endpoint, {
    ...options,
    method,
    headers,
    credentials: "same-origin"
  });

  if (!res.ok) {
    let errorDetail = `Lỗi hệ thống (${res.status})`;
    try {
      const problem: ProblemDetails = await res.json();
      errorDetail = problem.detail || problem.title || errorDetail;
    } catch {
      // Body not JSON
    }
    throw new Error(errorDetail);
  }

  if (res.status === 204 || res.headers.get("content-length") === "0") {
    return {} as T;
  }

  return (await res.json()) as T;
}

export const authApi = {
  getCsrf: () => fetchCsrfToken(),
  getMe: () => apiFetch<UserDto>("/api/v1/auth/me"),
  login: (data: { email: string; password: string }) =>
    apiFetch<UserDto>("/api/v1/auth/login", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  register: (data: { email: string; password: string; displayName: string; timezone?: string }) =>
    apiFetch<UserDto>("/api/v1/auth/register", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  logout: () =>
    apiFetch<{ message: string }>("/api/v1/auth/logout", {
      method: "POST"
    }),
  verifyEmail: (data: { email: string; token: string }) =>
    apiFetch<{ message: string }>("/api/v1/auth/verify-email", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  forgotPassword: (data: { email: string }) =>
    apiFetch<{ message: string }>("/api/v1/auth/forgot-password", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  resetPassword: (data: { email: string; token: string; newPassword: string }) =>
    apiFetch<{ message: string }>("/api/v1/auth/reset-password", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  googleLogin: (data: { subject: string; email: string; emailVerified: boolean; name?: string; timezone?: string }) =>
    apiFetch<UserDto>("/api/v1/auth/google/callback", {
      method: "POST",
      body: JSON.stringify(data)
    }),
  updateProfile: (data: { displayName: string; timezone: string; locale: string; theme: string }) =>
    apiFetch<UserProfileDto>("/api/v1/users/me/profile", {
      method: "PATCH",
      body: JSON.stringify(data)
    }),
  updatePreferences: (data: {
    emailNotificationsEnabled: boolean;
    quietHoursEnabled: boolean;
    quietStart?: string | null;
    quietEnd?: string | null;
    quietTimezone?: string | null;
  }) =>
    apiFetch<UserPreferenceDto>("/api/v1/users/me/preferences", {
      method: "PATCH",
      body: JSON.stringify(data)
    }),
  getSessions: () => apiFetch<UserSessionDto[]>("/api/v1/auth/sessions"),
  revokeSession: (sessionId: string) =>
    apiFetch<{ message: string }>(`/api/v1/auth/sessions/${sessionId}`, {
      method: "DELETE"
    }),
  revokeAllSessions: (exceptCurrent = true) =>
    apiFetch<{ message: string }>(`/api/v1/auth/sessions?exceptCurrent=${exceptCurrent}`, {
      method: "DELETE"
    })
};
