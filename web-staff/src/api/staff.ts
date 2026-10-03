import { api, type AuthResponse } from "./client";
import type { UserRole } from "../auth/roles";

export type StaffUser = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
  updatedAt: string;
};

export type StaffListResponse = {
  items: StaffUser[];
  total: number;
  page: number;
  pageSize: number;
};

export type AuditLogEntry = {
  id: string;
  createdAt: string;
  actorUserId: string | null;
  actorEmail: string;
  actorRole?: string | null;
  action: string;
  entityName?: string;
  entityId?: string;
  targetUserId: string;
  targetEmail: string;
  details: string;
};

export type AuditLogListResponse = {
  items: AuditLogEntry[];
  total: number;
  page: number;
  pageSize: number;
};

export type ForcePasswordResetResponse = {
  userId: string;
  email: string;
  temporaryPassword: string;
};

export const staffApi = {
  listStaff: (params?: { query?: string; role?: string; isActive?: boolean; page?: number; pageSize?: number }) => {
    const sp = new URLSearchParams();
    if (params?.query) sp.set("query", params.query);
    if (params?.role) sp.set("role", params.role);
    if (params?.isActive !== undefined) sp.set("isActive", String(params.isActive));
    if (params?.page) sp.set("page", String(params.page));
    if (params?.pageSize) sp.set("pageSize", String(params.pageSize));
    const queryStr = sp.toString() ? `?${sp.toString()}` : "";
    return api.request<StaffListResponse>(`/admin/staff${queryStr}`);
  },

  createStaff: (data: {
    fullName: string;
    email: string;
    phoneNumber: string;
    role: UserRole;
    temporaryPassword?: string;
  }) =>
    api.request<StaffUser>("/admin/staff", {
      method: "POST",
      body: JSON.stringify(data)
    }),

  updateRole: (id: string, role: UserRole) =>
    api.request<StaffUser>(`/admin/staff/${id}/role`, {
      method: "PATCH",
      body: JSON.stringify({ role })
    }),

  updateStatus: (id: string, isActive: boolean) =>
    api.request<StaffUser>(`/admin/staff/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive })
    }),

  forcePasswordReset: (id: string, temporaryPassword?: string) =>
    api.request<ForcePasswordResetResponse>(`/admin/staff/${id}/force-password-reset`, {
      method: "POST",
      body: JSON.stringify({ temporaryPassword })
    }),

  listAuditLogs: (page = 1, pageSize = 50) =>
    api.request<AuditLogListResponse>(`/admin/staff/audit-logs?page=${page}&pageSize=${pageSize}`),

  changePassword: (data: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }) =>
    api.request<AuthResponse>("/auth/change-password", {
      method: "POST",
      body: JSON.stringify(data)
    })
};
