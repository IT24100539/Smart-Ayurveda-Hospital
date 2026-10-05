import { api } from "./client";

export const DOCUMENT_CATEGORIES = [
  "LabReport",
  "PrescriptionScan",
  "DiagnosticScan",
  "DischargeSummary",
  "TreatmentPlan",
  "General"
] as const;

export type DocumentCategory = (typeof DOCUMENT_CATEGORIES)[number];

export type MedicalDocument = {
  id: string;
  patientId: string;
  title: string;
  category: DocumentCategory;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  fileUrl: string;
  summary: string;
};

export type MedicalDocumentPage = {
  items: MedicalDocument[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function listMedicalDocuments(params: {
  patientId: string;
  page?: number;
  pageSize?: number;
}): Promise<MedicalDocumentPage> {
  const search = new URLSearchParams({
    patientId: params.patientId,
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 20)
  });
  return api.request<MedicalDocumentPage>(`/medical-documents?${search.toString()}`);
}

export function uploadMedicalDocument(input: {
  patientId: string;
  title: string;
  category: DocumentCategory;
  summary: string | null;
  file: File;
}): Promise<MedicalDocument> {
  const body = new FormData();
  body.set("patientId", input.patientId);
  body.set("title", input.title);
  body.set("category", input.category);
  if (input.summary) body.set("summary", input.summary);
  body.set("file", input.file);
  return api.request<MedicalDocument>("/medical-documents", { method: "POST", body });
}

export function downloadMedicalDocument(id: string): Promise<{ blob: Blob; fileName: string }> {
  return api.requestBlob(`/medical-documents/${id}/file`);
}
