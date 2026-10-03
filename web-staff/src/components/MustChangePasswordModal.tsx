import { useState, type FormEvent, type ChangeEvent } from "react";
import { useAuthStore } from "../store/authStore";
import { staffApi } from "../api/staff";
import { Button, Modal } from "./ui";

export function MustChangePasswordModal() {
  const user = useAuthStore((state) => state.user);
  const login = useAuthStore((state) => state.login);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!user?.mustChangePassword) {
    return null;
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!currentPassword) {
      setError("Please enter your current temporary password.");
      return;
    }

    if (newPassword.length < 8) {
      setError("New password must be at least 8 characters long.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("New password and confirmation do not match.");
      return;
    }

    if (currentPassword === newPassword) {
      setError("New password cannot be the same as your temporary password.");
      return;
    }

    setLoading(true);
    try {
      const res = await staffApi.changePassword({
        currentPassword,
        newPassword,
        confirmPassword
      });
      login(res.token, res.user);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to change password. Please check your credentials.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title="Password Change Required"
      description="First-time login / Temporary password detected"
      icon={
        <svg viewBox="0 0 24 24" className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M12 9v4M12 17h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
        </svg>
      }
    >
        <p className="mt-3 text-sm text-muted">
          Your account has a temporary password assigned by an administrator. For security reasons, you must set a new secure password before accessing the hospital staff portal.
        </p>

        {error && (
          <div className="mt-3 rounded-lg border border-status-error-fg/30 bg-status-error-bg p-3 text-sm text-status-error-fg">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted">
              Current Temporary Password
            </label>
            <input
              type="password"
              required
              value={currentPassword}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setCurrentPassword(e.target.value)}
              placeholder="Enter temporary password"
              className="field mt-1"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted">
              New Password
            </label>
            <input
              type="password"
              required
              value={newPassword}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setNewPassword(e.target.value)}
              placeholder="Min. 8 characters"
              className="field mt-1"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted">
              Confirm New Password
            </label>
            <input
              type="password"
              required
              value={confirmPassword}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setConfirmPassword(e.target.value)}
              placeholder="Re-enter new password"
              className="field mt-1"
            />
          </div>

          <div className="pt-2">
            <Button
              type="submit"
              variant="primary"
              disabled={loading}
              className="w-full justify-center py-2.5 shadow-md"
            >
              {loading ? "Updating password..." : "Update Password & Continue"}
            </Button>
          </div>
        </form>
    </Modal>
  );
}
