import { api } from "./client";

export type Gender = "Female" | "Male" | "Other" | "Unspecified";
export type DoshaType = "None" | "Vata" | "Pitta" | "Kapha";

export type Patient = {
  id: string;
  uhid: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  phone: string;
  email: string | null;
  address: string | null;
  bloodGroup: string | null;
  allergies: string | null;
  prakriti: string;
  vikriti: string;
  isActive: boolean;
  createdAt: string;
};

export type CreatePatientRequest = {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  phone: string;
  email?: string | null;
  address?: string | null;
  bloodGroup?: string | null;
  allergies?: string | null;
  prakriti: DoshaType;
  vikriti: DoshaType;
};

export type PatientPage = {
  items: Patient[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function searchPatients(query?: string): Promise<PatientPage> {
  const params = new URLSearchParams({ page: "1", pageSize: "50" });
  if (query?.trim()) {
    params.set("q", query.trim());
  }
  return api.request<PatientPage>(`/patients?${params.toString()}`);
}

export function createPatient(body: CreatePatientRequest): Promise<Patient> {
  return api.request<Patient>("/patients", {
    method: "POST",
    body: JSON.stringify(body)
  });
}
