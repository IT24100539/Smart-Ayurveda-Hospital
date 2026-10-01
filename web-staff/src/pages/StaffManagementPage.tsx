import { useEffect, useState, useCallback, type FormEvent, type ChangeEvent } from "react";
import { staffApi, type StaffUser, type AuditLogEntry } from "../api/staff";
import { useAuthStore } from "../store/authStore";
import { type UserRole, STAFF_ROLES } from "../auth/roles";
import { Button } from "../components/ui";

export function StaffManagementPage() {
  const currentUser = useAuthStore((state) => state.user);

  const [activeTab, setActiveTab] = useState<"staff" | "audit">("staff");
  const [staffList, setStaffList] = useState<StaffUser[]>([]);
  const [totalStaff, setTotalStaff] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Audit logs state
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [auditLoading, setAuditLoading] = useState(false);

  // Modals state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showRoleModal, setShowRoleModal] = useState<StaffUser | null>(null);
  const [showStatusModal, setShowStatusModal] = useState<StaffUser | null>(null);
  const [showResetModal, setShowResetModal] = useState<StaffUser | null>(null);
  const [resetSuccessModal, setResetSuccessModal] = useState<{ email: string; tempPass: string } | null>(null);

  // Create form state
  const [createName, setCreateName] = useState("");
  const [createEmail, setCreateEmail] = useState("");
  const [createPhone, setCreatePhone] = useState("");
  const [createRole, setCreateRole] = useState<UserRole>("Doctor");
  const [createTempPass, setCreateTempPass] = useState("");
  const [createLoading, setCreateLoading] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // Role form state
  const [newRole, setNewRole] = useState<UserRole>("Doctor");
  const [roleLoading, setRoleLoading] = useState(false);
  const [roleError, setRoleError] = useState<string | null>(null);

  // Status form state
  const [statusLoading, setStatusLoading] = useState(false);
  const [statusError, setStatusError] = useState<string | null>(null);

  // Reset form state
  const [resetPassInput, setResetPassInput] = useState("");
  const [resetLoading, setResetLoading] = useState(false);
  const [resetError, setResetError] = useState<string | null>(null);

  const fetchStaff = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await staffApi.listStaff({
        query: search || undefined,
        role: roleFilter || undefined,
        isActive: statusFilter ? statusFilter === "active" : undefined,
        page,
        pageSize: 15
      });
      setStaffList(res.items);
      setTotalStaff(res.total);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load staff accounts.");
    } finally {
      setLoading(false);
    }
  }, [search, roleFilter, statusFilter, page]);

  const fetchAuditLogs = useCallback(async () => {
    setAuditLoading(true);
    try {
      const res = await staffApi.listAuditLogs(1, 50);
      setAuditLogs(res.items);
    } catch (err: unknown) {
      console.error("Failed to load audit logs", err);
    } finally {
      setAuditLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === "staff") {
      void fetchStaff();
    } else {
      void fetchAuditLogs();
    }
  }, [activeTab, fetchStaff, fetchAuditLogs]);

  const handleCreateSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    setCreateLoading(true);

    try {
      await staffApi.createStaff({
        fullName: createName.trim(),
        email: createEmail.trim(),
        phoneNumber: createPhone.trim(),
        role: createRole,
        temporaryPassword: createTempPass.trim() || undefined
      });
      setShowCreateModal(false);
      setCreateName("");
      setCreateEmail("");
      setCreatePhone("");
      setCreateTempPass("");
      void fetchStaff();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : "Failed to create staff account.");
    } finally {
      setCreateLoading(false);
    }
  };

  const handleRoleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!showRoleModal) return;
    setRoleError(null);
    setRoleLoading(true);

    try {
      await staffApi.updateRole(showRoleModal.id, newRole);
      setShowRoleModal(null);
      void fetchStaff();
    } catch (err: unknown) {
      setRoleError(err instanceof Error ? err.message : "Failed to update role.");
    } finally {
      setRoleLoading(false);
    }
  };

  const handleStatusSubmit = async () => {
    if (!showStatusModal) return;
    setStatusError(null);
    setStatusLoading(true);

    try {
      await staffApi.updateStatus(showStatusModal.id, !showStatusModal.isActive);
      setShowStatusModal(null);
      void fetchStaff();
    } catch (err: unknown) {
      setStatusError(err instanceof Error ? err.message : "Failed to change account status.");
    } finally {
      setStatusLoading(false);
    }
  };

  const handleResetSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!showResetModal) return;
    setResetError(null);
    setResetLoading(true);

    try {
      const res = await staffApi.forcePasswordReset(showResetModal.id, resetPassInput.trim() || undefined);
      setShowResetModal(null);
      setResetPassInput("");
      setResetSuccessModal({ email: res.email, tempPass: res.temporaryPassword });
      void fetchStaff();
    } catch (err: unknown) {
      setResetError(err instanceof Error ? err.message : "Failed to force password reset.");
    } finally {
      setResetLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-display text-2xl font-bold text-ink">Staff Account Management</h1>
          <p className="mt-1 text-sm text-muted">
            Manage hospital staff credentials, roles, account activation and audit trail.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant={activeTab === "staff" ? "primary" : "secondary"}
            onClick={() => setActiveTab("staff")}
          >
            Staff Directory
          </Button>
          <Button
            variant={activeTab === "audit" ? "primary" : "secondary"}
            onClick={() => setActiveTab("audit")}
          >
            Audit Logs
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              setShowCreateModal(true);
              setCreateError(null);
            }}
            className="shadow-sm"
          >
            + Add Staff Member
          </Button>
        </div>
      </div>

      {activeTab === "staff" ? (
        <>
          {/* Filters Bar */}
          <div className="flex flex-wrap items-center gap-3 rounded-xl border border-surface-border bg-surface-raised p-4 shadow-sm">
            <div className="min-w-[200px] flex-1">
              <input
                placeholder="Search by name, email, or phone..."
                value={search}
                onChange={(e: ChangeEvent<HTMLInputElement>) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
                className="w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <select
                aria-label="Filter by staff role"
                value={roleFilter}
                onChange={(e: ChangeEvent<HTMLSelectElement>) => {
                  setRoleFilter(e.target.value);
                  setPage(1);
                }}
                className="rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm"
              >
                <option value="">All Roles</option>
                <option value="Admin">Admin</option>
                <option value="Doctor">Doctor</option>
                <option value="FrontDeskStaff">Front Desk Staff</option>
              </select>
            </div>
            <div>
              <select
                aria-label="Filter by account status"
                value={statusFilter}
                onChange={(e: ChangeEvent<HTMLSelectElement>) => {
                  setStatusFilter(e.target.value);
                  setPage(1);
                }}
                className="rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm"
              >
                <option value="">All Statuses</option>
                <option value="active">Active Only</option>
                <option value="inactive">Deactivated Only</option>
              </select>
            </div>
          </div>

          {/* Error Notice */}
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              {error}
            </div>
          )}

          {/* Staff Table / Cards */}
          <div className="overflow-hidden rounded-xl border border-surface-border bg-surface-raised shadow-sm">
            {loading ? (
              <div className="flex min-h-[240px] items-center justify-center p-8 text-sm text-muted">
                Loading staff accounts...
              </div>
            ) : staffList.length === 0 ? (
              <div className="flex min-h-[240px] flex-col items-center justify-center p-8 text-center">
                <p className="text-base font-semibold text-ink">No staff accounts found</p>
                <p className="mt-1 text-sm text-muted">Try adjusting your search criteria or add a new staff member.</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="border-b border-surface-border bg-surface text-xs font-semibold uppercase tracking-wider text-muted">
                    <tr>
                      <th className="px-4 py-3">Staff Member</th>
                      <th className="px-4 py-3">Role</th>
                      <th className="px-4 py-3">Contact</th>
                      <th className="px-4 py-3">Status</th>
                      <th className="px-4 py-3">Password Status</th>
                      <th className="px-4 py-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-border">
                    {staffList.map((item) => {
                      const isSelf = currentUser?.id === item.id;
                      return (
                        <tr key={item.id} className="transition hover:bg-surface/50">
                          <td className="px-4 py-3 font-medium text-ink">
                            <div>{item.fullName}</div>
                            <div className="text-xs text-muted">{item.email}</div>
                          </td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-semibold text-primary">
                              {item.role}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-muted">{item.phoneNumber || "—"}</td>
                          <td className="px-4 py-3">
                            {item.isActive ? (
                              <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-700">
                                <span className="h-1.5 w-1.5 rounded-full bg-emerald-600" />
                                Active
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1.5 rounded-full bg-rose-50 px-2.5 py-0.5 text-xs font-medium text-rose-700">
                                <span className="h-1.5 w-1.5 rounded-full bg-rose-600" />
                                Deactivated
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            {item.mustChangePassword ? (
                              <span className="inline-flex items-center rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-800">
                                Must Change Password
                              </span>
                            ) : (
                              <span className="text-xs text-muted">Normal</span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <Button
                                variant="secondary"
                                disabled={isSelf}
                                onClick={() => {
                                  setShowRoleModal(item);
                                  setNewRole(item.role);
                                  setRoleError(null);
                                }}
                                className="text-xs py-1 px-2.5"
                                title={isSelf ? "You cannot modify your own role" : "Change staff role"}
                              >
                                Role
                              </Button>

                              <Button
                                variant="secondary"
                                disabled={isSelf}
                                onClick={() => {
                                  setShowStatusModal(item);
                                  setStatusError(null);
                                }}
                                className={`text-xs py-1 px-2.5 ${item.isActive ? "hover:text-red-700" : "hover:text-emerald-700"}`}
                                title={isSelf ? "You cannot deactivate yourself" : item.isActive ? "Deactivate account" : "Reactivate account"}
                              >
                                {item.isActive ? "Deactivate" : "Activate"}
                              </Button>

                              <Button
                                variant="secondary"
                                onClick={() => {
                                  setShowResetModal(item);
                                  setResetPassInput("");
                                  setResetError(null);
                                }}
                                className="text-xs py-1 px-2.5"
                                title="Force password reset"
                              >
                                Reset Pass
                              </Button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}

            {/* Pagination */}
            {totalStaff > 15 && (
              <div className="flex items-center justify-between border-t border-surface-border px-4 py-3">
                <p className="text-xs text-muted">
                  Showing {(page - 1) * 15 + 1} to {Math.min(page * 15, totalStaff)} of {totalStaff} staff members
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="secondary"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="text-xs"
                  >
                    Previous
                  </Button>
                  <Button
                    variant="secondary"
                    disabled={page * 15 >= totalStaff}
                    onClick={() => setPage((p) => p + 1)}
                    className="text-xs"
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}
          </div>
        </>
      ) : (
        /* Audit Logs View */
        <div className="overflow-hidden rounded-xl border border-surface-border bg-surface-raised shadow-sm">
          <div className="border-b border-surface-border bg-surface p-4">
            <h2 className="font-display text-base font-semibold text-ink">Staff Management Audit Trail</h2>
            <p className="text-xs text-muted">Complete ledger of role assignments, status changes, and forced resets.</p>
          </div>
          {auditLoading ? (
            <div className="flex min-h-[200px] items-center justify-center p-8 text-sm text-muted">
              Loading audit logs...
            </div>
          ) : auditLogs.length === 0 ? (
            <div className="p-8 text-center text-sm text-muted">No audit log records recorded yet.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-surface-border bg-surface text-xs font-semibold uppercase tracking-wider text-muted">
                  <tr>
                    <th className="px-4 py-3">Timestamp</th>
                    <th className="px-4 py-3">Actor</th>
                    <th className="px-4 py-3">Action</th>
                    <th className="px-4 py-3">Target</th>
                    <th className="px-4 py-3">Details</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-border">
                  {auditLogs.map((log) => (
                    <tr key={log.id} className="transition hover:bg-surface/50">
                      <td className="whitespace-nowrap px-4 py-3 text-xs text-muted">
                        {new Date(log.timestamp).toLocaleString()}
                      </td>
                      <td className="px-4 py-3 font-medium text-ink">{log.actorEmail}</td>
                      <td className="px-4 py-3">
                        <span className="inline-flex rounded-full bg-surface-border px-2 py-0.5 text-xs font-medium text-ink">
                          {log.action}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-ink-muted">{log.targetEmail}</td>
                      <td className="px-4 py-3 text-xs text-muted">{log.details}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* CREATE STAFF MODAL */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-lg rounded-2xl border border-surface-border bg-surface-raised p-6 shadow-xl">
            <h3 className="font-display text-lg font-semibold text-ink">Add New Staff Member</h3>
            <p className="mt-1 text-xs text-muted">
              Create an account with role and temporary password. User will be required to change password on first login.
            </p>

            {createError && (
              <div className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                {createError}
              </div>
            )}

            <form onSubmit={handleCreateSubmit} className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-semibold uppercase text-muted">Full Name</label>
                <input
                  required
                  value={createName}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setCreateName(e.target.value)}
                  placeholder="e.g. Dr. Haritha Bandara"
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-muted">Work Email</label>
                <input
                  type="email"
                  required
                  value={createEmail}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setCreateEmail(e.target.value)}
                  placeholder="e.g. haritha@smartayurveda.local"
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-muted">Phone Number</label>
                <input
                  required
                  value={createPhone}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setCreatePhone(e.target.value)}
                  placeholder="e.g. 0771234567"
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-muted">Role</label>
                <select
                  aria-label="Staff role selection"
                  value={createRole}
                  onChange={(e: ChangeEvent<HTMLSelectElement>) => setCreateRole(e.target.value as UserRole)}
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm"
                >
                  {STAFF_ROLES.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-muted">
                  Temporary Password (optional)
                </label>
                <input
                  type="text"
                  value={createTempPass}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setCreateTempPass(e.target.value)}
                  placeholder="Leave empty to auto-generate"
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 font-mono text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div className="mt-6 flex justify-end gap-3 pt-3">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setShowCreateModal(false)}
                >
                  Cancel
                </Button>
                <Button type="submit" variant="primary" disabled={createLoading}>
                  {createLoading ? "Creating..." : "Create Staff Account"}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* CHANGE ROLE MODAL */}
      {showRoleModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-surface-border bg-surface-raised p-6 shadow-xl">
            <h3 className="font-display text-lg font-semibold text-ink">Change Staff Role</h3>
            <p className="mt-1 text-xs text-muted">
              Select a new role for <span className="font-semibold text-ink">{showRoleModal.fullName}</span> ({showRoleModal.email}).
            </p>

            {roleError && (
              <div className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                {roleError}
              </div>
            )}

            <form onSubmit={handleRoleSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-muted">New Role</label>
                <select
                  aria-label="New role assignment"
                  value={newRole}
                  onChange={(e: ChangeEvent<HTMLSelectElement>) => setNewRole(e.target.value as UserRole)}
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 text-sm text-ink shadow-sm"
                >
                  {STAFF_ROLES.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setShowRoleModal(null)}
                >
                  Cancel
                </Button>
                <Button type="submit" variant="primary" disabled={roleLoading}>
                  {roleLoading ? "Updating..." : "Save Role Change"}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DEACTIVATE / REACTIVATE CONFIRMATION MODAL */}
      {showStatusModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-surface-border bg-surface-raised p-6 shadow-xl">
            <h3 className="font-display text-lg font-semibold text-ink">
              {showStatusModal.isActive ? "Deactivate Account" : "Reactivate Account"}
            </h3>
            <p className="mt-2 text-sm text-ink-muted">
              Are you sure you want to {showStatusModal.isActive ? "deactivate" : "reactivate"} the account of{" "}
              <span className="font-semibold text-ink">{showStatusModal.fullName}</span> ({showStatusModal.email})?
            </p>
            {showStatusModal.isActive && (
              <p className="mt-1 text-xs text-amber-700">
                Note: Deactivating will immediately invalidate all active login sessions and tokens for this user.
              </p>
            )}

            {statusError && (
              <div className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                {statusError}
              </div>
            )}

            <div className="mt-6 flex justify-end gap-3">
              <Button
                type="button"
                variant="secondary"
                onClick={() => setShowStatusModal(null)}
              >
                Cancel
              </Button>
              <Button
                type="button"
                variant={showStatusModal.isActive ? "secondary" : "primary"}
                onClick={handleStatusSubmit}
                disabled={statusLoading}
                className={showStatusModal.isActive ? "border-red-300 text-red-700 hover:bg-red-50" : ""}
              >
                {statusLoading ? "Updating..." : showStatusModal.isActive ? "Confirm Deactivation" : "Confirm Reactivation"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* FORCE PASSWORD RESET MODAL */}
      {showResetModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-surface-border bg-surface-raised p-6 shadow-xl">
            <h3 className="font-display text-lg font-semibold text-ink">Force Password Reset</h3>
            <p className="mt-1 text-xs text-muted">
              Forcing a reset immediately invalidates active sessions and sets a temporary password for{" "}
              <span className="font-semibold text-ink">{showResetModal.fullName}</span>.
            </p>

            {resetError && (
              <div className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
                {resetError}
              </div>
            )}

            <form onSubmit={handleResetSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-muted">
                  Custom Temporary Password (optional)
                </label>
                <input
                  type="text"
                  value={resetPassInput}
                  onChange={(e: ChangeEvent<HTMLInputElement>) => setResetPassInput(e.target.value)}
                  placeholder="Leave empty for auto-generated password"
                  className="mt-1 w-full rounded-lg border border-surface-border bg-surface px-3 py-2 font-mono text-sm text-ink shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setShowResetModal(null)}
                >
                  Cancel
                </Button>
                <Button type="submit" variant="primary" disabled={resetLoading}>
                  {resetLoading ? "Resetting..." : "Force Reset"}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* PASSWORD RESET SUCCESS MODAL */}
      {resetSuccessModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-surface-border bg-surface-raised p-6 shadow-xl">
            <h3 className="font-display text-lg font-semibold text-emerald-800">Password Reset Generated</h3>
            <p className="mt-2 text-sm text-ink-muted">
              The temporary password for <span className="font-semibold text-ink">{resetSuccessModal.email}</span> has been set. Share this credential securely:
            </p>

            <div className="mt-4 rounded-xl border border-surface-border bg-surface p-4 text-center">
              <span className="font-mono text-lg font-bold tracking-wider text-ink select-all">
                {resetSuccessModal.tempPass}
              </span>
            </div>

            <p className="mt-3 text-xs text-muted">
              The staff member will be required to change this temporary password immediately upon signing in.
            </p>

            <div className="mt-6 flex justify-end">
              <Button
                type="button"
                variant="primary"
                onClick={() => setResetSuccessModal(null)}
              >
                Done
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
