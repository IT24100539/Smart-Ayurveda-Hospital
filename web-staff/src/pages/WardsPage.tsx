import { useEffect, useState } from "react";
import { ApiError, api } from "../api/client";
import { useAuthStore } from "../store/authStore";

export type Bed = { id: string; bedLabel: string; isOccupied: boolean };
type Ward = { id: string; name: string; totalCapacity: number; occupiedBeds: number; beds: Bed[] };
type Admission = {
  id: string;
  patientName: string;
  wardId: string | null;
  reason: string;
  preferredDate: string;
  requestedByAgent: boolean;
};

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : "Something went wrong. Please try again.";
}

export function BedGrid({ beds }: { beds: Bed[] }) {
  return (
    <div className="grid grid-cols-4 gap-2 sm:grid-cols-6" aria-label="Bed availability">
      {beds.map((bed) => (
        <div
          key={bed.id}
          data-testid={`bed-${bed.isOccupied ? "occupied" : "free"}`}
          title={`${bed.bedLabel}: ${bed.isOccupied ? "Occupied" : "Free"}`}
          className={`flex aspect-square items-center justify-center rounded-lg border text-xs font-bold ${bed.isOccupied ? "border-primary bg-primary text-white" : "border-primary bg-transparent text-primary"}`}
        >
          {bed.bedLabel}
        </div>
      ))}
    </div>
  );
}

export function WardsPage() {
  const staffId = useAuthStore((state) => state.user?.id);
  const [wards, setWards] = useState<Ward[]>([]);
  const [admissions, setAdmissions] = useState<Admission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [decidingId, setDecidingId] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([api.request<Ward[]>("/wards"), api.request<Admission[]>("/admissions")])
      .then(([wardData, admissionData]) => {
        if (active) { setWards(wardData); setAdmissions(admissionData); }
      })
      .catch((caught) => { if (active) setError(errorMessage(caught)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refreshKey]);

  async function decide(admission: Admission, approve: boolean) {
    if (!staffId) return;
    const action = approve ? "approve" : "reject";
    if (!window.confirm(`Are you sure you want to ${action} this admission request?`)) return;
    setDecidingId(admission.id);
    setError(null);
    try {
      await api.request(`/admissions/${admission.id}/decision`, {
        method: "PATCH",
        body: JSON.stringify({ approve, decidedBy: staffId })
      });
      setRefreshKey((value) => value + 1);
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setDecidingId(null);
    }
  }

  return (
    <section className="space-y-6">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary">In-patient care</p>
        <h1>Ward & bed dashboard</h1>
        <p className="mt-1 text-sm text-muted">Live occupancy and admission requests requiring human approval.</p>
      </div>

      {error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-danger">{error}</div>}
      {loading ? (
        <div className="rounded-xl border border-surface-border bg-surface-raised p-8 text-center text-muted">Loading ward availability…</div>
      ) : (
        <>
          {wards.length === 0 ? (
            <div className="rounded-xl border border-surface-border bg-surface-raised p-8 text-center text-muted">No wards are available.</div>
          ) : (
            <div className="grid gap-5 xl:grid-cols-2">
              {wards.map((ward) => (
                <article key={ward.id} className="rounded-xl border border-surface-border bg-surface-raised p-5 shadow-sm">
                  <header className="mb-4 flex items-start justify-between gap-3">
                    <div><h2 className="font-serif text-lg font-semibold text-primary-dark">{ward.name}</h2><p className="mt-1 text-xs text-muted">Filled beds indicate occupied spaces</p></div>
                    <span className="rounded-full bg-primary-muted px-3 py-1 text-sm font-bold text-primary-dark">{ward.occupiedBeds} / {ward.totalCapacity}</span>
                  </header>
                  <BedGrid beds={ward.beds} />
                  <div className="mt-4 flex gap-5 text-xs text-muted"><span className="flex items-center gap-1.5"><i className="h-3 w-3 rounded-sm bg-primary" /> Occupied</span><span className="flex items-center gap-1.5"><i className="h-3 w-3 rounded-sm border border-primary" /> Free</span></div>
                </article>
              ))}
            </div>
          )}

          <section className="overflow-hidden rounded-xl border border-surface-border bg-surface-raised shadow-sm">
            <div className="border-b border-surface-border px-5 py-4"><h2 className="font-serif text-lg font-semibold text-primary-dark">Pending admission / bed requests</h2><p className="mt-1 text-sm text-muted">Every request requires a staff decision before a bed is assigned.</p></div>
            {admissions.length === 0 ? (
              <p className="p-8 text-center text-sm text-muted">No pending admission requests.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-surface text-xs uppercase tracking-wide text-muted"><tr><th className="px-5 py-3">Patient</th><th className="px-5 py-3">Preferred date</th><th className="px-5 py-3">Reason</th><th className="px-5 py-3">Source</th><th className="px-5 py-3 text-right">Decision</th></tr></thead>
                  <tbody className="divide-y divide-surface-border">
                    {admissions.map((admission) => (
                      <tr key={admission.id} className={admission.requestedByAgent ? "bg-primary-muted/35" : ""}>
                        <td className="px-5 py-4 font-semibold text-ink">{admission.patientName}</td>
                        <td className="whitespace-nowrap px-5 py-4 text-muted">{admission.preferredDate}</td>
                        <td className="max-w-xs px-5 py-4 text-muted">{admission.reason}</td>
                        <td className="px-5 py-4">{admission.requestedByAgent ? <span className="inline-flex items-center gap-1.5 rounded-full bg-primary px-2.5 py-1 text-xs font-semibold text-white"><span aria-hidden="true">✦</span> AI · Scheduling & Bed Agent</span> : <span className="text-xs text-muted">Staff/Patient direct</span>}</td>
                        <td className="px-5 py-4"><div className="flex justify-end gap-2"><button disabled={decidingId === admission.id} onClick={() => decide(admission, true)} className="rounded-lg bg-primary px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-dark disabled:opacity-50">Approve</button><button disabled={decidingId === admission.id} onClick={() => decide(admission, false)} className="rounded-lg border border-danger px-3 py-1.5 text-xs font-semibold text-danger hover:bg-red-50 disabled:opacity-50">Reject</button></div></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </section>
  );
}
