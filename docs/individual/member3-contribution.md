# Member 3 — Appointments, wards, and beds

Branch: `member3/appointment-ward-bed`

## Owned component

Appointment requests, slot capacity, ward occupancy, and admission requests. Approving an admission assigns one free bed. A second booking is rejected when the last seat is taken.

## Code

- API: `AppointmentsController`, `AppointmentActionsController`, `WardsController`, `AdmissionsController`, `AppointmentService`, `WardService`
- PostgreSQL: migration `20260911090858_AddAppointmentAndWard` (`Appointments`, `Wards`, `Beds`, `AdmissionRequests`)
- React: `web-staff/src/pages/AppointmentsPage.tsx`, `web-staff/src/pages/WardsPage.tsx`
- Flutter: `mobile-patient/lib/src/features/appointments/`, `mobile-patient/lib/src/features/wards/`
- Agent: `agent-service/app/agents/scheduling_bed_agent.py`
- Internal routes: `InternalSchedulingController` (`InternalServiceOnly`)

## Reflection

Patients request a visit. Staff approve it. Ward staff approve an admission only when a bed is free. The scheduling agent files a pending admission and does not allocate the bed; staff confirm that on the ward page.
