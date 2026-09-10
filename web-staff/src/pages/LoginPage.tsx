import { useEffect, useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { isStaffRole } from "../auth/roles";
import { hasValidJwt, useAuthStore } from "../store/authStore";

type FieldErrors = {
  email?: string;
  password?: string;
};

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
    <div className="grid min-h-screen place-items-center px-4">
      <form
        className="w-full max-w-md rounded-2xl border border-surface-border bg-surface-raised p-8 shadow-sm"
        onSubmit={onSubmit}
        noValidate
      >
        <h1>Staff sign-in</h1>
        <p className="mt-1 text-sm text-muted">
          Sign in to manage patients, panchakarma, and hospital operations.
        </p>

        <label className="mt-6 block text-sm font-semibold" htmlFor="email">
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
          className="mt-1 w-full rounded-lg border border-surface-border bg-white px-3 py-2 outline-none ring-primary focus:ring-2"
        />
        {fieldErrors.email ? (
          <p className="mt-1 text-sm text-danger" role="alert">
            {fieldErrors.email}
          </p>
        ) : null}

        <label className="mt-4 block text-sm font-semibold" htmlFor="password">
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
          className="mt-1 w-full rounded-lg border border-surface-border bg-white px-3 py-2 outline-none ring-primary focus:ring-2"
        />
        {fieldErrors.password ? (
          <p className="mt-1 text-sm text-danger" role="alert">
            {fieldErrors.password}
          </p>
        ) : null}

        {formError ? (
          <p className="mt-4 text-sm text-danger" role="alert">
            {formError}
          </p>
        ) : null}

        <button
          className="mt-6 w-full rounded-lg bg-primary px-4 py-2.5 font-semibold text-white hover:bg-primary-dark disabled:opacity-60"
          type="submit"
          disabled={busy}
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </div>
  );
}
