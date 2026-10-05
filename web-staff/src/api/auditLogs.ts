import { api } from "./client";

export type AuditLog = {
  id: string;
  actorUserId: string | null;
  actorEmail: string;
  actorRole: string | null;
  action: string;
  entityName: string;
  entityId: string;
  targetUserId: string;
  targetEmail: string;
  details: string;
  createdAt: string;
  ipAddress: string | null;
};

export type AuditLogPage = {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type AuditLogQuery = {
  query?: string;
  action?: string;
  entityName?: string;
  entityId?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
};

export function listAuditLogs(params: AuditLogQuery = {}): Promise<AuditLogPage> {
  const search = new URLSearchParams();
  const query = params.query?.trim();
  if (query) {
    search.set("query", query);
  }
  if (params.action) {
    search.set("action", params.action);
  }
  if (params.entityName) {
    search.set("entityName", params.entityName);
  }
  const entityId = params.entityId?.trim();
  if (entityId) {
    search.set("entityId", entityId);
  }
  if (params.fromDate) {
    search.set("fromDate", params.fromDate);
  }
  if (params.toDate) {
    search.set("toDate", params.toDate);
  }
  search.set("page", String(params.page ?? 1));
  search.set("pageSize", String(params.pageSize ?? 20));
  return api.request<AuditLogPage>(`/audit-logs?${search.toString()}`);
}
