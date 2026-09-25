# Member 1 — Patient and user management

Branch: `member1/patient-user-management`

## Owned component

Patient registration, login, and the patient record (UHID, prakriti, vikriti). Staff search and edit patients in the React portal. Patients register and sign in from the Flutter app.

## Code

- API: `AuthController`, `PatientsController`, `AuthService`, `PatientService`, `RegisterRequestValidator`, `PatientRequestValidators`
- PostgreSQL: migration `20260909170636_InitialIdentity` (`Users`, `StaffUsers`, `Patients`)
- React: `web-staff/src/pages/PatientsPage.tsx`, `web-staff/src/store/authStore.ts`
- Flutter: `mobile-patient/lib/src/features/auth/`
- Agent: `agent-service/app/agents/intake_agent.py`, routed as `patient_info` from `app/graph/coordinator.py`

## Reflection

The patient API rejects unauthenticated reads. Registration and login issue a JWT. The staff portal lists and creates patients. The patient app registers and signs in, then stores the token. The intake agent answers prakriti, vikriti, and registration questions and does not write the patient row itself.
