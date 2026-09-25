import { useEffect, useMemo, useState } from "react";
import { ApiError, api } from "../api/client";
import { PageHeader } from "../components/ui";
import { useAuthStore } from "../store/authStore";

type AppointmentStatus = "Pending" | "Approved" | "Rejected" | "Cancelled" | "Completed";

type Appointment = {
  id: string;
  patientName: string;
  treatmentId: string;
  treatmentName: string;
  requestedDate: string;
  requestedTimeSlot: string;
  status: AppointmentStatus;
};

type AppointmentPage = { items: Appointment[] };

const statusStyles: Record<AppointmentStatus, string> = {
  Pending: "bg-amber-100 text-amber-800",
  Approved: "bg-emerald-100 text-emerald-800",
  Rejected: "bg-red-100 text-red-700",
  Cancelled: "bg-slate-100 text-slate-600",
  Completed: "bg-primary-muted text-primary-dark"
};

function startOfWeek(date: Date): Date {
  const result = new Date(date);
  const day = result.getDay();
  result.setDate(result.getDate() - (day === 0 ? 6 : day - 1));
  result.setHours(0, 0, 0, 0);
  return result;
}

function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

function isoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : "Something went wrong. Please try again.";
}

export function AppointmentsPage() {
  const staffId = useAuthStore((state) => state.user?.id);
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date()));
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [selectedTreatment, setSelectedTreatment] = useState("all");
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [note, setNote] = useState("");
  const [loading, setLoading] = useState(true);
  const [deciding, setDeciding] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const days = useMemo(() => Array.from({ length: 5 }, (_, index) => addDays(weekStart, index)), [weekStart]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all(
      days.map((day) => api.request<AppointmentPage>(`/appointments?date=${isoDate(day)}&pageSize=100`))
    )
      .then((pages) => {
        if (active) setAppointments(pages.flatMap((page) => page.items));
      })
      .catch((caught) => {
        if (active) setError(errorMessage(caught));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [days, refreshKey]);

  const treatments = useMemo(
    () => Array.from(new Set(appointments.map((item) => item.treatmentName))).sort(),
    [appointments]
  );

  async function decide(status: "Approved" | "Rejected") {
    if (!selected || !staffId) return;
    if (!window.confirm(`${status === "Approved" ? "Approve" : "Reject"} this appointment?`)) return;
    setDeciding(true);
    setError(null);
    try {
      await api.request(`/appointments/${selected.id}/decision`, {
        method: "PATCH",
        body: JSON.stringify({ status, decidedBy: staffId, note: note.trim() || null })
      });
      setSelected(null);
      setNote("");
      setRefreshKey((value) => value + 1);
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setDeciding(false);
    }
  }

  return (
    <section className="mx-auto max-w-7xl space-y-5">
      <PageHeader
        kicker="Care schedule"
        title="Appointment calendar"
        description="Review the working week and action pending requests."
      />
      <img
        src="/images/consultation-desk.png"
        alt="Consultation desk with an appointment book and tulsi"
        className="h-44 w-full rounded-2xl object-cover shadow-md"
      />
      <div className="flex flex-wrap items-end justify-between gap-4">
        <label className="text-sm font-medium text-ink">
          Treatment
          <select
            className="ml-2 rounded-lg border border-surface-border bg-surface-raised px-3 py-2 text-sm"
            value={selectedTreatment}
            onChange={(event) => setSelectedTreatment(event.target.value)}
          >
            <option value="all">All treatments</option>
            {treatments.map((treatment) => <option key={treatment}>{treatment}</option>)}
          </select>
        </label>
      </div>

      <div className="flex items-center justify-between rounded-xl border border-surface-border bg-surface-raised px-4 py-3">
        <button className="rounded-lg border border-surface-border px-3 py-1.5 text-sm font-semibold text-primary hover:bg-primary-muted" onClick={() => setWeekStart(addDays(weekStart, -7))}>← Previous</button>
        <p className="font-serif font-semibold text-ink">
          {days[0].toLocaleDateString(undefined, { month: "short", day: "numeric" })} – {days[4].toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" })}
        </p>
        <button className="rounded-lg border border-surface-border px-3 py-1.5 text-sm font-semibold text-primary hover:bg-primary-muted" onClick={() => setWeekStart(addDays(weekStart, 7))}>Next →</button>
      </div>

      {error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-danger">{error}</div>}
      {loading ? (
        <div className="rounded-xl border border-surface-border bg-surface-raised p-8 text-center text-muted">Loading appointments…</div>
      ) : (
        <div className="grid min-h-[30rem] grid-cols-1 overflow-hidden rounded-xl border border-surface-border bg-surface-raised md:grid-cols-5">
          {days.map((day) => {
            const date = isoDate(day);
            const items = appointments.filter((item) => item.requestedDate === date && (selectedTreatment === "all" || item.treatmentName === selectedTreatment));
            return (
              <div key={date} className="border-b border-surface-border p-3 md:border-b-0 md:border-r last:border-r-0">
                <div className="mb-3 border-b border-surface-border pb-2">
                  <p className="text-xs font-semibold uppercase tracking-wide text-muted">{day.toLocaleDateString(undefined, { weekday: "short" })}</p>
                  <p className="font-serif text-lg font-semibold text-ink">{day.toLocaleDateString(undefined, { month: "short", day: "numeric" })}</p>
                </div>
                <div className="space-y-2">
                  {items.length === 0 && <p className="py-6 text-center text-sm text-muted">No appointments</p>}
                  {items.map((appointment) => (
                    <button
                      key={appointment.id}
                      type="button"
                      disabled={appointment.status !== "Pending"}
                      onClick={() => { setSelected(appointment); setNote(""); }}
                      className="w-full rounded-lg border border-surface-border bg-white p-3 text-left shadow-sm transition hover:border-primary disabled:cursor-default disabled:hover:border-surface-border"
                    >
                      <p className="text-xs font-semibold text-primary">{appointment.requestedTimeSlot}</p>
                      <p className="mt-1 text-sm font-semibold text-ink">{appointment.treatmentName}</p>
                      <p className="mt-0.5 truncate text-xs text-muted">{appointment.patientName}</p>
                      <span className={`mt-2 inline-flex rounded-full px-2 py-0.5 text-[11px] font-semibold ${statusStyles[appointment.status]}`}>{appointment.status}</span>
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {selected && (
        <div className="fixed inset-0 z-20 flex items-center justify-end bg-ink/25" onMouseDown={() => !deciding && setSelected(null)}>
          <aside className="h-full w-full max-w-md bg-surface-raised p-6 shadow-2xl" onMouseDown={(event) => event.stopPropagation()} aria-label="Appointment decision">
            <div className="flex items-start justify-between">
              <div><p className="text-xs font-semibold uppercase tracking-wide text-primary">Pending appointment</p><h2 className="mt-1 font-serif text-xl font-semibold">Review request</h2></div>
              <button aria-label="Close" className="text-xl text-muted" onClick={() => setSelected(null)}>×</button>
            </div>
            <dl className="mt-6 space-y-3 rounded-xl bg-surface p-4 text-sm">
              <div><dt className="text-xs text-muted">Patient</dt><dd className="font-semibold">{selected.patientName}</dd></div>
              <div><dt className="text-xs text-muted">Treatment</dt><dd className="font-semibold">{selected.treatmentName}</dd></div>
              <div><dt className="text-xs text-muted">Schedule</dt><dd className="font-semibold">{selected.requestedDate} · {selected.requestedTimeSlot}</dd></div>
            </dl>
            <label className="mt-5 block text-sm font-semibold">Decision note <span className="font-normal text-muted">(optional)</span>
              <textarea value={note} onChange={(event) => setNote(event.target.value)} rows={4} className="mt-2 w-full rounded-lg border border-surface-border bg-white p-3 font-normal outline-none focus:border-primary" placeholder="Add a note for the care team" />
            </label>
            <div className="mt-6 flex gap-3">
              <button disabled={deciding} onClick={() => decide("Approved")} className="flex-1 rounded-lg bg-primary px-4 py-2.5 font-semibold text-white hover:bg-primary-dark disabled:opacity-60">Approve</button>
              <button disabled={deciding} onClick={() => decide("Rejected")} className="flex-1 rounded-lg border border-danger px-4 py-2.5 font-semibold text-danger hover:bg-red-50 disabled:opacity-60">Reject</button>
            </div>
          </aside>
        </div>
      )}
    </section>
  );
}
