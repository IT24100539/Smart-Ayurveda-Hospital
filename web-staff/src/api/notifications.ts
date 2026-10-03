import { api } from "./client";

export type StaffNotification = {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
};

export function listStaffNotifications(): Promise<StaffNotification[]> {
  return api.request<StaffNotification[]>("/notifications/staff");
}
