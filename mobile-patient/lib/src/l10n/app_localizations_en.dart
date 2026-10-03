// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Smart Ayurveda';

  @override
  String get splashTagline => 'Ayurvedic care, guided by your prakriti';

  @override
  String get chooseLanguage => 'Choose your language';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get languageEnglish => 'English';

  @override
  String get continueLabel => 'Continue';

  @override
  String get signIn => 'Sign in';

  @override
  String get register => 'Register';

  @override
  String get signInSubtitle => 'Sign in to follow your treatment plan';

  @override
  String get registerSubtitle =>
      'Create an account to book your first consultation';

  @override
  String get needAccount => 'New patient? Register';

  @override
  String get haveAccount => 'Already registered? Sign in';

  @override
  String get fullNameLabel => 'Full name';

  @override
  String get emailLabel => 'Email';

  @override
  String get phoneNumberLabel => 'Phone number';

  @override
  String get dateOfBirthLabel => 'Date of birth';

  @override
  String get dateOfBirthRequired => 'Please enter your date of birth';

  @override
  String get genderLabel => 'Gender';

  @override
  String get genderRequired => 'Please select a gender';

  @override
  String get genderFemale => 'Female';

  @override
  String get genderMale => 'Male';

  @override
  String get genderOther => 'Other';

  @override
  String get genderUnspecified => 'Unspecified';

  @override
  String get passwordLabel => 'Password';

  @override
  String get fullNameRequired => 'Please enter your full name';

  @override
  String get emailRequired => 'Please enter your email';

  @override
  String get emailInvalid => 'Please enter a valid email address';

  @override
  String get phoneNumberRequired => 'Please enter your phone number';

  @override
  String get phoneNumberInvalid =>
      'Enter a valid Sri Lankan mobile number (07XXXXXXXX or +947XXXXXXXX)';

  @override
  String get passwordRequired => 'Please enter your password';

  @override
  String passwordTooShort(int minLength) {
    return 'Password must be at least $minLength characters';
  }

  @override
  String passwordTooLong(int maxLength) {
    return 'Password must be at most $maxLength characters';
  }

  @override
  String get invalidCredentialsMessage => 'Incorrect email or password.';

  @override
  String get registrationUnavailableMessage =>
      'Unable to create an account with the details provided.';

  @override
  String get accountTemporarilyLocked =>
      'This account is temporarily locked. Please try again later.';

  @override
  String get tooManyAttempts => 'Too many attempts. Please try again later.';

  @override
  String get genericErrorMessage => 'Something went wrong. Please try again.';

  @override
  String get networkErrorMessage =>
      'Cannot reach the hospital server. Check your connection.';

  @override
  String get sessionExpiredMessage =>
      'Your session has expired. Please sign in again.';

  @override
  String get navHome => 'Home';

  @override
  String get navTreatments => 'Treatments';

  @override
  String get navAppointments => 'Appointments';

  @override
  String get navFeedback => 'Feedback';

  @override
  String get navProfile => 'Profile';

  @override
  String homeGreeting(String name) {
    return 'Ayubowan, $name';
  }

  @override
  String get homeGreetingGeneric => 'Ayubowan';

  @override
  String get homeSubtitle =>
      'Book consultations, follow your treatment schedule, and review your registration summary.';

  @override
  String get homeHospitalName => 'Smart Ayurveda Hospital';

  @override
  String get homeHospitalAddress => 'Pallekele, Kundasale 20168';

  @override
  String get homeHospitalPhone => '+94 81 242 0541';

  @override
  String get homeHospitalHours => 'Mon-Fri 8:00 AM - 5:30 PM';

  @override
  String get homeLoadError => 'Could not load hospital information. Try again.';

  @override
  String get comingSoonTitle => 'Coming soon';

  @override
  String get comingSoonBody =>
      'This part of your care record is still being prepared.';

  @override
  String get treatmentsPlaceholder =>
      'Your panchakarma and therapy history will appear here.';

  @override
  String get appointmentsPlaceholder =>
      'Your nadi pariksha and consultation bookings will appear here.';

  @override
  String get profilePlaceholder =>
      'Your prakriti profile and account settings will appear here.';

  @override
  String get languageSectionTitle => 'Language';

  @override
  String get signOut => 'Sign out';

  @override
  String get leaveFeedback => 'Leave feedback';

  @override
  String get submitFeedbackTitle => 'Share your experience';

  @override
  String get ratingLabel => 'How was this visit?';

  @override
  String get commentLabel => 'Comment';

  @override
  String get commentHint => 'What should the care team know about this visit?';

  @override
  String get commentRequired => 'Please share a short comment';

  @override
  String get ratingRequired => 'Choose a star rating';

  @override
  String get anonymousLabel => 'Post anonymously';

  @override
  String get anonymousHelp => 'Your name stays off the public board.';

  @override
  String get postedAsLabel => 'Posted as';

  @override
  String get yourName => 'Your name';

  @override
  String get submitFeedback => 'Send feedback';

  @override
  String get feedbackSent =>
      'Thank you. Your note will appear after the care team reviews it.';

  @override
  String get linkedVisit => 'Linked to this completed visit';

  @override
  String get feedbackNeedsLink => 'Choose a completed visit or a treatment.';

  @override
  String get writeFeedback => 'Write feedback';

  @override
  String get hubCommunity => 'Community';

  @override
  String get hubMine => 'My feedback';

  @override
  String get hubComplaints => 'Complaints';

  @override
  String get hubNotifications => 'Notifications';

  @override
  String get linkToVisit => 'Completed visit';

  @override
  String get linkToTreatment => 'Treatment';

  @override
  String get chooseTreatment => 'Choose a treatment';

  @override
  String editTimeRemaining(int hours, int minutes) {
    return '${hours}h ${minutes}m left to edit or withdraw';
  }

  @override
  String get feedbackStatusPending => 'Pending review';

  @override
  String get feedbackStatusVisible => 'Visible';

  @override
  String get feedbackStatusHidden => 'Hidden';

  @override
  String get feedbackStatusWithdrawn => 'Withdrawn';

  @override
  String get publicFeedTitle => 'Patient feedback';

  @override
  String get publicFeedEmpty =>
      'No approved notes yet. They will appear here after review.';

  @override
  String get anonymousPatient => 'Anonymous patient';

  @override
  String get helpful => 'Helpful';

  @override
  String get notHelpful => 'Not helpful';

  @override
  String get repliesHeading => 'Replies';

  @override
  String get noReplies => 'No replies yet.';

  @override
  String get careTeam => 'Care team';

  @override
  String get patientRole => 'Patient';

  @override
  String get reactionFailed => 'Could not save your reaction.';

  @override
  String get complaintsTitle => 'My complaints';

  @override
  String get submitComplaintTitle => 'Raise a concern';

  @override
  String get newComplaint => 'New complaint';

  @override
  String get subjectLabel => 'Subject';

  @override
  String get descriptionLabel => 'Description';

  @override
  String get subjectRequired => 'Please enter a subject';

  @override
  String get descriptionRequired => 'Please describe what happened';

  @override
  String get priorityLabel => 'Priority';

  @override
  String get priorityNormal => 'Normal';

  @override
  String get priorityHigh => 'High';

  @override
  String get complaintSent => 'We have received your concern.';

  @override
  String get complaintsEmpty => 'You have not raised a concern yet.';

  @override
  String get statusOpen => 'Open';

  @override
  String get statusInProgress => 'In progress';

  @override
  String get statusEscalated => 'Escalated';

  @override
  String get statusResolved => 'Resolved';

  @override
  String get notificationsTitle => 'Notifications';

  @override
  String get unreadLabel => 'Unread';

  @override
  String get notificationsEmpty => 'You are up to date.';

  @override
  String get notificationReply => 'Reply';

  @override
  String get notificationStatus => 'Status update';

  @override
  String get notificationEscalated => 'Escalated';

  @override
  String get notificationGeneral => 'Notice';

  @override
  String get notificationAppointmentApproved => 'Appointment approved';

  @override
  String get notificationAppointmentRejected => 'Appointment not approved';

  @override
  String get notificationAppointmentRescheduled => 'Appointment rescheduled';

  @override
  String get notificationAppointmentCancelled => 'Appointment cancelled';

  @override
  String get notificationPrescriptionIssued => 'Prescription issued';

  @override
  String get notificationInvoiceIssued => 'Invoice issued';

  @override
  String get retry => 'Try again';

  @override
  String get myFeedbackTitle => 'My feedback';

  @override
  String get myFeedbackEmpty => 'You have not shared feedback yet.';

  @override
  String get editFeedback => 'Edit';

  @override
  String get withdrawFeedback => 'Withdraw';

  @override
  String get withdrawConfirm =>
      'Withdraw this feedback? It will be hidden from the public board.';

  @override
  String get feedbackUpdated => 'Your feedback was updated.';

  @override
  String get feedbackWithdrawn => 'Your feedback was withdrawn.';

  @override
  String get canStillEdit =>
      'You can edit this for 24 hours after it was sent.';

  @override
  String get editingClosed => 'The 24-hour editing window has closed.';

  @override
  String get replyHint => 'Reply to this note';

  @override
  String get sendReply => 'Send reply';

  @override
  String get replySent => 'Your reply was posted.';

  @override
  String get markAllRead => 'Mark all read';

  @override
  String get chooseCompletedVisit => 'Completed visit';

  @override
  String get noCompletedVisit =>
      'No completed visit yet. You can still write about a treatment.';

  @override
  String get escalatedOn => 'Escalated';

  @override
  String get saveChanges => 'Save changes';

  @override
  String get myHealthHubTitle => 'My Health Hub';

  @override
  String get myHealthHubSubtitle =>
      'Therapy sessions, prescriptions, invoices, and documents.';

  @override
  String get myTherapySessionsTitle => 'My therapy sessions';

  @override
  String get noTherapySessionsFound => 'No therapy sessions recorded yet.';

  @override
  String get nextSessionLabel => 'Next Session';

  @override
  String get myRegistrationSummaryTitle => 'My registration summary';

  @override
  String get uhidLabel => 'UHID';

  @override
  String get prakritiLabel => 'Prakriti';

  @override
  String get vikritiLabel => 'Vikriti';

  @override
  String get allergiesLabel => 'Allergies';

  @override
  String get bloodGroupLabel => 'Blood Group';

  @override
  String get notRecorded => 'Not recorded';

  @override
  String get healthHubError => 'Could not load health records.';

  @override
  String sessionsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count sessions',
      one: '1 session',
    );
    return '$_temp0';
  }

  @override
  String get noPatientRecordLinked =>
      'No patient record is linked to this login.';

  @override
  String get noPatientRecordLinkedHelp =>
      'The clinical record must use the same email address. Please contact reception or update your profile to link your clinical record.';

  @override
  String get unlinkedRecordRetry => 'Check again';

  @override
  String get unlinkedRecordHelpAction => 'Contact reception';

  @override
  String get forgotPasswordLink => 'Forgot password?';

  @override
  String get forgotPasswordTitle => 'Reset password';

  @override
  String get forgotPasswordHeading => 'Forgot your password?';

  @override
  String get forgotPasswordBody =>
      'Enter the email on your account. If it is registered, we will send a reset link.';

  @override
  String get forgotPasswordSubmit => 'Send reset link';

  @override
  String get forgotPasswordBackToSignIn => 'Back to sign in';

  @override
  String get forgotPasswordSuccess =>
      'If an account with that email exists, a password reset link has been sent.';

  @override
  String get forgotPasswordError =>
      'Could not send a reset request. Check your connection and try again.';

  @override
  String get resetPasswordTitle => 'New password';

  @override
  String get resetPasswordHeading => 'Create a new password';

  @override
  String get resetPasswordBody =>
      'Enter the reset token from your email and choose a new password.';

  @override
  String get resetTokenLabel => 'Reset token';

  @override
  String get resetTokenRequired => 'Enter the reset token from your email';

  @override
  String get newPasswordLabel => 'New password';

  @override
  String get confirmPasswordLabel => 'Confirm password';

  @override
  String get confirmPasswordRequired => 'Please confirm your password';

  @override
  String get passwordsDoNotMatch => 'Passwords do not match';

  @override
  String get resetPasswordSubmit => 'Reset password';

  @override
  String get resetPasswordSuccess =>
      'Your password has been reset. You can now sign in with the new password.';

  @override
  String get resetPasswordError =>
      'Could not reset the password. The token may be invalid or expired.';

  @override
  String get resetPasswordSignInNow => 'Sign in now';

  @override
  String get passwordNeedsUppercase =>
      'Password must contain at least one uppercase letter';

  @override
  String get passwordNeedsLowercase =>
      'Password must contain at least one lowercase letter';

  @override
  String get passwordNeedsDigit => 'Password must contain at least one digit';

  @override
  String get passwordNeedsSpecial =>
      'Password must contain at least one special character';

  @override
  String get showPassword => 'Show password';

  @override
  String get hidePassword => 'Hide password';

  @override
  String get doctorsTitle => 'Physicians';

  @override
  String get doctorsSubtitle => 'Vaidyas who consult at the hospital.';

  @override
  String get doctorsNavSubtitle => 'Meet the vaidya team';

  @override
  String get doctorsSearchHint => 'Search by name or specialty';

  @override
  String get doctorsEmpty => 'No physicians are listed yet.';

  @override
  String get doctorsSearchEmpty => 'No physicians match your search.';

  @override
  String get doctorsLoadError => 'Could not load physicians.';

  @override
  String get doctorProfileLoadError => 'Could not load this physician.';

  @override
  String get doctorQualificationsLabel => 'Qualifications';

  @override
  String get doctorAboutLabel => 'About';

  @override
  String get healthHubUpcomingTab => 'Upcoming';

  @override
  String get healthHubTherapyTab => 'Therapy';

  @override
  String get healthHubRegistrationTab => 'Registration';

  @override
  String get healthHubUpcomingEmpty => 'No upcoming appointments.';

  @override
  String get healthHubPrescriptionsTab => 'Prescriptions';

  @override
  String get healthHubInvoicesTab => 'Invoices';

  @override
  String get healthHubDocumentsTab => 'Documents';

  @override
  String get myPrescriptionsTitle => 'My prescriptions';

  @override
  String get prescriptionsEmpty => 'No prescriptions have been issued yet.';

  @override
  String get prescriptionsLoadError => 'Could not load prescriptions.';

  @override
  String prescriptionIssuedOn(String date) {
    return 'Issued $date';
  }

  @override
  String prescriptionRevision(int number) {
    return 'Revision $number';
  }

  @override
  String get prescriptionStatusIssued => 'Issued';

  @override
  String get prescriptionStatusSuperseded => 'Superseded';

  @override
  String get prescriptionStatusCancelled => 'Cancelled';

  @override
  String get prescriptionStatusDraft => 'Draft';

  @override
  String get myInvoicesTitle => 'My invoices';

  @override
  String get invoicesEmpty => 'No invoices have been issued yet.';

  @override
  String get invoicesLoadError => 'Could not load invoices.';

  @override
  String get paymentHistoryTitle => 'Payment history';

  @override
  String get paymentsEmpty => 'No payments recorded.';

  @override
  String get invoiceTotalLabel => 'Total';

  @override
  String get amountPaidLabel => 'Paid';

  @override
  String get balanceLabel => 'Balance';

  @override
  String get invoiceStatusIssued => 'Issued';

  @override
  String get invoiceStatusPaid => 'Paid';

  @override
  String get invoiceStatusCancelled => 'Cancelled';

  @override
  String get invoiceStatusDraft => 'Draft';

  @override
  String get paymentMethodCash => 'Cash';

  @override
  String get paymentMethodCard => 'Card';

  @override
  String get paymentMethodBankTransfer => 'Bank transfer';

  @override
  String get myDocumentsTitle => 'My documents';

  @override
  String get documentsEmpty => 'No documents have been uploaded yet.';

  @override
  String get documentsLoadError => 'Could not load documents.';

  @override
  String get documentView => 'View';

  @override
  String get documentDownload => 'Download';

  @override
  String documentSaved(String fileName) {
    return 'Saved $fileName';
  }

  @override
  String get documentDownloadError => 'Could not download this document.';

  @override
  String get documentOpenError => 'Could not open this document.';

  @override
  String get documentPdfNotice => 'This PDF is available to download.';

  @override
  String get documentFileNotice => 'This file is available to download.';

  @override
  String get documentCategoryLabReport => 'Lab report';

  @override
  String get documentCategoryPrescriptionScan => 'Prescription scan';

  @override
  String get documentCategoryDiagnosticScan => 'Diagnostic scan';

  @override
  String get documentCategoryDischargeSummary => 'Discharge summary';

  @override
  String get documentCategoryTreatmentPlan => 'Treatment plan';

  @override
  String get documentCategoryGeneral => 'General';

  @override
  String doctorRatingSummary(String rating, int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count reviews',
      one: '1 review',
    );
    return '$rating from $_temp0';
  }

  @override
  String get appearanceSectionTitle => 'Appearance';

  @override
  String get themeSystem => 'System';

  @override
  String get themeLight => 'Light';

  @override
  String get themeDark => 'Dark';

  @override
  String get privacySectionTitle => 'Privacy';

  @override
  String get privacyPolicyTitle => 'Privacy policy';

  @override
  String get privacyPolicySubtitle =>
      'How your care record is handled in this app.';

  @override
  String get termsOfUseTitle => 'Terms of use';

  @override
  String get termsOfUseSubtitle => 'The rules for using the patient app.';

  @override
  String get legalPlaceholderBanner => 'ASK ME for the real text';

  @override
  String get legalPlaceholderHint =>
      'Placeholder. Replace this screen with the hospital\'s approved wording.';

  @override
  String get privacyPolicyBody =>
      'This is placeholder copy, not the hospital privacy policy.\n\nIt will describe how Smart Ayurveda Hospital uses your prakriti profile, appointments, prescriptions, invoices, and clinical documents inside this app.';

  @override
  String get termsOfUseBody =>
      'This is placeholder copy, not the hospital terms of use.\n\nIt will cover appointment requests, Charaka answers, and your responsibilities when using the patient app.';

  @override
  String get retentionTitle => 'Data and chat retention';

  @override
  String get retentionNote =>
      'Care records and Charaka chat messages are kept only for the period the hospital retention policy will set. That period is not written yet.';

  @override
  String get devGalleryTitle => 'Component gallery';

  @override
  String get devGalleryHint =>
      'Debug only. These samples are not hospital records.';

  @override
  String get devGallerySampleName => 'Sample';

  @override
  String get devGallerySampleUhid => 'UHID';

  @override
  String get devGallerySection => 'Shared components';

  @override
  String get devGalleryOpen => 'Open';

  @override
  String get devGalleryKicker => 'Therapy session';

  @override
  String get devGalleryFact => 'Date';

  @override
  String get devGalleryFactValue => 'From the appointment';

  @override
  String get devGalleryPrice => 'From catalogue';

  @override
  String get devGalleryDuration => 'From catalogue';

  @override
  String get devGalleryCategory => 'Therapy';

  @override
  String get devGalleryKpi => 'Loaded count';

  @override
  String get devGalleryEmpty => 'Nothing here';

  @override
  String get devGalleryError => 'Could not load this sample.';

  @override
  String get devGalleryStepTherapy => 'Therapy';

  @override
  String get devGalleryStepDate => 'Date';

  @override
  String get devGalleryStepTime => 'Time';

  @override
  String get devGalleryStepConfirm => 'Confirm';
}
