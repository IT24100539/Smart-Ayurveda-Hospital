import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { getFeedbackStats, type FeedbackStats } from "../api/feedback";
import { listWorkflows, type WorkflowExecution } from "../api/workflows";
import { useAuthStore } from "../store/authStore";
import { Badge, Card, ErrorState, LoadingState, type BadgeTone } from "../components/ui";

type AppointmentStatus = "Pending" | "Approved" | "Rejected" | "Cancelled" | "Completed";

type Appointment = {
  id: string;
  patientName: string;
  treatmentName: string;
  requestedTimeSlot: string;
  status: AppointmentStatus;
};

type Page<T> = { items: T[]; totalCount: number };

type Ward = { id: string; name: string; totalCapacity: number; occupiedBeds: number };

type Admission = { id: string; patientName: string; reason: string; preferredDate: string };

type Complaint = { id: string; subject: string; patientName: string; status: string; priority: string };

type StaffComment = {
  id: string;
  patientName: string;
  rating: number;
  comment: string;
  isAnonymous: boolean;
  sentiment: string | null;
  status: string;
};

type Snapshot = {
  patientCount: number;
  appointments: Appointment[];
  wards: Ward[];
  admissions: Admission[];
  complaints: Complaint[];
  comments: StaffComment[];
  workflows: WorkflowExecution[];
  feedback: FeedbackStats | null;
};

const emptySnapshot = (): Snapshot => ({
  patientCount: 0,
  appointments: [],
  wards: [],
  admissions: [],
  complaints: [],
  comments: [],
  workflows: [],
  feedback: null
});

function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

function greeting(date: Date): string {
  const hour = date.getHours();
  if (hour < 12) return "Good morning";
  if (hour < 17) return "Good afternoon";
  return "Good evening";
}

function roleLabel(role: string | undefined): string {
  if (role === "Doctor") return "Vaidya";
  if (role === "FrontDeskStaff") return "Front desk";
  if (role === "Admin") return "Administrator";
  return role ?? "Staff";
}

function appointmentTone(status: AppointmentStatus): BadgeTone {
  if (status === "Approved" || status === "Completed") return "approved";
  if (status === "Rejected" || status === "Cancelled") return "rejected";
  return "pending";
}

function displayName(fullName: string | undefined): string {
  const name = fullName?.trim();
  return name || "there";
}

export function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const [snapshot, setSnapshot] = useState<Snapshot>(emptySnapshot);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    const date = todayIso();

    Promise.allSettled([
      api.request<Page<{ id: string }>>("/patients?page=1&pageSize=1"),
      api.request<Page<Appointment>>(`/appointments?date=${date}&pageSize=100`),
      api.request<Ward[]>("/wards"),
      api.request<Admission[]>("/admissions"),
      api.request<Complaint[]>("/complaints"),
      api.request<Page<StaffComment>>("/feedback/staff?page=1&pageSize=5"),
      getFeedbackStats(),
      listWorkflows({ agentName: "", approvalStatus: "Pending" })
    ]).then((results) => {
      if (!active) return;
      const [patients, appointments, wards, admissions, complaints, comments, feedback, workflows] = results;
      const failed = results.filter((result) => result.status === "rejected").length;
      setSnapshot({
        patientCount: patients.status === "fulfilled" ? patients.value.totalCount : 0,
        appointments: appointments.status === "fulfilled" ? appointments.value.items : [],
        wards: wards.status === "fulfilled" ? wards.value : [],
        admissions: admissions.status === "fulfilled" ? admissions.value : [],
        complaints: complaints.status === "fulfilled" ? complaints.value : [],
        comments: comments.status === "fulfilled" ? comments.value.items : [],
        feedback: feedback.status === "fulfilled" ? feedback.value : null,
        workflows: workflows.status === "fulfilled" ? workflows.value.items : []
      });
      if (failed === results.length) {
        const reason = results.find((result) => result.status === "rejected");
        const message =
          reason?.status === "rejected" && reason.reason instanceof ApiError
            ? reason.reason.message
            : "Unable to load the hospital overview.";
        setError(message);
      } else if (failed > 0) {
        setError("Some figures could not be loaded. The rest of the overview is current.");
      }
      setLoading(false);
    });

    return () => {
      active = false;
    };
  }, [reloadKey]);

  const now = new Date();
  const pendingAppointments = snapshot.appointments.filter((item) => item.status === "Pending");
  const openComplaints = snapshot.complaints.filter((item) => item.status !== "Resolved");
  const occupied = snapshot.wards.reduce((sum, ward) => sum + ward.occupiedBeds, 0);
  const capacity = snapshot.wards.reduce((sum, ward) => sum + ward.totalCapacity, 0);
  const occupancy = capacity === 0 ? 0 : Math.round((occupied / capacity) * 100);

  const metrics = [
    { label: "Patients", value: String(snapshot.patientCount), detail: "Active records", to: "/patients" },
    { label: "Today", value: String(snapshot.appointments.length), detail: "Therapy visits", to: "/appointments" },
    { label: "Awaiting decision", value: String(pendingAppointments.length), detail: "Appointment requests", to: "/appointments" },
    { label: "Beds filled", value: capacity === 0 ? "—" : `${occupied}/${capacity}`, detail: `${occupancy}% occupancy`, to: "/wards" },
    { label: "Feedback", value: snapshot.feedback ? snapshot.feedback.averageRating.toFixed(1) : "—", detail: snapshot.feedback ? `${snapshot.feedback.pendingModeration} to moderate` : "Rating unavailable", to: "/feedback" },
    { label: "AI approvals", value: String(snapshot.workflows.length), detail: "Plans waiting", to: "/ai-approvals" }
  ];

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <section className="relative overflow-hidden rounded-2xl bg-primary-dark text-white shadow-lg">
        <div className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-white/10" aria-hidden="true" />
        <div className="pointer-events-none absolute bottom-0 left-0 h-1 w-full bg-gradient-to-r from-amber-300/80 via-amber-200/30 to-transparent" aria-hidden="true" />
        <img
          src="/images/ayurveda-courtyard.png"
          alt="Courtyard of the Ayurveda hospital"
          className="h-44 w-full object-cover sm:h-52"
        />
        <div className="relative grid gap-6 p-6 md:grid-cols-[1.5fr_0.8fr] md:p-8">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-primary-muted">Hospital operations</p>
            <h1 className="mt-2 text-3xl text-white">
              {greeting(now)}, {displayName(user?.fullName)}
            </h1>
            <p className="mt-3 max-w-xl text-sm leading-6 text-primary-muted">
              Today’s panchakarma schedule, ward beds, and the decisions still waiting on staff.
            </p>
          </div>
          <div className="rounded-xl border border-white/10 bg-white/10 p-5">
            <p className="text-sm text-primary-muted">
              {now.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" })}
            </p>
            <p className="mt-2 font-display text-2xl">{roleLabel(user?.role)}</p>
            <p className="mt-1 text-sm text-primary-muted">{openComplaints.length} open complaints</p>
          </div>
        </div>
      </section>

      {loading ? <LoadingState label="Loading today’s hospital overview…" /> : null}
      {!loading && error && snapshot.appointments.length === 0 && snapshot.patientCount === 0 && snapshot.wards.length === 0 ? (
        <ErrorState message={error} onRetry={() => setReloadKey((value) => value + 1)} />
      ) : null}
      {!loading && error && (snapshot.patientCount > 0 || snapshot.wards.length > 0 || snapshot.appointments.length > 0) ? (
        <p className="rounded-xl border border-status-pending-bg bg-status-pending-bg px-4 py-3 text-sm text-status-pending-fg" role="status">
          {error}
        </p>
      ) : null}

      {!loading ? (
        <>
          <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-label="Hospital figures">
            {metrics.map((metric) => (
              <Link key={metric.label} to={metric.to} className="block rounded-xl focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary">
                <Card className="relative h-full overflow-hidden p-5 pt-6 transition hover:-translate-y-0.5 hover:shadow-lg">
                  <span className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-amber-300 to-primary" aria-hidden="true" />
                  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-muted">{metric.label}</p>
                  <p className="mt-2 font-display text-3xl text-primary-dark">{metric.value}</p>
                  <p className="mt-1 text-sm text-muted">{metric.detail}</p>
                </Card>
              </Link>
            ))}
          </section>

          <div className="grid gap-6 xl:grid-cols-5">
            <Card className="p-5 xl:col-span-3">
              <div className="flex items-end justify-between gap-3">
                <div>
                  <h2 className="font-display text-xl text-primary-dark">Today’s schedule</h2>
                  <p className="mt-1 text-sm text-muted">Consultations and therapies booked for this date.</p>
                </div>
                <Link to="/appointments" className="text-sm font-semibold text-primary hover:text-primary-dark">
                  Open calendar
                </Link>
              </div>
              {snapshot.appointments.length === 0 ? (
                <p className="mt-6 rounded-lg bg-surface px-4 py-8 text-center text-sm text-muted">No appointments are booked for today.</p>
              ) : (
                <ul className="mt-4 divide-y divide-surface-border">
                  {snapshot.appointments.slice(0, 8).map((item) => (
                    <li key={item.id} className="flex items-center justify-between gap-3 py-3">
                      <div>
                        <p className="font-semibold text-ink">{item.patientName}</p>
                        <p className="text-sm text-muted">{item.treatmentName}</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <span className="text-sm font-medium text-ink">{item.requestedTimeSlot}</span>
                        <Badge tone={appointmentTone(item.status)}>{item.status}</Badge>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </Card>

            <Card className="p-5 xl:col-span-2">
              <h2 className="font-display text-xl text-primary-dark">Needs a decision</h2>
              <p className="mt-1 text-sm text-muted">Requests that stay pending until staff act.</p>
              <ul className="mt-4 space-y-3">
                <AttentionRow to="/appointments" label="Appointment requests" count={pendingAppointments.length} />
                <AttentionRow to="/wards" label="Admission requests" count={snapshot.admissions.length} />
                <AttentionRow to="/feedback" label="Open complaints" count={openComplaints.length} />
                <AttentionRow to="/feedback" label="Feedback to moderate" count={snapshot.feedback?.pendingModeration ?? 0} />
                <AttentionRow to="/ai-approvals" label="Agent plans" count={snapshot.workflows.length} />
              </ul>
              {snapshot.admissions[0] ? (
                <p className="mt-4 rounded-lg bg-surface px-3 py-3 text-sm text-ink">
                  Next admission: <span className="font-semibold">{snapshot.admissions[0].patientName}</span>
                  <span className="block text-muted">{snapshot.admissions[0].reason}</span>
                </p>
              ) : null}
            </Card>
          </div>

          <Card className="p-5">
            <div className="flex items-end justify-between gap-3">
              <div>
                <h2 className="font-display text-xl text-primary-dark">Patient feedback</h2>
                <p className="mt-1 text-sm text-muted">
                  Latest comments on care
                  {snapshot.feedback ? `, averaging ${snapshot.feedback.averageRating.toFixed(1)} out of 5` : ""}.
                </p>
              </div>
              <Link to="/feedback" className="text-sm font-semibold text-primary hover:text-primary-dark">
                Review all
              </Link>
            </div>
            {snapshot.comments.length === 0 ? (
              <p className="mt-6 rounded-lg bg-surface px-4 py-8 text-center text-sm text-muted">No patient comments yet.</p>
            ) : (
              <ul className="mt-4 space-y-3">
                {snapshot.comments.map((item) => (
                  <li key={item.id} className="rounded-2xl border border-white/80 bg-white/70 px-4 py-4 shadow-sm">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <p className="font-semibold text-ink">{item.isAnonymous ? "Anonymous patient" : item.patientName}</p>
                      <div className="flex items-center gap-2">
                        <span className="tracking-wide text-amber-600" aria-label={`${item.rating} out of 5`}>
                          {"★".repeat(item.rating)}
                          <span className="text-surface-border">{"★".repeat(Math.max(0, 5 - item.rating))}</span>
                        </span>
                        <Badge tone={item.sentiment === "Negative" ? "rejected" : item.status === "PendingModeration" ? "pending" : "approved"}>
                          {item.sentiment ?? item.status}
                        </Badge>
                      </div>
                    </div>
                    <blockquote className="mt-3 border-l-4 border-amber-300 pl-3 font-display text-base leading-7 text-ink">
                      {item.comment}
                    </blockquote>
                  </li>
                ))}
              </ul>
            )}
          </Card>

          <Card className="p-5">
            <div className="flex items-end justify-between gap-3">
              <div>
                <h2 className="font-display text-xl text-primary-dark">Ward occupancy</h2>
                <p className="mt-1 text-sm text-muted">Filled beds across the hospital.</p>
              </div>
              <Link to="/wards" className="text-sm font-semibold text-primary hover:text-primary-dark">
                Manage beds
              </Link>
            </div>
            {snapshot.wards.length === 0 ? (
              <p className="mt-6 text-sm text-muted">No wards are configured.</p>
            ) : (
              <ul className="mt-5 grid gap-4 md:grid-cols-2">
                {snapshot.wards.map((ward) => {
                  const percent = ward.totalCapacity === 0 ? 0 : Math.round((ward.occupiedBeds / ward.totalCapacity) * 100);
                  return (
                    <li key={ward.id}>
                      <div className="mb-2 flex items-baseline justify-between gap-3">
                        <p className="font-semibold text-ink">{ward.name}</p>
                        <p className="text-sm text-muted">
                          {ward.occupiedBeds} / {ward.totalCapacity}
                        </p>
                      </div>
                      <div className="h-2 overflow-hidden rounded-full bg-primary-muted" aria-hidden="true">
                        <div className="h-full rounded-full bg-primary" style={{ width: `${percent}%` }} />
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </Card>
        </>
      ) : null}
    </div>
  );
}

function AttentionRow({ to, label, count }: { to: string; label: string; count: number }) {
  return (
    <li>
      <Link to={to} className="flex items-center justify-between rounded-lg bg-surface px-3 py-3 text-sm hover:bg-primary-muted">
        <span className="font-medium text-ink">{label}</span>
        <span className="font-display text-lg text-primary-dark">{count}</span>
      </Link>
    </li>
  );
}
