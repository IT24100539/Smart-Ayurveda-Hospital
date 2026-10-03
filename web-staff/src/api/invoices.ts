import { api } from "./client";

export type InvoiceStatus = "Draft" | "Issued" | "Paid" | "Cancelled";
export type InvoiceLineSource = "Treatment" | "Admission";
export type InvoicePaymentMethod = "Cash" | "Card" | "BankTransfer";

export type InvoiceLineInput = {
  appointmentId: string | null;
  admissionId: string | null;
  quantity: number;
  unitPrice: number | null;
};

export type InvoiceLine = {
  id: string;
  source: InvoiceLineSource;
  appointmentId: string | null;
  treatmentId: string | null;
  admissionId: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type InvoicePayment = {
  id: string;
  amount: number;
  method: InvoicePaymentMethod;
  paidOn: string;
  reference: string | null;
  recordedByUserId: string;
};

export type Invoice = {
  id: string;
  invoiceNumber: string;
  patientId: string;
  currency: string;
  status: InvoiceStatus;
  total: number;
  amountPaid: number;
  balance: number;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  issuedAt: string | null;
  paidAt: string | null;
  cancelledAt: string | null;
  notes: string | null;
  lines: InvoiceLine[];
  payments: InvoicePayment[];
};

export type InvoicePage = {
  items: Invoice[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type VisitOption = {
  id: string;
  patientId: string;
  patientName: string;
  treatmentName: string;
  requestedDate: string;
  requestedTimeSlot: string;
  status: string;
};

export function listInvoices(params: { patientId?: string; status?: string; page?: number; pageSize?: number } = {}): Promise<InvoicePage> {
  const search = new URLSearchParams();
  if (params.patientId) search.set("patientId", params.patientId);
  if (params.status) search.set("status", params.status);
  search.set("page", String(params.page ?? 1));
  search.set("pageSize", String(params.pageSize ?? 20));
  return api.request<InvoicePage>(`/invoices?${search.toString()}`);
}

export function createInvoice(body: {
  patientId: string;
  currency: string | null;
  notes: string | null;
  lines: InvoiceLineInput[];
}): Promise<Invoice> {
  return api.request<Invoice>("/invoices", {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export function issueInvoice(id: string): Promise<Invoice> {
  return api.request<Invoice>(`/invoices/${id}/issue`, { method: "POST" });
}

export function cancelInvoice(id: string): Promise<Invoice> {
  return api.request<Invoice>(`/invoices/${id}/cancel`, { method: "POST" });
}

export function recordInvoicePayment(
  id: string,
  body: { amount: number; method: InvoicePaymentMethod; paidOn: string; reference: string | null }
): Promise<Invoice> {
  return api.request<Invoice>(`/invoices/${id}/payments`, {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export function listPatientVisits(patientId: string): Promise<{ items: VisitOption[] }> {
  const search = new URLSearchParams({ patientId, page: "1", pageSize: "100" });
  return api.request<{ items: VisitOption[] }>(`/appointments?${search.toString()}`);
}
