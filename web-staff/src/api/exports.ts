import { api } from "./client";
import type { Invoice } from "./invoices";
import { listInvoices } from "./invoices";
import { eachIsoDate, localIsoDate } from "../exports/csv";

const PAGE_SIZE = 100;
const MAX_PAGES = 50;

export type AppointmentExportRow = {
  id: string;
  patientName: string;
  patientUhid: string;
  treatmentName: string;
  requestedDate: string;
  requestedTimeSlot: string;
  status: string;
};

type AppointmentPage = {
  items: AppointmentExportRow[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export async function collectAppointments(fromDate: string, toDate: string): Promise<AppointmentExportRow[]> {
  const rows: AppointmentExportRow[] = [];
  for (const date of eachIsoDate(fromDate, toDate)) {
    let page = 1;
    let seen = 0;
    while (page <= MAX_PAGES) {
      const search = new URLSearchParams({ date, page: String(page), pageSize: String(PAGE_SIZE) });
      const result = await api.request<AppointmentPage>(`/appointments?${search.toString()}`);
      rows.push(...result.items);
      seen += result.items.length;
      if (result.items.length === 0 || seen >= result.totalCount) break;
      page += 1;
    }
  }
  return rows;
}

export async function collectInvoices(fromDate: string, toDate: string): Promise<Invoice[]> {
  const matched: Invoice[] = [];
  for (let page = 1; page <= MAX_PAGES; page += 1) {
    const result = await listInvoices({ page, pageSize: PAGE_SIZE });
    if (result.items.length === 0) break;

    let olderThanRange = false;
    for (const invoice of result.items) {
      const day = localIsoDate(invoice.createdAt);
      if (day > toDate) continue;
      if (day < fromDate) {
        olderThanRange = true;
        continue;
      }
      matched.push(invoice);
    }

    if (olderThanRange || result.items.length < PAGE_SIZE || page * PAGE_SIZE >= result.totalCount) {
      break;
    }
  }
  return matched;
}
