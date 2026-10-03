import type { Patient } from "../../api/patients";
import { Badge } from "./Badge";

type PatientHeaderCardProps = {
  patient: Pick<
    Patient,
    "firstName" | "lastName" | "uhid" | "dateOfBirth" | "gender" | "phone" | "email" | "bloodGroup" | "allergies" | "prakriti" | "vikriti" | "isActive"
  >;
};

function ageFrom(dateOfBirth: string): number | null {
  const born = new Date(dateOfBirth);
  if (Number.isNaN(born.getTime())) return null;
  const now = new Date();
  let age = now.getFullYear() - born.getFullYear();
  if (now.getMonth() < born.getMonth() || (now.getMonth() === born.getMonth() && now.getDate() < born.getDate())) {
    age -= 1;
  }
  return age >= 0 ? age : null;
}

/**
 * Header card for a patient record: a dark herbal banner with the name, UHID and
 * dosha pills, above a small facts grid. Presentational only.
 */
export function PatientHeaderCard({ patient }: PatientHeaderCardProps) {
  const age = ageFrom(patient.dateOfBirth);
  const facts: { label: string; value: string }[] = [
    { label: "Age and gender", value: [age === null ? null : `${age} yrs`, patient.gender].filter(Boolean).join(" · ") },
    { label: "Phone", value: patient.phone },
    { label: "Email", value: patient.email ?? "Not recorded" },
    { label: "Blood group", value: patient.bloodGroup ?? "Not recorded" },
    { label: "Allergies", value: patient.allergies ?? "None recorded" }
  ];

  return (
    <section className="overflow-hidden rounded-header border border-surface-border bg-surface-raised shadow-card" aria-label="Patient summary">
      <div className="header-card on-dark relative p-5 sm:p-6">
        <div className="relative flex flex-wrap items-center gap-4">
          <span
            className="avatar-mark flex shrink-0 items-center justify-center border border-on-header/50 bg-on-header/15 font-display text-2xl font-semibold text-on-header"
            aria-hidden="true"
          >
            {(patient.firstName[0] ?? "") + (patient.lastName[0] ?? "")}
          </span>
          <div className="min-w-0 flex-1">
            <p className="font-display text-page text-on-header">
              {patient.firstName} {patient.lastName}
            </p>
            <p className="mt-2 inline-flex rounded-full bg-pill-bg px-3 py-1 text-label font-semibold uppercase text-pill-fg">
              UHID {patient.uhid}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge tone={patient.isActive ? "approved" : "rejected"}>{patient.isActive ? "Active" : "Inactive"}</Badge>
            <Badge tone="neutral">{`Prakriti: ${patient.prakriti}`}</Badge>
            <Badge tone="neutral">{`Vikriti: ${patient.vikriti}`}</Badge>
          </div>
        </div>
      </div>
      <dl className="grid gap-4 p-5 sm:grid-cols-2 lg:grid-cols-5 sm:p-6">
        {facts.map((fact) => (
          <div key={fact.label}>
            <dt className="text-xs font-semibold uppercase tracking-wide text-muted">{fact.label}</dt>
            <dd className="mt-1 text-sm font-medium text-ink">{fact.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
