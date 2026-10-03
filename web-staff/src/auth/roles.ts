export type UserRole = "Patient" | "FrontDeskStaff" | "Doctor" | "Admin";

export type AuthUser = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  mustChangePassword?: boolean;
};

export const STAFF_ROLES: UserRole[] = ["Admin", "Doctor", "FrontDeskStaff"];

export const ROUTE_ROLES = {
  dashboard: STAFF_ROLES,
  patients: STAFF_ROLES,
  treatments: ["Admin", "Doctor"] as UserRole[],
  appointments: STAFF_ROLES,
  wards: ["Admin", "Doctor"] as UserRole[],
  feedback: STAFF_ROLES,
  aiApprovals: STAFF_ROLES,
  staffManagement: ["Admin"] as UserRole[],
  doctors: ["Admin"] as UserRole[],
  auditLogs: ["Admin"] as UserRole[],
  prescriptions: ["Doctor"] as UserRole[],
  billing: ["FrontDeskStaff", "Admin"] as UserRole[],
  documents: STAFF_ROLES,
  notifications: STAFF_ROLES,
  exports: STAFF_ROLES
};

export function isStaffRole(role: UserRole | undefined): boolean {
  return role !== undefined && STAFF_ROLES.includes(role);
}
