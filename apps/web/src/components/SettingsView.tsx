import { useState } from "react";
import { ProfileForm } from "./ProfileForm";
import { PreferencesForm } from "./PreferencesForm";
import { SessionsManager } from "./SessionsManager";
import type { UserDto } from "../types";

interface SettingsViewProps {
  user: UserDto;
  onUserUpdated: (user: UserDto) => void;
}

export function SettingsView({ user, onUserUpdated }: SettingsViewProps) {
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const handleSuccess = (msg: string) => {
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(null), 3500);
  };

  return (
    <div className="settings-container">
      {successMsg && (
        <div className="alert alert-success" role="status">
          {successMsg}
        </div>
      )}

      <ProfileForm user={user} onUserUpdated={onUserUpdated} onSuccess={handleSuccess} />
      <PreferencesForm user={user} onUserUpdated={onUserUpdated} onSuccess={handleSuccess} />
      <SessionsManager />
    </div>
  );
}
