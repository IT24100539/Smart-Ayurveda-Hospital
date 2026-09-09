export function DashboardPage() {
  return (
    <section>
      <h1>Today in the hospital</h1>
      <p className="muted">Operational snapshot for consultations, panchakarma, and inpatient care.</p>
      <div className="grid-cards">
        <article className="stat"><span className="muted">Appointments</span><strong>—</strong></article>
        <article className="stat"><span className="muted">Checked in</span><strong>—</strong></article>
        <article className="stat"><span className="muted">Therapies</span><strong>—</strong></article>
        <article className="stat"><span className="muted">Open invoices</span><strong>—</strong></article>
      </div>
    </section>
  );
}
