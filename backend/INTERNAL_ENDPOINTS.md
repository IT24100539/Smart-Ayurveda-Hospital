Internal Scheduling & Bed Agent endpoints
=========================================

Configure `InternalService__Key` in the backend environment (or `InternalService:Key`
in a secret configuration provider). Set the Python agent's `INTERNAL_SERVICE_KEY`
to the same secret. No default secret is supplied; missing configuration fails closed.
Send the key in `X-Internal-Service-Key`. JWTs alone cannot access these routes,
and the service key grants no access to public Staff/Admin/Patient routes.

- `GET /api/internal/wards/{id}/availability`: `wardId`, `wardName`,
  `totalCapacity`, `occupiedCapacity`, `freeCapacity`; no bed or patient data.
- `POST /api/internal/admissions`: `patientId`, `wardId`, `reason`,
  `preferredDate` (YYYY-MM-DD). Snake_case field equivalents are also accepted.
  Returns HTTP 201 with `admissionRequestId`. Creates only a Pending request,
  with RequestedByAgent=true and no bed assignment. Human approval remains
  through `PATCH /api/admissions/{id}/decision` with existing JWT authorization.
- `GET /api/treatments/{id}/availability?date=YYYY-MM-DD`: `treatmentId`,
  `date`, `available`, optional `reason`. Uses BookingValidator and existing
  active appointment counts; any matching schedule with capacity is available.
  Nonpositive MaxPatients retains the booking service's unlimited convention.
  Availability is a snapshot, never a reservation or allocation.

The pre-existing internal `POST /api/internal/appointments/check-slot` placeholder
is also key-protected; its placeholder behavior is otherwise unchanged.
