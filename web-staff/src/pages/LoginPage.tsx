import { useEffect, useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { isStaffRole } from "../auth/roles";
import { hasValidJwt, useAuthStore } from "../store/authStore";
import { HospitalLogo } from "../components/HospitalMark";
import { Button, Card } from "../components/ui";

type FieldErrors = {
  email?: string;
  password?: string;
};

function AlertMark() {
  return (
    <svg viewBox="0 0 16 16" aria-hidden="true" className="h-3.5 w-3.5 shrink-0">
      <path d="M8 2.5 14 13.5H2L8 2.5z" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      <path d="M8 7v2.8" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function firstFieldError(fields: Record<string, string[]>, names: string[]): string | undefined {
  for (const name of names) {
    const value = fields[name]?.[0];
    if (value) {
      return value;
    }
  }
  return undefined;
}

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const login = useAuthStore((state) => state.login);
  const token = useAuthStore((state) => state.token);
  const user = useAuthStore((state) => state.user);
  const [hydrated, setHydrated] = useState(() => useAuthStore.persist.hasHydrated());
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const unsub = useAuthStore.persist.onFinishHydration(() => setHydrated(true));
    setHydrated(useAuthStore.persist.hasHydrated());
    return unsub;
  }, []);

  if (hydrated && hasValidJwt(token) && user) {
    const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname;
    return <Navigate to={from && from !== "/login" ? from : "/dashboard"} replace />;
  }

  function validate(): FieldErrors {
    const next: FieldErrors = {};
    if (!email.trim()) {
      next.email = "Email is required.";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      next.email = "Enter a valid email address.";
    }
    if (!password) {
      next.password = "Password is required.";
    } else if (password.length < 8) {
      next.password = "Password must be at least 8 characters.";
    }
    return next;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const next = validate();
    setFieldErrors(next);
    setFormError(null);
    if (Object.keys(next).length > 0) {
      return;
    }

    setBusy(true);
    try {
      const auth = await api.login(email.trim(), password);
      if (!isStaffRole(auth.user.role)) {
        setFormError("This portal is for hospital staff.");
        return;
      }
      login(auth.token, auth.user);
      navigate("/dashboard", { replace: true });
    } catch (err) {
      if (err instanceof ApiError) {
        const emailError = firstFieldError(err.fields, ["email", "Email"]);
        const passwordError = firstFieldError(err.fields, ["password", "Password"]);
        setFieldErrors({ email: emailError, password: passwordError });
        setFormError(emailError || passwordError ? null : err.message);
      } else {
        setFormError("Unable to sign in.");
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <section className="relative hidden overflow-hidden bg-primary-dark px-12 py-16 text-white lg:flex lg:flex-col lg:justify-between">
        <img
          src="/images/ayurveda-courtyard.png"
          alt=""
          className="absolute inset-0 h-full w-full object-cover opacity-40"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-primary-dark via-primary-dark/80 to-primary-dark/40" />
        <div className="relative">
        <div className="flex items-center gap-3">
          <HospitalLogo className="h-14 w-14" />
          <div>
            <p className="font-display text-2xl">Smart Ayurveda</p>
            <p className="mt-1 text-sm text-primary-muted">Hospital operations</p>
          </div>
        </div>
        <div className="relative">
          <h2 className="max-w-md font-display text-4xl leading-tight text-white">
            One desk for patients, therapies, and ward beds.
          </h2>
          <ul className="mt-8 space-y-3 text-sm text-primary-muted">
            <li>Look up a UHID and record prakriti and vikriti.</li>
            <li>Review the panchakarma schedule before a slot is double-booked.</li>
            <li>Approve admissions and agent plans before they reach the patient.</li>
          </ul>
        </div>
        <p className="relative text-xs text-primary-muted">Staff only. Patients use the mobile app.</p>
        </div>
      </section>
      <div className="grid place-items-center bg-surface px-4 py-10">
      <Card className="w-full max-w-md p-8">
        <form onSubmit={onSubmit} noValidate>
          <h1>Staff sign-in</h1>
          <p className="mt-1 text-sm text-muted">
            Sign in to manage patients, panchakarma, and hospital operations.
          </p>

          <label className="mt-6 block text-sm font-semibold text-ink" htmlFor="email">
            Email
          </label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            aria-invalid={Boolean(fieldErrors.email)}
            className="mt-1 w-full rounded-md border border-surface-border bg-surface-raised px-3 py-2 text-ink outline-none ring-primary focus:ring-2"
          />
          {fieldErrors.email ? (
            <p className="mt-1 flex items-center gap-1 text-sm text-status-error-fg" role="alert">
              <AlertMark />
              {fieldErrors.email}
            </p>
          ) : null}

          <label className="mt-4 block text-sm font-semibold text-ink" htmlFor="password">
            Password
          </label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-invalid={Boolean(fieldErrors.password)}
            className="mt-1 w-full rounded-md border border-surface-border bg-surface-raised px-3 py-2 text-ink outline-none ring-primary focus:ring-2"
          />
          {fieldErrors.password ? (
            <p className="mt-1 flex items-center gap-1 text-sm text-status-error-fg" role="alert">
              <AlertMark />
              {fieldErrors.password}
            </p>
          ) : null}

          {formError ? (
            <p className="mt-4 flex items-center gap-1 text-sm text-status-error-fg" role="alert">
              <AlertMark />
              {formError}
            </p>
          ) : null}

          <Button className="mt-6 w-full" type="submit" disabled={busy}>
            {busy ? "Signing in…" : "Sign in"}
          </Button>
        </form>
      </Card>
      </div>
    </div>
  );
}
