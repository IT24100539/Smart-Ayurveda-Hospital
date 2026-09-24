export enum TreatmentCategory {
  Consultation = 0,
  Panchakarma = 1,
  Therapy = 2,
  Massage = 3,
  Other = 4
}

export interface ScheduleEntryDto {
  id: string;
  treatmentId: string;
  therapistId?: string;
  therapistName?: string;
  dayOfWeek: number; // 0 = Sunday, 6 = Saturday
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
  availableDays: number[]; // 0-6
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
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  maxSlotsPerDay: number;
  therapistId?: string;
  isActive?: boolean;
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

export const deactivateTreatment = async (id: string): Promise<void> => {
  await api.request(`/treatments/${id}/deactivate`, {
    method: 'PATCH'
  });
};

export const createScheduleEntry = async (treatmentId: string, data: CreateScheduleEntryRequest): Promise<ScheduleEntryDto> => {
  return api.request(`/treatments/${treatmentId}/schedule`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
};

export const deleteScheduleEntry = async (treatmentId: string, entryId: string): Promise<void> => {
  await api.request(`/treatments/${treatmentId}/schedule/${entryId}`, {
    method: 'DELETE'
  });
};
