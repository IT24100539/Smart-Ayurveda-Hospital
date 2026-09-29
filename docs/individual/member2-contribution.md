# Member 2 — Treatment information

Branch: `member2/treatment-information`

## Owned component

The treatment catalogue and weekly schedules, including capacity per slot. Patients browse treatments. Staff edit the catalogue and the schedule.

## Code

- API: `TreatmentsController`, `TreatmentService`, `TreatmentRequestValidators`
- PostgreSQL: migration `20260911163800_AddTreatmentAndSchedule` (`Treatments`, `TreatmentSchedules`, `Therapists`)
- React: `web-staff/src/pages/TreatmentsPage.tsx`, `web-staff/src/components/treatments/TreatmentsView.tsx`
- Flutter: `mobile-patient/lib/src/features/treatments/`
- Agent: `agent-service/app/agents/treatment_info_agent.py`

## Reflection

Catalogue reads are anonymous so a patient can browse before booking. Creating and editing treatments and schedules requires a staff role. The treatment-info agent reads the catalogue through the internal API and returns a proposal; it does not publish a schedule change.
