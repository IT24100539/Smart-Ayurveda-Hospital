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
  String get emailAlreadyRegisteredMessage =>
      'An account already exists for this email.';

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
      'Book consultations, follow your panchakarma plan, and track your herbal medicines.';

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
  String get feedbackNeedsLink =>
      'Open this from a completed appointment so the visit can be attached.';

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
      'No completed visit is available for feedback yet.';

  @override
  String get escalatedOn => 'Escalated';

  @override
  String get saveChanges => 'Save changes';
}
