/** Matches Hospital.Domain.Enums.TreatmentCategory. The API serializes these as names. */
export const TREATMENT_CATEGORIES = [
  "Panchakarma",
  "Shirodhara",
  "HerbalSteam",
  "Nasya",
  "General",
  "Abhyanga",
  "Consultation"
] as const;

export type TreatmentCategory = (typeof TREATMENT_CATEGORIES)[number];

/** Sunday-first labels for the schedule grid. Values match the API Weekday names. */
export const WEEKDAYS = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday"
] as const;

export type WeekdayName = (typeof WEEKDAYS)[number];

export function formatCategory(category: string): string {
  return category.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export interface ScheduleEntryDto {
  id: string;
  treatmentId: string;
  therapistId?: string;
  therapistName?: string;
  dayOfWeek: WeekdayName;
  startTime: string; // "HH:mm:ss"
  endTime: string; // "HH:mm:ss"
  maxSlotsPerDay: number;
  isActive: boolean;
}

export interface TreatmentSummaryDto {
  id: string;
  name: string;
  nameSinhala: string;
  description: string;
  descriptionSinhala: string;
  category: TreatmentCategory;
  durationMinutes: number;
  unitPrice: number;
  isActive: boolean;
  availableDays: WeekdayName[];
}

export interface TreatmentDetailDto extends TreatmentSummaryDto {
  schedule: ScheduleEntryDto[];
}

export interface CreateTreatmentRequest {
  name: string;
  nameSinhala: string;
  description: string;
  descriptionSinhala: string;
  category: TreatmentCategory;
  durationMinutes: number;
  unitPrice: number;
  schedules?: CreateScheduleEntryRequest[];
}

export interface CreateScheduleEntryRequest {
  dayOfWeek: WeekdayName;
  startTime: string;
  endTime: string;
  maxSlotsPerDay: number;
  therapistId?: string;
  isActive?: boolean;
}

export interface UpdateScheduleEntryRequest extends CreateScheduleEntryRequest {
  isActive: boolean;
}

export interface UpdateTreatmentRequest {
  name: string;
  nameSinhala: string;
  description: string;
  descriptionSinhala: string;
  category: TreatmentCategory;
  durationMinutes: number;
  unitPrice: number;
}

export interface TreatmentSearchQuery {
  name?: string;
  category?: TreatmentCategory;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

import { api } from './client';

export const getTreatments = async (params: TreatmentSearchQuery): Promise<{ items: TreatmentSummaryDto[], totalCount: number }> => {
  const query = new URLSearchParams();
  if (params.name) query.append('Name', params.name);
  if (params.category !== undefined) query.append('Category', params.category.toString());
  if (params.activeOnly !== undefined) query.append('ActiveOnly', params.activeOnly.toString());
  if (params.page) query.append('Page', params.page.toString());
  if (params.pageSize) query.append('PageSize', params.pageSize.toString());
  if (params.sort) query.append('Sort', params.sort);

  return api.request(`/treatments?${query.toString()}`);
};

export const getTreatmentDetails = async (id: string): Promise<TreatmentDetailDto> => {
  return api.request(`/treatments/${id}`);
};

export const createTreatment = async (data: CreateTreatmentRequest): Promise<TreatmentDetailDto> => {
  return api.request('/treatments', {
    method: 'POST',
    body: JSON.stringify(data)
  });
};

export const updateTreatment = async (id: string, data: UpdateTreatmentRequest): Promise<TreatmentDetailDto> => {
  return api.request(`/treatments/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
};

export const deactivateTreatment = async (id: string): Promise<TreatmentDetailDto> => {
  return api.request(`/treatments/${id}/deactivate`, {
    method: 'PATCH'
  });
};

export const createScheduleEntry = async (treatmentId: string, data: CreateScheduleEntryRequest): Promise<ScheduleEntryDto> => {
  return api.request(`/treatments/${treatmentId}/schedule`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
};

export const updateScheduleEntry = async (
  treatmentId: string,
  entryId: string,
  data: UpdateScheduleEntryRequest
): Promise<ScheduleEntryDto> => {
  return api.request(`/treatments/${treatmentId}/schedule/${entryId}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
};

export const deleteScheduleEntry = async (treatmentId: string, entryId: string): Promise<void> => {
  await api.request(`/treatments/${treatmentId}/schedule/${entryId}`, {
    method: 'DELETE'
  });
};
