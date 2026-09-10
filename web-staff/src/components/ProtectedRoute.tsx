import { useEffect, useState, type ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import type { UserRole } from "../auth/roles";
import { hasValidJwt, useAuthStore } from "../store/authStore";

type ProtectedRouteProps = {
  children: ReactNode;
  roles?: UserRole[];
};

export function ProtectedRoute({ children, roles }: ProtectedRouteProps) {
  const location = useLocation();
  const token = useAuthStore((state) => state.token);
  const user = useAuthStore((state) => state.user);
  const [hydrated, setHydrated] = useState(() => useAuthStore.persist.hasHydrated());

  useEffect(() => {
    const unsub = useAuthStore.persist.onFinishHydration(() => setHydrated(true));
    setHydrated(useAuthStore.persist.hasHydrated());
    return unsub;
  }, []);

  if (!hydrated) {
    return (
      <div className="grid min-h-screen place-items-center text-muted" role="status">
        Loading…
      </div>
    );
  }

  if (!hasValidJwt(token) || !user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (roles && roles.length > 0 && !roles.includes(user.role)) {
    return (
      <section>
        <h1>Access denied</h1>
      </section>
    );
  }

  return <>{children}</>;
}
