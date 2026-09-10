export type UserRole = "Patient" | "FrontDeskStaff" | "Doctor" | "Admin";

export type AuthUser = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
};

export const STAFF_ROLES: UserRole[] = ["Admin", "Doctor", "FrontDeskStaff"];

export const ROUTE_ROLES = {
  dashboard: STAFF_ROLES,
  patients: STAFF_ROLES,
  treatments: ["Admin", "Doctor"] as UserRole[],
  appointments: STAFF_ROLES,
  wards: ["Admin", "Doctor"] as UserRole[],
  feedback: ["Admin", "FrontDeskStaff"] as UserRole[],
  aiApprovals: ["Admin", "Doctor"] as UserRole[]
};

export function isStaffRole(role: UserRole | undefined): boolean {
  return role !== undefined && STAFF_ROLES.includes(role);
}
