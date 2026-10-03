import { api } from "./client";

/** Physician profile. This is not a staff login account. */
export type Doctor = {
  id: string;
  name: string;
  specialty: string;
  qualifications: string;
  bio: string | null;
  isActive: boolean;
  isSample: boolean;
  hasPhoto: boolean;
  photoUrl: string | null;
  rating?: number | null;
  ratingCount?: number | null;
};

export type DoctorInput = {
  name: string;
  specialty: string;
  qualifications: string;
  bio: string | null;
};

export type DoctorPage = {
  items: Doctor[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type DoctorListParams = {
  query?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
};

export function listDoctors(params: DoctorListParams = {}): Promise<DoctorPage> {
  const search = new URLSearchParams();
  const query = params.query?.trim();
  if (query) {
    search.set("query", query);
  }
  if (params.activeOnly) {
    search.set("activeOnly", "true");
  }
  search.set("page", String(params.page ?? 1));
  search.set("pageSize", String(params.pageSize ?? 20));
  return api.request<DoctorPage>(`/doctors?${search.toString()}`);
}

export function createDoctor(body: DoctorInput): Promise<Doctor> {
  return api.request<Doctor>("/doctors", {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export function updateDoctor(id: string, body: DoctorInput): Promise<Doctor> {
  return api.request<Doctor>(`/doctors/${id}`, {
    method: "PUT",
    body: JSON.stringify(body)
  });
}

export function deactivateDoctor(id: string): Promise<Doctor> {
  return api.request<Doctor>(`/doctors/${id}/deactivate`, { method: "POST" });
}

export function uploadDoctorPhoto(id: string, photo: File): Promise<Doctor> {
  const body = new FormData();
  body.append("photo", photo);
  return api.request<Doctor>(`/doctors/${id}/photo`, { method: "POST", body });
}

export function removeDoctorPhoto(id: string): Promise<Doctor> {
  return api.request<Doctor>(`/doctors/${id}/photo`, { method: "DELETE" });
}
