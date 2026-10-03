import { api } from "./client";

export type PrescriptionStatus = "Draft" | "Issued" | "Superseded" | "Cancelled";

export type PrescriptionItemInput = {
  name: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string | null;
};

export type PrescriptionItem = {
  id: string;
  name: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string;
};

export type Prescription = {
  id: string;
  patientId: string;
  appointmentId: string;
  doctorUserId: string;
  doctorName: string;
  status: PrescriptionStatus;
  revisionNumber: number;
  rootPrescriptionId: string;
  revisesPrescriptionId: string | null;
  supersededByPrescriptionId: string | null;
  createdAt: string;
  updatedAt: string;
  issuedAt: string | null;
  cancelledAt: string | null;
  supersededAt: string | null;
  items: PrescriptionItem[];
};

export type PrescriptionRevision = {
  id: string;
  previousPrescriptionId: string;
  revisedPrescriptionId: string;
  revisionNumber: number;
  revisedByUserId: string;
  reason: string;
  revisedAt: string;
};

export type PrescriptionHistory = {
  rootPrescriptionId: string;
  versions: Prescription[];
  revisions: PrescriptionRevision[];
};

export type PrescriptionPage = {
  items: Prescription[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function listPrescriptions(params: { patientId?: string; appointmentId?: string; page?: number; pageSize?: number } = {}): Promise<PrescriptionPage> {
  const search = new URLSearchParams();
  if (params.patientId) search.set("patientId", params.patientId);
  if (params.appointmentId) search.set("appointmentId", params.appointmentId);
  search.set("page", String(params.page ?? 1));
  search.set("pageSize", String(params.pageSize ?? 20));
  return api.request<PrescriptionPage>(`/prescriptions?${search.toString()}`);
}

export function getPrescriptionHistory(id: string): Promise<PrescriptionHistory> {
  return api.request<PrescriptionHistory>(`/prescriptions/${id}/history`);
}

export function createPrescription(body: {
  patientId: string;
  appointmentId: string;
  items: PrescriptionItemInput[];
}): Promise<Prescription> {
  return api.request<Prescription>("/prescriptions", {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export function issuePrescription(id: string): Promise<Prescription> {
  return api.request<Prescription>(`/prescriptions/${id}/issue`, { method: "POST" });
}

export function revisePrescription(
  id: string,
  body: { reason: string; items: PrescriptionItemInput[] }
): Promise<Prescription> {
  return api.request<Prescription>(`/prescriptions/${id}/revise`, {
    method: "POST",
    body: JSON.stringify(body)
  });
}
