import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../api/client";
import { createPatient, searchPatients, type DoshaType, type Gender, type Patient } from "../api/patients";
import { Badge, Button, Card, EmptyState, ErrorState, LoadingState, PageHeader, PatientHeaderCard } from "../components/ui";

const fieldClass =
  "field mt-1";

const genders: Gender[] = ["Female", "Male", "Other", "Unspecified"];
const doshas: DoshaType[] = ["Vata", "Pitta", "Kapha", "None"];

type Draft = {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  phone: string;
  email: string;
  address: string;
  bloodGroup: string;
  allergies: string;
  prakriti: DoshaType;
  vikriti: DoshaType;
};

const emptyDraft = (): Draft => ({
  firstName: "",
  lastName: "",
  dateOfBirth: "",
  gender: "Female",
  phone: "",
  email: "",
  address: "",
  bloodGroup: "",
  allergies: "",
  prakriti: "Vata",
  vikriti: "None"
});

function blankToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

export function PatientsPage() {
  const [query, setQuery] = useState("");
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  async function load(nextQuery = query) {
    setLoading(true);
    setLoadError(null);
    try {
      const page = await searchPatients(nextQuery);
      setPatients(page.items);
      setSelectedId((current) => (current && page.items.some((item) => item.id === current) ? current : page.items[0]?.id ?? null));
    } catch (error) {
      setLoadError(error instanceof ApiError ? error.message : "Unable to load patients.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load("");
  }, []);

  function update<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    setFormError(null);
    if (!draft.firstName.trim() || !draft.lastName.trim() || !draft.phone.trim() || !draft.dateOfBirth) {
      setFormError("First name, last name, date of birth, and phone are required.");
      return;
    }

    setSaving(true);
    try {
      const created = await createPatient({
        firstName: draft.firstName.trim(),
        lastName: draft.lastName.trim(),
        dateOfBirth: draft.dateOfBirth,
        gender: draft.gender,
        phone: draft.phone.trim(),
        email: blankToNull(draft.email),
        address: blankToNull(draft.address),
        bloodGroup: blankToNull(draft.bloodGroup),
        allergies: blankToNull(draft.allergies),
        prakriti: draft.prakriti,
        vikriti: draft.vikriti
      });
      setPatients((current) => [created, ...current.filter((item) => item.id !== created.id)]);
      setSelectedId(created.id);
      setDraft(emptyDraft());
      setFormOpen(false);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : "Unable to create the patient.");
    } finally {
      setSaving(false);
    }
  }

  const selected = patients.find((item) => item.id === selectedId) ?? null;

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Records"
        title="Patients"
        description="Register a patient record and look up an existing UHID, prakriti, and vikriti."
        action={
          <Button className="w-full !bg-hero-button !text-hero-deep hover:!bg-hero-button-hover sm:w-auto" onClick={() => setFormOpen((open) => !open)}>
            {formOpen ? "Close form" : "New patient"}
          </Button>
        }
      />

      {formOpen ? (
        <Card className="mb-6 p-4 sm:p-6">
          <form onSubmit={onCreate} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <label className="block text-sm font-semibold text-ink">
              First name
              <input className={fieldClass} value={draft.firstName} onChange={(event) => update("firstName", event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Last name
              <input className={fieldClass} value={draft.lastName} onChange={(event) => update("lastName", event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Date of birth
              <input className={fieldClass} type="date" value={draft.dateOfBirth} onChange={(event) => update("dateOfBirth", event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Gender
              <select className={fieldClass} value={draft.gender} onChange={(event) => update("gender", event.target.value as Gender)}>
                {genders.map((gender) => (
                  <option key={gender} value={gender}>
                    {gender}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink">
              Phone
              <input className={fieldClass} value={draft.phone} onChange={(event) => update("phone", event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Email
              <input className={fieldClass} type="email" value={draft.email} onChange={(event) => update("email", event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Prakriti
              <select className={fieldClass} value={draft.prakriti} onChange={(event) => update("prakriti", event.target.value as DoshaType)}>
                {doshas.map((dosha) => (
                  <option key={dosha} value={dosha}>
                    {dosha}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink">
              Vikriti
              <select className={fieldClass} value={draft.vikriti} onChange={(event) => update("vikriti", event.target.value as DoshaType)}>
                {doshas.map((dosha) => (
                  <option key={dosha} value={dosha}>
                    {dosha}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Address
              <input className={fieldClass} value={draft.address} onChange={(event) => update("address", event.target.value)} />
            </label>
            {formError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2" role="alert">
                {formError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : "Save patient"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      {selected ? (
        <div className="mb-6">
          <PatientHeaderCard patient={selected} />
        </div>
      ) : null}

      <Card className="overflow-hidden">
        <form
          className="border-b border-surface-border bg-neutral-50 p-4"
          onSubmit={(event) => {
            event.preventDefault();
            void load(query);
          }}
        >
          <label className="block text-sm font-semibold text-ink" htmlFor="patient-search">
            Search
            <input
              id="patient-search"
              className={fieldClass}
              placeholder="Name, phone, or UHID"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </label>
        </form>

        {loading ? (
          <div className="p-4">
            <LoadingState label="Loading patients…" />
          </div>
        ) : loadError ? (
          <div className="p-4">
            <ErrorState message={loadError} onRetry={() => void load(query)} />
          </div>
        ) : patients.length === 0 ? (
          <div className="p-4">
            <EmptyState title="No patients found." description="Register a patient to open their hospital record." />
          </div>
        ) : (
          <div className="table-scroll">
            <table className="data-table min-w-[40rem]">
              <thead>
                <tr>
                  <th>Patient</th>
                  <th>UHID</th>
                  <th>Phone</th>
                  <th>Prakriti</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {patients.map((patient) => (
                  <tr
                    key={patient.id}
                    className={patient.id === selectedId ? "bg-primary-muted/50" : undefined}
                    onClick={() => setSelectedId(patient.id)}
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gold-muted text-xs font-bold text-gold" aria-hidden="true">
                          {(patient.firstName[0] ?? "") + (patient.lastName[0] ?? "")}
                        </span>
                        <button type="button" className="text-left font-semibold text-ink">
                          {patient.firstName} {patient.lastName}
                        </button>
                      </div>
                    </td>
                    <td className="px-4 py-3 font-mono text-xs text-muted">{patient.uhid}</td>
                    <td className="px-4 py-3 text-muted">{patient.phone}</td>
                    <td className="px-4 py-3">
                      <Badge tone="neutral">{patient.prakriti}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={patient.isActive ? "approved" : "rejected"}>{patient.isActive ? "Active" : "Inactive"}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
