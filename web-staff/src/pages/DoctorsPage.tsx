import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../api/client";
import {
  createDoctor,
  deactivateDoctor,
  listDoctors,
  removeDoctorPhoto,
  updateDoctor,
  uploadDoctorPhoto,
  type Doctor,
  type DoctorInput
} from "../api/doctors";
import { Badge, Button, Card, DataTable, PageHeader, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;
const NAME_MAX = 160;
const SPECIALTY_MAX = 160;
const QUALIFICATIONS_MAX = 400;
const BIO_MAX = 2000;
const PHOTO_MAX_BYTES = 512 * 1024;

const fieldClass =
  "field mt-1";

type Draft = {
  name: string;
  specialty: string;
  qualifications: string;
  bio: string;
};

type FieldKey = keyof Draft | "photo";
type FieldErrors = Partial<Record<FieldKey, string>>;

const emptyDraft = (): Draft => ({
  name: "",
  specialty: "",
  qualifications: "",
  bio: ""
});

function photoProblem(file: File): string | null {
  const type = file.type === "image/jpg" || file.type === "image/pjpeg" ? "image/jpeg" : file.type;
  const extensionOk = /\.(jpe?g|png|webp)$/i.test(file.name);
  const typeOk = type === "image/jpeg" || type === "image/png" || type === "image/webp" || (type === "" && extensionOk);
  if (!typeOk) {
    return "Photo must be a JPEG, PNG, or WebP image.";
  }
  if (file.size === 0) {
    return "Photo file is empty.";
  }
  if (file.size > PHOTO_MAX_BYTES) {
    return "Photo must be no larger than 512 KB.";
  }
  return null;
}

function validateDraft(draft: Draft, photo: File | null): FieldErrors {
  const errors: FieldErrors = {};
  const name = draft.name.trim();
  const specialty = draft.specialty.trim();
  const qualifications = draft.qualifications.trim();
  const bio = draft.bio.trim();

  if (!name) {
    errors.name = "Name is required.";
  } else if (name.length > NAME_MAX) {
    errors.name = `Name must be at most ${NAME_MAX} characters.`;
  }

  if (!specialty) {
    errors.specialty = "Specialty is required.";
  } else if (specialty.length > SPECIALTY_MAX) {
    errors.specialty = `Specialty must be at most ${SPECIALTY_MAX} characters.`;
  }

  if (!qualifications) {
    errors.qualifications = "Qualifications are required.";
  } else if (qualifications.length > QUALIFICATIONS_MAX) {
    errors.qualifications = `Qualifications must be at most ${QUALIFICATIONS_MAX} characters.`;
  }

  if (bio.length > BIO_MAX) {
    errors.bio = `Bio must be at most ${BIO_MAX} characters.`;
  }

  if (photo) {
    const problem = photoProblem(photo);
    if (problem) {
      errors.photo = problem;
    }
  }

  return errors;
}

function toInput(draft: Draft): DoctorInput {
  const bio = draft.bio.trim();
  return {
    name: draft.name.trim(),
    specialty: draft.specialty.trim(),
    qualifications: draft.qualifications.trim(),
    bio: bio ? bio : null
  };
}

function messageFrom(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

function serverField(error: unknown, name: string): string | undefined {
  if (!(error instanceof ApiError)) {
    return undefined;
  }
  const match = Object.entries(error.fields).find(([key]) => key.toLowerCase() === name.toLowerCase());
  return match?.[1][0];
}

function ratingLabel(doctor: Doctor): string {
  if (doctor.rating == null || doctor.ratingCount == null) {
    return "—";
  }
  return `${doctor.rating.toFixed(1)} (${doctor.ratingCount})`;
}

export function DoctorsPage() {
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [appliedQuery, setAppliedQuery] = useState("");
  const [appliedActiveOnly, setAppliedActiveOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Doctor | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listDoctors({
      query: appliedQuery,
      activeOnly: appliedActiveOnly,
      page,
      pageSize: PAGE_SIZE
    })
      .then((result) => {
        if (cancelled) return;
        setDoctors(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setDoctors([]);
        setTotalCount(0);
        setLoadError(messageFrom(error, "Unable to load doctors."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [appliedQuery, appliedActiveOnly, page, reloadKey]);

  function upsert(doctor: Doctor) {
    const exists = doctors.some((item) => item.id === doctor.id);
    setDoctors((current) =>
      current.some((item) => item.id === doctor.id)
        ? current.map((item) => (item.id === doctor.id ? doctor : item))
        : [doctor, ...current]
    );
    if (!exists) {
      setTotalCount((count) => count + 1);
    }
  }

  function closeForm() {
    setFormOpen(false);
    setEditing(null);
    setDraft(emptyDraft());
    setPhotoFile(null);
    setFieldErrors({});
    setFormError(null);
  }

  function openCreate() {
    setEditing(null);
    setDraft(emptyDraft());
    setPhotoFile(null);
    setFieldErrors({});
    setFormError(null);
    setFormOpen(true);
  }

  function openEdit(doctor: Doctor) {
    setEditing(doctor);
    setDraft({
      name: doctor.name,
      specialty: doctor.specialty,
      qualifications: doctor.qualifications,
      bio: doctor.bio ?? ""
    });
    setPhotoFile(null);
    setFieldErrors({});
    setFormError(null);
    setFormOpen(true);
  }

  function updateDraft<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
    setFieldErrors((current) => ({ ...current, [key]: undefined }));
  }

  async function onSave(event: FormEvent) {
    event.preventDefault();
    const errors = validateDraft(draft, photoFile);
    setFieldErrors(errors);
    setFormError(null);
    if (Object.values(errors).some(Boolean)) {
      return;
    }

    setSaving(true);
    try {
      const saved = editing ? await updateDoctor(editing.id, toInput(draft)) : await createDoctor(toInput(draft));
      let finalDoctor = saved;
      if (photoFile) {
        try {
          finalDoctor = await uploadDoctorPhoto(saved.id, photoFile);
        } catch (error: unknown) {
          upsert(saved);
          setEditing(saved);
          setPhotoFile(null);
          setFormError(messageFrom(error, "The doctor was saved, but the portrait could not be uploaded."));
          return;
        }
      }
      upsert(finalDoctor);
      closeForm();
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to save the doctor."));
      setFieldErrors({
        name: serverField(error, "name"),
        specialty: serverField(error, "specialty"),
        qualifications: serverField(error, "qualifications"),
        bio: serverField(error, "bio"),
        photo: serverField(error, "photo")
      });
    } finally {
      setSaving(false);
    }
  }

  async function onDeactivate(doctor: Doctor) {
    setBusyId(doctor.id);
    setLoadError(null);
    try {
      const updated = await deactivateDoctor(doctor.id);
      upsert(updated);
      setConfirmId(null);
    } catch (error: unknown) {
      setLoadError(messageFrom(error, "Unable to deactivate the doctor."));
    } finally {
      setBusyId(null);
    }
  }

  async function onRemovePhoto() {
    if (!editing) return;
    setSaving(true);
    setFormError(null);
    try {
      const updated = await removeDoctorPhoto(editing.id);
      upsert(updated);
      setEditing(updated);
      setPhotoFile(null);
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to remove the portrait."));
    } finally {
      setSaving(false);
    }
  }

  const columns: DataTableColumn<Doctor>[] = [
    {
      id: "name",
      header: "Doctor",
      render: (doctor) => (
        <div>
          <p className="font-semibold">{doctor.name}</p>
          {doctor.isSample ? <Badge tone="pending">Sample</Badge> : null}
        </div>
      )
    },
    { id: "specialty", header: "Specialty", render: (doctor) => doctor.specialty },
    { id: "qualifications", header: "Qualifications", render: (doctor) => doctor.qualifications },
    {
      id: "status",
      header: "Status",
      render: (doctor) => <Badge tone={doctor.isActive ? "success" : "rejected"}>{doctor.isActive ? "Active" : "Inactive"}</Badge>
    },
    { id: "rating", header: "Rating", render: (doctor) => ratingLabel(doctor) },
    {
      id: "actions",
      header: "Actions",
      render: (doctor) => (
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="secondary" onClick={() => openEdit(doctor)}>
            {`Edit ${doctor.name}`}
          </Button>
          {doctor.isActive ? (
            confirmId === doctor.id ? (
              <>
                <Button variant="danger" disabled={busyId === doctor.id} onClick={() => void onDeactivate(doctor)}>
                  {busyId === doctor.id ? "Deactivating…" : `Confirm deactivation of ${doctor.name}`}
                </Button>
                <Button variant="secondary" onClick={() => setConfirmId(null)}>
                  Keep active
                </Button>
              </>
            ) : (
              <Button variant="secondary" onClick={() => setConfirmId(doctor.id)}>
                {`Deactivate ${doctor.name}`}
              </Button>
            )
          ) : null}
        </div>
      )
    }
  ];

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Directory"
        title="Doctors"
        description="Maintain vaidya profiles, specialties, and portraits. These records are not staff sign-in accounts."
        action={
          <Button
            className="w-full !bg-hero-button !text-hero-deep hover:!bg-hero-button-hover sm:w-auto"
            onClick={() => (formOpen ? closeForm() : openCreate())}
          >
            {formOpen ? "Close form" : "Add doctor"}
          </Button>
        }
      />

      {formOpen ? (
        <Card className="mb-6 p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">
            {editing ? `Edit ${editing.name}` : "New doctor"}
          </h2>
          {editing?.isSample ? (
            <p className="mt-1 text-sm text-muted">This is a sample profile used for development.</p>
          ) : null}
          <form onSubmit={onSave} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Name
              <input className={fieldClass} value={draft.name} onChange={(event) => updateDraft("name", event.target.value)} />
              {fieldErrors.name ? (
                <span className="mt-1 block text-sm font-normal text-status-error-fg" role="alert">
                  {fieldErrors.name}
                </span>
              ) : null}
            </label>
            <label className="block text-sm font-semibold text-ink">
              Specialty
              <input
                className={fieldClass}
                placeholder="Kayachikitsa"
                value={draft.specialty}
                onChange={(event) => updateDraft("specialty", event.target.value)}
              />
              {fieldErrors.specialty ? (
                <span className="mt-1 block text-sm font-normal text-status-error-fg" role="alert">
                  {fieldErrors.specialty}
                </span>
              ) : null}
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Qualifications
              <input
                className={fieldClass}
                placeholder="BAMS, MD (Ayu)"
                value={draft.qualifications}
                onChange={(event) => updateDraft("qualifications", event.target.value)}
              />
              {fieldErrors.qualifications ? (
                <span className="mt-1 block text-sm font-normal text-status-error-fg" role="alert">
                  {fieldErrors.qualifications}
                </span>
              ) : null}
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Bio
              <textarea
                className={`${fieldClass} min-h-24`}
                value={draft.bio}
                onChange={(event) => updateDraft("bio", event.target.value)}
              />
              {fieldErrors.bio ? (
                <span className="mt-1 block text-sm font-normal text-status-error-fg" role="alert">
                  {fieldErrors.bio}
                </span>
              ) : null}
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Portrait
              <input
                className={fieldClass}
                type="file"
                accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
                onChange={(event) => {
                  const file = event.target.files?.[0] ?? null;
                  setPhotoFile(file);
                  setFieldErrors((current) => ({ ...current, photo: file ? photoProblem(file) ?? undefined : undefined }));
                }}
              />
              <span className="mt-1 block text-xs font-normal text-muted">JPEG, PNG, or WebP, up to 512 KB.</span>
              {fieldErrors.photo ? (
                <span className="mt-1 block text-sm font-normal text-status-error-fg" role="alert">
                  {fieldErrors.photo}
                </span>
              ) : null}
            </label>
            {editing?.hasPhoto ? (
              <div className="sm:col-span-2">
                <Button variant="secondary" disabled={saving} onClick={() => void onRemovePhoto()}>
                  Remove portrait
                </Button>
              </div>
            ) : null}
            {formError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2" role="alert">
                {formError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : editing ? "Save changes" : "Save doctor"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <DataTable
        columns={columns}
        rows={doctors}
        getRowId={(doctor) => doctor.id}
        caption="Doctors"
        page={page}
        pageCount={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading doctors…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle="No doctors found."
        emptyDescription="Add a vaidya profile, or change the search."
        filter={
          <form
            className="flex flex-col gap-3 sm:flex-row sm:items-end"
            onSubmit={(event) => {
              event.preventDefault();
              setAppliedQuery(query.trim());
              setAppliedActiveOnly(status === "active");
              setPage(1);
            }}
          >
            <label className="block min-w-0 flex-1 text-sm font-semibold text-ink" htmlFor="doctor-search">
              Search
              <input
                id="doctor-search"
                className={fieldClass}
                placeholder="Name or specialty"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
              />
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="doctor-status">
              Status
              <select
                id="doctor-status"
                className={fieldClass}
                value={status}
                onChange={(event) => setStatus(event.target.value)}
              >
                <option value="">All physicians</option>
                <option value="active">Active only</option>
              </select>
            </label>
            <Button type="submit">Search</Button>
          </form>
        }
      />
    </div>
  );
}
