CREATE TABLE audit_logs (
    "Id" uuid NOT NULL,
    "ActorUserId" uuid,
    "ActorEmail" character varying(256) NOT NULL,
    "ActorRole" character varying(32),
    "Action" character varying(64) NOT NULL,
    "EntityName" character varying(64) NOT NULL,
    "EntityId" character varying(64) NOT NULL,
    "TargetUserId" uuid NOT NULL,
    "TargetEmail" character varying(256) NOT NULL,
    "Details" character varying(2000) NOT NULL,
    "IpAddress" character varying(64),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id")
);


CREATE TABLE "DoctorRosters" (
    "Id" uuid NOT NULL,
    "DoctorUserId" uuid NOT NULL,
    "DayOfWeek" integer NOT NULL,
    "StartTime" interval NOT NULL,
    "EndTime" interval NOT NULL,
    "MaxPatients" integer NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "UnavailabilityReason" text,
    "LeaveStartDate" timestamp with time zone,
    "LeaveEndDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DoctorRosters" PRIMARY KEY ("Id")
);


CREATE TABLE doctors (
    "Id" uuid NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Specialty" character varying(160) NOT NULL,
    "Qualifications" character varying(400) NOT NULL,
    "Bio" character varying(2000),
    "IsActive" boolean NOT NULL,
    "IsSample" boolean NOT NULL DEFAULT FALSE,
    "PhotoStorageKey" character varying(64),
    "PhotoContentType" character varying(32),
    "PhotoSizeBytes" bigint,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_doctors" PRIMARY KEY ("Id")
);


CREATE TABLE "MedicalDocuments" (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "Title" text NOT NULL,
    "Category" integer NOT NULL,
    "FilePath" text NOT NULL,
    "ContentType" text NOT NULL,
    "FileSizeBytes" bigint NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    "UploadedByUserId" uuid NOT NULL,
    "Summary" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MedicalDocuments" PRIMARY KEY ("Id")
);


CREATE TABLE patients (
    "Id" uuid NOT NULL,
    "Uhid" character varying(32) NOT NULL,
    "FirstName" character varying(80) NOT NULL,
    "LastName" character varying(80) NOT NULL,
    "DateOfBirth" date NOT NULL,
    "Gender" integer NOT NULL,
    "Phone" character varying(20) NOT NULL,
    "Email" character varying(256),
    "Address" character varying(500),
    "BloodGroup" character varying(8),
    "Allergies" character varying(1000),
    "Prakriti" integer NOT NULL,
    "Vikriti" integer NOT NULL,
    "PasswordHash" character varying(256),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_patients" PRIMARY KEY ("Id")
);


CREATE TABLE staff_users (
    "Id" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "FullName" character varying(160) NOT NULL,
    "Role" integer NOT NULL,
    "Specialization" character varying(160),
    "Phone" character varying(20),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_staff_users" PRIMARY KEY ("Id")
);


CREATE TABLE treatments (
    "Id" uuid NOT NULL,
    "Name" character varying(160) NOT NULL,
    "NameSinhala" character varying(160) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "DescriptionSinhala" character varying(2000) NOT NULL,
    "Category" character varying(32) NOT NULL,
    "IsActive" boolean NOT NULL,
    "DurationMinutes" integer NOT NULL,
    "UnitPrice" numeric(12,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_treatments" PRIMARY KEY ("Id")
);


CREATE TABLE users (
    "Id" uuid NOT NULL,
    "FullName" character varying(160) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PhoneNumber" character varying(20) NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "Role" character varying(32) NOT NULL,
    "IsActive" boolean NOT NULL,
    "TokenVersion" integer NOT NULL DEFAULT 1,
    "MustChangePassword" boolean NOT NULL DEFAULT FALSE,
    "FailedLoginCount" integer NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "PasswordResetTokenHash" character varying(128),
    "PasswordResetTokenExpiresAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id")
);


CREATE TABLE wards (
    "Id" uuid NOT NULL,
    "Name" character varying(120) NOT NULL,
    "NameSinhala" character varying(120) NOT NULL,
    "Gender" character varying(16) NOT NULL,
    "TotalCapacity" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_wards" PRIMARY KEY ("Id")
);


CREATE TABLE workflow_executions (
    "Id" uuid NOT NULL,
    "AgentName" character varying(64) NOT NULL,
    "ObjectiveText" character varying(4000) NOT NULL,
    "PlanJson" jsonb NOT NULL,
    "CompletedStepsJson" jsonb NOT NULL,
    "ToolResultsJson" jsonb NOT NULL,
    "ValidationResultsJson" jsonb NOT NULL,
    "ErrorsJson" jsonb,
    "ApprovalStatus" character varying(32) NOT NULL,
    "FinalOutcome" character varying(32) NOT NULL,
    "RelatedEntityType" character varying(64),
    "RelatedEntityId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_workflow_executions" PRIMARY KEY ("Id")
);


CREATE TABLE patient_device_tokens (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "Token" character varying(4096) NOT NULL,
    "Platform" character varying(16) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_patient_device_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT ck_patient_device_tokens_platform CHECK ("Platform" IN ('android', 'ios', 'web')),
    CONSTRAINT "FK_patient_device_tokens_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE CASCADE
);


CREATE TABLE notifications (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "Title" character varying(160) NOT NULL,
    "Message" character varying(1000) NOT NULL,
    "Type" integer NOT NULL,
    "IsRead" boolean NOT NULL,
    "StaffUserId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_notifications_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_notifications_staff_users_StaffUserId" FOREIGN KEY ("StaffUserId") REFERENCES staff_users ("Id") ON DELETE RESTRICT
);


CREATE TABLE invoices (
    "Id" uuid NOT NULL,
    "InvoiceNumber" character varying(32) NOT NULL,
    "PatientId" uuid NOT NULL,
    "Currency" character varying(3) NOT NULL DEFAULT 'LKR',
    "Status" character varying(32) NOT NULL,
    "Total" numeric(14,2) NOT NULL,
    "AmountPaid" numeric(14,2) NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "IssuedAt" timestamp with time zone,
    "PaidAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "Notes" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_invoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_invoices_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_invoices_users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE TABLE therapists (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "FullName" character varying(160) NOT NULL,
    "Specialization" character varying(160) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_therapists" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_therapists_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
);


CREATE TABLE beds (
    "Id" uuid NOT NULL,
    "WardId" uuid NOT NULL,
    "BedLabel" character varying(16) NOT NULL,
    "IsOccupied" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_beds" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_beds_wards_WardId" FOREIGN KEY ("WardId") REFERENCES wards ("Id") ON DELETE RESTRICT
);


CREATE TABLE invoice_payments (
    "Id" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "Amount" numeric(14,2) NOT NULL,
    "Method" character varying(32) NOT NULL,
    "PaidOn" timestamp with time zone NOT NULL,
    "Reference" character varying(80),
    "RecordedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_invoice_payments" PRIMARY KEY ("Id"),
    CONSTRAINT ck_invoice_payments_amount CHECK ("Amount" > 0),
    CONSTRAINT "FK_invoice_payments_invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES invoices ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_invoice_payments_users_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE TABLE treatment_schedules (
    "Id" uuid NOT NULL,
    "TreatmentId" uuid NOT NULL,
    "TherapistId" uuid,
    "DayOfWeek" character varying(16) NOT NULL,
    "StartTime" time without time zone NOT NULL,
    "EndTime" time without time zone NOT NULL,
    "TimeSlot" character varying(32) NOT NULL,
    "MaxSlotsPerDay" integer NOT NULL,
    "MaxPatients" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_treatment_schedules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_treatment_schedules_therapists_TherapistId" FOREIGN KEY ("TherapistId") REFERENCES therapists ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_treatment_schedules_treatments_TreatmentId" FOREIGN KEY ("TreatmentId") REFERENCES treatments ("Id") ON DELETE RESTRICT
);


CREATE TABLE admission_requests (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "WardId" uuid,
    "BedId" uuid,
    "Reason" character varying(1000) NOT NULL,
    "PreferredDate" date NOT NULL,
    "Status" character varying(32) NOT NULL,
    "RequestedByAgent" boolean NOT NULL,
    "DecidedById" uuid,
    "DecidedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_admission_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_admission_requests_beds_BedId" FOREIGN KEY ("BedId") REFERENCES beds ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_admission_requests_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_admission_requests_users_DecidedById" FOREIGN KEY ("DecidedById") REFERENCES users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_admission_requests_wards_WardId" FOREIGN KEY ("WardId") REFERENCES wards ("Id") ON DELETE RESTRICT
);


CREATE TABLE appointments (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "TreatmentId" uuid NOT NULL,
    "ScheduleId" uuid,
    "RequestedDate" date NOT NULL,
    "RequestedTimeSlot" character varying(32) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "DecidedById" uuid,
    "DecidedAt" timestamp with time zone,
    "DoctorId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_appointments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_appointments_doctors_DoctorId" FOREIGN KEY ("DoctorId") REFERENCES doctors ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_appointments_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_appointments_treatment_schedules_ScheduleId" FOREIGN KEY ("ScheduleId") REFERENCES treatment_schedules ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_appointments_treatments_TreatmentId" FOREIGN KEY ("TreatmentId") REFERENCES treatments ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_appointments_users_DecidedById" FOREIGN KEY ("DecidedById") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE TABLE feedbacks (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "PatientNameSnapshot" character varying(160) NOT NULL,
    "AppointmentId" uuid,
    "TreatmentId" uuid,
    "Rating" integer NOT NULL,
    "Comment" character varying(2000) NOT NULL,
    "IsAnonymous" boolean NOT NULL,
    "Sentiment" integer,
    "Category" integer,
    "Status" integer NOT NULL,
    "ModeratedById" uuid,
    "ModeratedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_feedbacks" PRIMARY KEY ("Id"),
    CONSTRAINT ck_feedbacks_rating CHECK ("Rating" >= 1 AND "Rating" <= 5),
    CONSTRAINT "FK_feedbacks_appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES appointments ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_feedbacks_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_feedbacks_staff_users_ModeratedById" FOREIGN KEY ("ModeratedById") REFERENCES staff_users ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_feedbacks_treatments_TreatmentId" FOREIGN KEY ("TreatmentId") REFERENCES treatments ("Id") ON DELETE SET NULL
);


CREATE TABLE invoice_lines (
    "Id" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "SortOrder" integer NOT NULL,
    "Source" character varying(32) NOT NULL,
    "Description" character varying(240) NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(14,2) NOT NULL,
    "LineTotal" numeric(14,2) NOT NULL,
    "OpenSourceKey" character varying(80),
    "TreatmentId" uuid,
    "AppointmentId" uuid,
    "AdmissionRequestId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_invoice_lines" PRIMARY KEY ("Id"),
    CONSTRAINT ck_invoice_lines_one_source CHECK (("AppointmentId" IS NOT NULL AND "AdmissionRequestId" IS NULL) OR ("AppointmentId" IS NULL AND "AdmissionRequestId" IS NOT NULL)),
    CONSTRAINT ck_invoice_lines_quantity CHECK ("Quantity" >= 1),
    CONSTRAINT ck_invoice_lines_unit_price CHECK ("UnitPrice" > 0),
    CONSTRAINT "FK_invoice_lines_admission_requests_AdmissionRequestId" FOREIGN KEY ("AdmissionRequestId") REFERENCES admission_requests ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_invoice_lines_appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES appointments ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_invoice_lines_invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES invoices ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_invoice_lines_treatments_TreatmentId" FOREIGN KEY ("TreatmentId") REFERENCES treatments ("Id") ON DELETE RESTRICT
);


CREATE TABLE prescriptions (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "AppointmentId" uuid NOT NULL,
    "DoctorUserId" uuid NOT NULL,
    "DoctorName" character varying(160) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "RevisionNumber" integer NOT NULL,
    "RootPrescriptionId" uuid NOT NULL,
    "RevisesPrescriptionId" uuid,
    "SupersededByPrescriptionId" uuid,
    "IssuedAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "SupersededAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_prescriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_prescriptions_appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES appointments ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescriptions_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescriptions_prescriptions_RevisesPrescriptionId" FOREIGN KEY ("RevisesPrescriptionId") REFERENCES prescriptions ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescriptions_prescriptions_SupersededByPrescriptionId" FOREIGN KEY ("SupersededByPrescriptionId") REFERENCES prescriptions ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescriptions_users_DoctorUserId" FOREIGN KEY ("DoctorUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE TABLE complaints (
    "Id" uuid NOT NULL,
    "PatientId" uuid NOT NULL,
    "FeedbackId" uuid,
    "Subject" character varying(200) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Priority" integer NOT NULL,
    "Status" integer NOT NULL,
    "AssignedToId" uuid,
    "EscalatedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_complaints" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_complaints_feedbacks_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES feedbacks ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_complaints_patients_PatientId" FOREIGN KEY ("PatientId") REFERENCES patients ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_complaints_staff_users_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES staff_users ("Id") ON DELETE SET NULL
);


CREATE TABLE feedback_reactions (
    "Id" uuid NOT NULL,
    "FeedbackId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ReactionType" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_feedback_reactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_feedback_reactions_feedbacks_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES feedbacks ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_feedback_reactions_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE TABLE feedback_replies (
    "Id" uuid NOT NULL,
    "FeedbackId" uuid NOT NULL,
    "UserId" uuid,
    "UserRole" integer NOT NULL,
    "Reply" character varying(2000) NOT NULL,
    "IsAiGenerated" boolean NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_feedback_replies" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_feedback_replies_feedbacks_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES feedbacks ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_feedback_replies_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
);


CREATE TABLE prescription_items (
    "Id" uuid NOT NULL,
    "PrescriptionId" uuid NOT NULL,
    "SortOrder" integer NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Dosage" character varying(120) NOT NULL,
    "Frequency" character varying(120) NOT NULL,
    "Duration" character varying(80) NOT NULL,
    "Instructions" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_prescription_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_prescription_items_prescriptions_PrescriptionId" FOREIGN KEY ("PrescriptionId") REFERENCES prescriptions ("Id") ON DELETE CASCADE
);


CREATE TABLE prescription_revisions (
    "Id" uuid NOT NULL,
    "PreviousPrescriptionId" uuid NOT NULL,
    "RevisedPrescriptionId" uuid NOT NULL,
    "RevisionNumber" integer NOT NULL,
    "RevisedByUserId" uuid NOT NULL,
    "RevisedAt" timestamp with time zone NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_prescription_revisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_prescription_revisions_prescriptions_PreviousPrescriptionId" FOREIGN KEY ("PreviousPrescriptionId") REFERENCES prescriptions ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescription_revisions_prescriptions_RevisedPrescriptionId" FOREIGN KEY ("RevisedPrescriptionId") REFERENCES prescriptions ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_prescription_revisions_users_RevisedByUserId" FOREIGN KEY ("RevisedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);


CREATE INDEX "IX_admission_requests_BedId" ON admission_requests ("BedId");


CREATE INDEX "IX_admission_requests_DecidedById" ON admission_requests ("DecidedById");


CREATE INDEX "IX_admission_requests_PatientId" ON admission_requests ("PatientId");


CREATE INDEX "IX_admission_requests_WardId" ON admission_requests ("WardId");


CREATE INDEX "IX_appointments_DecidedById" ON appointments ("DecidedById");


CREATE INDEX "IX_appointments_DoctorId" ON appointments ("DoctorId");


CREATE INDEX "IX_appointments_ScheduleId" ON appointments ("ScheduleId");


CREATE INDEX ix_appointments_treatment_requested_date ON appointments ("TreatmentId", "RequestedDate");


CREATE UNIQUE INDEX ux_appointments_patient_treatment_slot_active ON appointments ("PatientId", "TreatmentId", "RequestedDate", "RequestedTimeSlot") WHERE "Status" <> 'Cancelled';


CREATE INDEX "IX_audit_logs_ActorUserId" ON audit_logs ("ActorUserId");


CREATE INDEX "IX_audit_logs_CreatedAt" ON audit_logs ("CreatedAt");


CREATE INDEX "IX_audit_logs_EntityName_CreatedAt" ON audit_logs ("EntityName", "CreatedAt");


CREATE INDEX "IX_audit_logs_TargetUserId" ON audit_logs ("TargetUserId");


CREATE UNIQUE INDEX "IX_beds_WardId_BedLabel" ON beds ("WardId", "BedLabel");


CREATE INDEX "IX_complaints_AssignedToId" ON complaints ("AssignedToId");


CREATE INDEX "IX_complaints_CreatedAt" ON complaints ("CreatedAt");


CREATE INDEX "IX_complaints_FeedbackId" ON complaints ("FeedbackId");


CREATE INDEX "IX_complaints_PatientId" ON complaints ("PatientId");


CREATE INDEX "IX_complaints_Status" ON complaints ("Status");


CREATE INDEX "IX_doctors_IsActive" ON doctors ("IsActive");


CREATE INDEX "IX_doctors_Name" ON doctors ("Name");


CREATE UNIQUE INDEX "IX_feedback_reactions_FeedbackId_UserId" ON feedback_reactions ("FeedbackId", "UserId");


CREATE INDEX "IX_feedback_reactions_UserId" ON feedback_reactions ("UserId");


CREATE INDEX "IX_feedback_replies_FeedbackId" ON feedback_replies ("FeedbackId");


CREATE INDEX "IX_feedback_replies_UserId" ON feedback_replies ("UserId");


CREATE INDEX "IX_feedbacks_AppointmentId" ON feedbacks ("AppointmentId");


CREATE INDEX "IX_feedbacks_Category" ON feedbacks ("Category");


CREATE INDEX "IX_feedbacks_CreatedAt" ON feedbacks ("CreatedAt");


CREATE INDEX "IX_feedbacks_ModeratedById" ON feedbacks ("ModeratedById");


CREATE INDEX "IX_feedbacks_PatientId" ON feedbacks ("PatientId");


CREATE INDEX "IX_feedbacks_Sentiment" ON feedbacks ("Sentiment");


CREATE INDEX "IX_feedbacks_Status" ON feedbacks ("Status");


CREATE INDEX "IX_feedbacks_TreatmentId" ON feedbacks ("TreatmentId");


CREATE INDEX "IX_invoice_lines_AdmissionRequestId" ON invoice_lines ("AdmissionRequestId");


CREATE INDEX "IX_invoice_lines_AppointmentId" ON invoice_lines ("AppointmentId");


CREATE INDEX ix_invoice_lines_order ON invoice_lines ("InvoiceId", "SortOrder");


CREATE INDEX "IX_invoice_lines_TreatmentId" ON invoice_lines ("TreatmentId");


CREATE UNIQUE INDEX ux_invoice_lines_open_source ON invoice_lines ("OpenSourceKey") WHERE "OpenSourceKey" IS NOT NULL;


CREATE INDEX ix_invoice_payments_invoice_id ON invoice_payments ("InvoiceId");


CREATE INDEX "IX_invoice_payments_RecordedByUserId" ON invoice_payments ("RecordedByUserId");


CREATE INDEX "IX_invoices_CreatedByUserId" ON invoices ("CreatedByUserId");


CREATE INDEX ix_invoices_patient_id ON invoices ("PatientId");


CREATE INDEX ix_invoices_status ON invoices ("Status");


CREATE UNIQUE INDEX ux_invoices_number ON invoices ("InvoiceNumber");


CREATE INDEX "IX_notifications_CreatedAt" ON notifications ("CreatedAt");


CREATE INDEX "IX_notifications_PatientId_IsRead" ON notifications ("PatientId", "IsRead");


CREATE INDEX "IX_notifications_StaffUserId" ON notifications ("StaffUserId");


CREATE INDEX ix_patient_device_tokens_patient_id ON patient_device_tokens ("PatientId");


CREATE UNIQUE INDEX ux_patient_device_tokens_token ON patient_device_tokens ("Token");


CREATE UNIQUE INDEX "IX_patients_Phone" ON patients ("Phone");


CREATE UNIQUE INDEX "IX_patients_Uhid" ON patients ("Uhid");


CREATE INDEX ix_prescription_items_order ON prescription_items ("PrescriptionId", "SortOrder");


CREATE INDEX ix_prescription_revisions_previous ON prescription_revisions ("PreviousPrescriptionId");


CREATE INDEX "IX_prescription_revisions_RevisedByUserId" ON prescription_revisions ("RevisedByUserId");


CREATE UNIQUE INDEX ux_prescription_revisions_revised ON prescription_revisions ("RevisedPrescriptionId");


CREATE INDEX "IX_prescriptions_DoctorUserId" ON prescriptions ("DoctorUserId");


CREATE INDEX ix_prescriptions_patient_id ON prescriptions ("PatientId");


CREATE INDEX "IX_prescriptions_RevisesPrescriptionId" ON prescriptions ("RevisesPrescriptionId");


CREATE INDEX ix_prescriptions_root ON prescriptions ("RootPrescriptionId");


CREATE INDEX ix_prescriptions_status ON prescriptions ("Status");


CREATE INDEX "IX_prescriptions_SupersededByPrescriptionId" ON prescriptions ("SupersededByPrescriptionId");


CREATE UNIQUE INDEX ux_prescriptions_appointment_open ON prescriptions ("AppointmentId") WHERE "Status" IN ('Draft', 'Issued');


CREATE UNIQUE INDEX "IX_staff_users_Email" ON staff_users ("Email");


CREATE UNIQUE INDEX "IX_therapists_UserId" ON therapists ("UserId");


CREATE INDEX ix_schedules_treatment_day ON treatment_schedules ("TreatmentId", "DayOfWeek");


CREATE UNIQUE INDEX ix_schedules_unique_slot ON treatment_schedules ("TreatmentId", "TherapistId", "DayOfWeek", "StartTime");


CREATE INDEX "IX_treatment_schedules_TherapistId" ON treatment_schedules ("TherapistId");


CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");


CREATE INDEX ix_workflow_executions_agent_name ON workflow_executions ("AgentName");


CREATE INDEX ix_workflow_executions_approval_status ON workflow_executions ("ApprovalStatus");


CREATE INDEX ix_workflow_executions_created_at ON workflow_executions ("CreatedAt");


CREATE INDEX ix_workflow_executions_related_entity ON workflow_executions ("RelatedEntityType", "RelatedEntityId");


CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20260909170636_InitialIdentity', '8.0.11'),
('20260911090858_AddAppointmentAndWard', '8.0.11'),
('20260911163800_AddTreatmentAndSchedule', '8.0.11'),
('20260921130434_AddFeedbackAndCommunication', '8.0.11'),
('20260923101804_AddFeedbackDashboardIndexes', '8.0.11'),
('20260923103426_AddNotificationStaffRecipient', '8.0.11'),
('20260925034829_AddWorkflowExecutions', '8.0.11'),
('20260925081832_StandardizeForeignKeyIdSuffix', '8.0.11'),
('20260928050702_RemoveUnusedBillingAndPrescriptionEntities', '8.0.11'),
('20261001171410_AddUserTokenVersionMustChangePasswordAndAuditLogs', '8.0.11'),
('20261002034255_AddDomainEntitiesRosterPrescriptionInvoiceDocument', '8.0.11'),
('20261002083314_AddAuditLogIpAddress', '8.0.11'),
('20261002092652_AddDoctorDirectory', '8.0.11'),
('20261002095017_AddPrescriptionChart', '8.0.11'),
('20261002101249_AddInvoiceBilling', '8.0.11'),
('20261002104119_AddPatientDeviceTokens', '8.0.11');


