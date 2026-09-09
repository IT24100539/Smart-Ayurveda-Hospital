import { useEffect, useState } from "react";
import { api, type Appointment } from "../api/client";

const STATUS: Record<number, string> = {
  1: "Scheduled",
  2: "Checked in",
  3: "In progress",
  4: "Completed",
  5: "Cancelled",
  6: "No show"
};

export function AppointmentsPage() {
  const [items, setItems] = useState<Appointment[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.appointments()
      .then((page) => setItems(page.items))
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load appointments"));
  }, []);

  return (
    <section>
      <h1>Appointments</h1>
      {error ? <p className="error">{error}</p> : null}
      <table className="table">
        <thead>
          <tr><th>When</th><th>Patient</th><th>Doctor</th><th>Reason</th><th>Status</th></tr>
        </thead>
        <tbody>
          {items.map((a) => (
            <tr key={a.id}>
              <td>{new Date(a.scheduledAt).toLocaleString()}</td>
              <td>{a.patientName} ({a.patientUhid})</td>
              <td>{a.doctorName}</td>
              <td>{a.reason}</td>
              <td>{typeof a.status === "string" ? a.status : STATUS[a.status] ?? a.status}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
