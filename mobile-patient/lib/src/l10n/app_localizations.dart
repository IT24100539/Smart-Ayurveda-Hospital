import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_si.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('si'),
  ];

  /// Application name shown on the splash screen and app bars
  ///
  /// In en, this message translates to:
  /// **'Smart Ayurveda'**
  String get appTitle;

  /// Short subtitle under the app name on the splash screen
  ///
  /// In en, this message translates to:
  /// **'Ayurvedic care, guided by your prakriti'**
  String get splashTagline;

  /// Label above the language switcher on the splash screen
  ///
  /// In en, this message translates to:
  /// **'Choose your language'**
  String get chooseLanguage;

  /// Name of the Sinhala language, always written in Sinhala
  ///
  /// In en, this message translates to:
  /// **'සිංහල'**
  String get languageSinhala;

  /// Name of the English language, always written in English
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get languageEnglish;

  /// Button that leaves the splash screen
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get continueLabel;

  /// Title and submit button of the login form
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get signIn;

  /// Title and submit button of the registration form
  ///
  /// In en, this message translates to:
  /// **'Register'**
  String get register;

  /// No description provided for @signInSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Sign in to follow your treatment plan'**
  String get signInSubtitle;

  /// No description provided for @registerSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Create an account to book your first consultation'**
  String get registerSubtitle;

  /// Link that switches the auth form from login to registration
  ///
  /// In en, this message translates to:
  /// **'New patient? Register'**
  String get needAccount;

  /// Link that switches the auth form from registration to login
  ///
  /// In en, this message translates to:
  /// **'Already registered? Sign in'**
  String get haveAccount;

  /// No description provided for @fullNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get fullNameLabel;

  /// No description provided for @emailLabel.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get emailLabel;

  /// No description provided for @phoneNumberLabel.
  ///
  /// In en, this message translates to:
  /// **'Phone number'**
  String get phoneNumberLabel;

  /// No description provided for @dateOfBirthLabel.
  ///
  /// In en, this message translates to:
  /// **'Date of birth'**
  String get dateOfBirthLabel;

  /// No description provided for @dateOfBirthRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your date of birth'**
  String get dateOfBirthRequired;

  /// No description provided for @genderLabel.
  ///
  /// In en, this message translates to:
  /// **'Gender'**
  String get genderLabel;

  /// No description provided for @genderRequired.
  ///
  /// In en, this message translates to:
  /// **'Please select a gender'**
  String get genderRequired;

  /// No description provided for @genderFemale.
  ///
  /// In en, this message translates to:
  /// **'Female'**
  String get genderFemale;

  /// No description provided for @genderMale.
  ///
  /// In en, this message translates to:
  /// **'Male'**
  String get genderMale;

  /// No description provided for @genderOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get genderOther;

  /// No description provided for @genderUnspecified.
  ///
  /// In en, this message translates to:
  /// **'Unspecified'**
  String get genderUnspecified;

  /// No description provided for @passwordLabel.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get passwordLabel;

  /// No description provided for @fullNameRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your full name'**
  String get fullNameRequired;

  /// No description provided for @emailRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your email'**
  String get emailRequired;

  /// No description provided for @emailInvalid.
  ///
  /// In en, this message translates to:
  /// **'Please enter a valid email address'**
  String get emailInvalid;

  /// No description provided for @phoneNumberRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your phone number'**
  String get phoneNumberRequired;

  /// No description provided for @phoneNumberInvalid.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid Sri Lankan mobile number (07XXXXXXXX or +947XXXXXXXX)'**
  String get phoneNumberInvalid;

  /// No description provided for @passwordRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter your password'**
  String get passwordRequired;

  /// Mirrors the backend rule that passwords are 8-128 characters
  ///
  /// In en, this message translates to:
  /// **'Password must be at least {minLength} characters'**
  String passwordTooShort(int minLength);

  /// Mirrors the backend rule that passwords are 8-128 characters
  ///
  /// In en, this message translates to:
  /// **'Password must be at most {maxLength} characters'**
  String passwordTooLong(int maxLength);

  /// Shown when login returns 401
  ///
  /// In en, this message translates to:
  /// **'Incorrect email or password.'**
  String get invalidCredentialsMessage;

  /// Shown when register is rejected without revealing whether the email exists
  ///
  /// In en, this message translates to:
  /// **'Unable to create an account with the details provided.'**
  String get registrationUnavailableMessage;

  /// Shown when login returns 401 because the account is locked
  ///
  /// In en, this message translates to:
  /// **'This account is temporarily locked. Please try again later.'**
  String get accountTemporarilyLocked;

  /// Shown when login, register, or password reset is rate limited
  ///
  /// In en, this message translates to:
  /// **'Too many attempts. Please try again later.'**
  String get tooManyAttempts;

  /// Fallback message when the API returns no readable error detail
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get genericErrorMessage;

  /// No description provided for @networkErrorMessage.
  ///
  /// In en, this message translates to:
  /// **'Cannot reach the hospital server. Check your connection.'**
  String get networkErrorMessage;

  /// No description provided for @sessionExpiredMessage.
  ///
  /// In en, this message translates to:
  /// **'Your session has expired. Please sign in again.'**
  String get sessionExpiredMessage;

  /// No description provided for @navHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get navHome;

  /// No description provided for @navTreatments.
  ///
  /// In en, this message translates to:
  /// **'Treatments'**
  String get navTreatments;

  /// No description provided for @navAppointments.
  ///
  /// In en, this message translates to:
  /// **'Appointments'**
  String get navAppointments;

  /// Bottom navigation label for the patient feedback tab
  ///
  /// In en, this message translates to:
  /// **'Feedback'**
  String get navFeedback;

  /// No description provided for @navProfile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get navProfile;

  /// Greeting on the home tab using the patient's first name
  ///
  /// In en, this message translates to:
  /// **'Ayubowan, {name}'**
  String homeGreeting(String name);

  /// Greeting used when the patient's name is not known yet
  ///
  /// In en, this message translates to:
  /// **'Ayubowan'**
  String get homeGreetingGeneric;

  /// No description provided for @homeSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Book consultations, follow your treatment schedule, and review your registration summary.'**
  String get homeSubtitle;

  /// Hospital name on the home information card
  ///
  /// In en, this message translates to:
  /// **'Smart Ayurveda Hospital'**
  String get homeHospitalName;

  /// No description provided for @homeHospitalAddress.
  ///
  /// In en, this message translates to:
  /// **'Pallekele, Kundasale 20168'**
  String get homeHospitalAddress;

  /// No description provided for @homeHospitalPhone.
  ///
  /// In en, this message translates to:
  /// **'+94 81 242 0541'**
  String get homeHospitalPhone;

  /// No description provided for @homeHospitalHours.
  ///
  /// In en, this message translates to:
  /// **'Mon-Fri 8:00 AM - 5:30 PM'**
  String get homeHospitalHours;

  /// No description provided for @homeLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load hospital information. Try again.'**
  String get homeLoadError;

  /// No description provided for @comingSoonTitle.
  ///
  /// In en, this message translates to:
  /// **'Coming soon'**
  String get comingSoonTitle;

  /// No description provided for @comingSoonBody.
  ///
  /// In en, this message translates to:
  /// **'This part of your care record is still being prepared.'**
  String get comingSoonBody;

  /// No description provided for @treatmentsPlaceholder.
  ///
  /// In en, this message translates to:
  /// **'Your panchakarma and therapy history will appear here.'**
  String get treatmentsPlaceholder;

  /// No description provided for @appointmentsPlaceholder.
  ///
  /// In en, this message translates to:
  /// **'Your nadi pariksha and consultation bookings will appear here.'**
  String get appointmentsPlaceholder;

  /// No description provided for @profilePlaceholder.
  ///
  /// In en, this message translates to:
  /// **'Your prakriti profile and account settings will appear here.'**
  String get profilePlaceholder;

  /// No description provided for @languageSectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get languageSectionTitle;

  /// No description provided for @signOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get signOut;

  /// No description provided for @leaveFeedback.
  ///
  /// In en, this message translates to:
  /// **'Leave feedback'**
  String get leaveFeedback;

  /// No description provided for @submitFeedbackTitle.
  ///
  /// In en, this message translates to:
  /// **'Share your experience'**
  String get submitFeedbackTitle;

  /// No description provided for @ratingLabel.
  ///
  /// In en, this message translates to:
  /// **'How was this visit?'**
  String get ratingLabel;

  /// No description provided for @commentLabel.
  ///
  /// In en, this message translates to:
  /// **'Comment'**
  String get commentLabel;

  /// No description provided for @commentHint.
  ///
  /// In en, this message translates to:
  /// **'What should the care team know about this visit?'**
  String get commentHint;

  /// No description provided for @commentRequired.
  ///
  /// In en, this message translates to:
  /// **'Please share a short comment'**
  String get commentRequired;

  /// No description provided for @ratingRequired.
  ///
  /// In en, this message translates to:
  /// **'Choose a star rating'**
  String get ratingRequired;

  /// No description provided for @anonymousLabel.
  ///
  /// In en, this message translates to:
  /// **'Post anonymously'**
  String get anonymousLabel;

  /// No description provided for @anonymousHelp.
  ///
  /// In en, this message translates to:
  /// **'Your name stays off the public board.'**
  String get anonymousHelp;

  /// No description provided for @postedAsLabel.
  ///
  /// In en, this message translates to:
  /// **'Posted as'**
  String get postedAsLabel;

  /// No description provided for @yourName.
  ///
  /// In en, this message translates to:
  /// **'Your name'**
  String get yourName;

  /// No description provided for @submitFeedback.
  ///
  /// In en, this message translates to:
  /// **'Send feedback'**
  String get submitFeedback;

  /// No description provided for @feedbackSent.
  ///
  /// In en, this message translates to:
  /// **'Thank you. Your note will appear after the care team reviews it.'**
  String get feedbackSent;

  /// No description provided for @linkedVisit.
  ///
  /// In en, this message translates to:
  /// **'Linked to this completed visit'**
  String get linkedVisit;

  /// No description provided for @feedbackNeedsLink.
  ///
  /// In en, this message translates to:
  /// **'Choose a completed visit or a treatment.'**
  String get feedbackNeedsLink;

  /// No description provided for @writeFeedback.
  ///
  /// In en, this message translates to:
  /// **'Write feedback'**
  String get writeFeedback;

  /// No description provided for @hubCommunity.
  ///
  /// In en, this message translates to:
  /// **'Community'**
  String get hubCommunity;

  /// No description provided for @hubMine.
  ///
  /// In en, this message translates to:
  /// **'My feedback'**
  String get hubMine;

  /// No description provided for @hubComplaints.
  ///
  /// In en, this message translates to:
  /// **'Complaints'**
  String get hubComplaints;

  /// No description provided for @hubNotifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get hubNotifications;

  /// No description provided for @linkToVisit.
  ///
  /// In en, this message translates to:
  /// **'Completed visit'**
  String get linkToVisit;

  /// No description provided for @linkToTreatment.
  ///
  /// In en, this message translates to:
  /// **'Treatment'**
  String get linkToTreatment;

  /// No description provided for @chooseTreatment.
  ///
  /// In en, this message translates to:
  /// **'Choose a treatment'**
  String get chooseTreatment;

  /// No description provided for @editTimeRemaining.
  ///
  /// In en, this message translates to:
  /// **'{hours}h {minutes}m left to edit or withdraw'**
  String editTimeRemaining(int hours, int minutes);

  /// No description provided for @feedbackStatusPending.
  ///
  /// In en, this message translates to:
  /// **'Pending review'**
  String get feedbackStatusPending;

  /// No description provided for @feedbackStatusVisible.
  ///
  /// In en, this message translates to:
  /// **'Visible'**
  String get feedbackStatusVisible;

  /// No description provided for @feedbackStatusHidden.
  ///
  /// In en, this message translates to:
  /// **'Hidden'**
  String get feedbackStatusHidden;

  /// No description provided for @feedbackStatusWithdrawn.
  ///
  /// In en, this message translates to:
  /// **'Withdrawn'**
  String get feedbackStatusWithdrawn;

  /// No description provided for @publicFeedTitle.
  ///
  /// In en, this message translates to:
  /// **'Patient feedback'**
  String get publicFeedTitle;

  /// No description provided for @publicFeedEmpty.
  ///
  /// In en, this message translates to:
  /// **'No approved notes yet. They will appear here after review.'**
  String get publicFeedEmpty;

  /// No description provided for @anonymousPatient.
  ///
  /// In en, this message translates to:
  /// **'Anonymous patient'**
  String get anonymousPatient;

  /// No description provided for @helpful.
  ///
  /// In en, this message translates to:
  /// **'Helpful'**
  String get helpful;

  /// No description provided for @notHelpful.
  ///
  /// In en, this message translates to:
  /// **'Not helpful'**
  String get notHelpful;

  /// No description provided for @repliesHeading.
  ///
  /// In en, this message translates to:
  /// **'Replies'**
  String get repliesHeading;

  /// No description provided for @noReplies.
  ///
  /// In en, this message translates to:
  /// **'No replies yet.'**
  String get noReplies;

  /// No description provided for @careTeam.
  ///
  /// In en, this message translates to:
  /// **'Care team'**
  String get careTeam;

  /// No description provided for @patientRole.
  ///
  /// In en, this message translates to:
  /// **'Patient'**
  String get patientRole;

  /// No description provided for @reactionFailed.
  ///
  /// In en, this message translates to:
  /// **'Could not save your reaction.'**
  String get reactionFailed;

  /// No description provided for @complaintsTitle.
  ///
  /// In en, this message translates to:
  /// **'My complaints'**
  String get complaintsTitle;

  /// No description provided for @submitComplaintTitle.
  ///
  /// In en, this message translates to:
  /// **'Raise a concern'**
  String get submitComplaintTitle;

  /// No description provided for @newComplaint.
  ///
  /// In en, this message translates to:
  /// **'New complaint'**
  String get newComplaint;

  /// No description provided for @subjectLabel.
  ///
  /// In en, this message translates to:
  /// **'Subject'**
  String get subjectLabel;

  /// No description provided for @descriptionLabel.
  ///
  /// In en, this message translates to:
  /// **'Description'**
  String get descriptionLabel;

  /// No description provided for @subjectRequired.
  ///
  /// In en, this message translates to:
  /// **'Please enter a subject'**
  String get subjectRequired;

  /// No description provided for @descriptionRequired.
  ///
  /// In en, this message translates to:
  /// **'Please describe what happened'**
  String get descriptionRequired;

  /// No description provided for @priorityLabel.
  ///
  /// In en, this message translates to:
  /// **'Priority'**
  String get priorityLabel;

  /// No description provided for @priorityNormal.
  ///
  /// In en, this message translates to:
  /// **'Normal'**
  String get priorityNormal;

  /// No description provided for @priorityHigh.
  ///
  /// In en, this message translates to:
  /// **'High'**
  String get priorityHigh;

  /// No description provided for @complaintSent.
  ///
  /// In en, this message translates to:
  /// **'We have received your concern.'**
  String get complaintSent;

  /// No description provided for @complaintsEmpty.
  ///
  /// In en, this message translates to:
  /// **'You have not raised a concern yet.'**
  String get complaintsEmpty;

  /// No description provided for @statusOpen.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get statusOpen;

  /// No description provided for @statusInProgress.
  ///
  /// In en, this message translates to:
  /// **'In progress'**
  String get statusInProgress;

  /// No description provided for @statusEscalated.
  ///
  /// In en, this message translates to:
  /// **'Escalated'**
  String get statusEscalated;

  /// No description provided for @statusResolved.
  ///
  /// In en, this message translates to:
  /// **'Resolved'**
  String get statusResolved;

  /// No description provided for @notificationsTitle.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get notificationsTitle;

  /// No description provided for @unreadLabel.
  ///
  /// In en, this message translates to:
  /// **'Unread'**
  String get unreadLabel;

  /// No description provided for @notificationsEmpty.
  ///
  /// In en, this message translates to:
  /// **'You are up to date.'**
  String get notificationsEmpty;

  /// No description provided for @notificationReply.
  ///
  /// In en, this message translates to:
  /// **'Reply'**
  String get notificationReply;

  /// No description provided for @notificationStatus.
  ///
  /// In en, this message translates to:
  /// **'Status update'**
  String get notificationStatus;

  /// No description provided for @notificationEscalated.
  ///
  /// In en, this message translates to:
  /// **'Escalated'**
  String get notificationEscalated;

  /// No description provided for @notificationGeneral.
  ///
  /// In en, this message translates to:
  /// **'Notice'**
  String get notificationGeneral;

  /// No description provided for @notificationAppointmentApproved.
  ///
  /// In en, this message translates to:
  /// **'Appointment approved'**
  String get notificationAppointmentApproved;

  /// No description provided for @notificationAppointmentRejected.
  ///
  /// In en, this message translates to:
  /// **'Appointment not approved'**
  String get notificationAppointmentRejected;

  /// No description provided for @notificationAppointmentRescheduled.
  ///
  /// In en, this message translates to:
  /// **'Appointment rescheduled'**
  String get notificationAppointmentRescheduled;

  /// No description provided for @notificationAppointmentCancelled.
  ///
  /// In en, this message translates to:
  /// **'Appointment cancelled'**
  String get notificationAppointmentCancelled;

  /// No description provided for @notificationPrescriptionIssued.
  ///
  /// In en, this message translates to:
  /// **'Prescription issued'**
  String get notificationPrescriptionIssued;

  /// No description provided for @notificationInvoiceIssued.
  ///
  /// In en, this message translates to:
  /// **'Invoice issued'**
  String get notificationInvoiceIssued;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get retry;

  /// No description provided for @myFeedbackTitle.
  ///
  /// In en, this message translates to:
  /// **'My feedback'**
  String get myFeedbackTitle;

  /// No description provided for @myFeedbackEmpty.
  ///
  /// In en, this message translates to:
  /// **'You have not shared feedback yet.'**
  String get myFeedbackEmpty;

  /// No description provided for @editFeedback.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get editFeedback;

  /// No description provided for @withdrawFeedback.
  ///
  /// In en, this message translates to:
  /// **'Withdraw'**
  String get withdrawFeedback;

  /// No description provided for @withdrawConfirm.
  ///
  /// In en, this message translates to:
  /// **'Withdraw this feedback? It will be hidden from the public board.'**
  String get withdrawConfirm;

  /// No description provided for @feedbackUpdated.
  ///
  /// In en, this message translates to:
  /// **'Your feedback was updated.'**
  String get feedbackUpdated;

  /// No description provided for @feedbackWithdrawn.
  ///
  /// In en, this message translates to:
  /// **'Your feedback was withdrawn.'**
  String get feedbackWithdrawn;

  /// No description provided for @canStillEdit.
  ///
  /// In en, this message translates to:
  /// **'You can edit this for 24 hours after it was sent.'**
  String get canStillEdit;

  /// No description provided for @editingClosed.
  ///
  /// In en, this message translates to:
  /// **'The 24-hour editing window has closed.'**
  String get editingClosed;

  /// No description provided for @replyHint.
  ///
  /// In en, this message translates to:
  /// **'Reply to this note'**
  String get replyHint;

  /// No description provided for @sendReply.
  ///
  /// In en, this message translates to:
  /// **'Send reply'**
  String get sendReply;

  /// No description provided for @replySent.
  ///
  /// In en, this message translates to:
  /// **'Your reply was posted.'**
  String get replySent;

  /// No description provided for @markAllRead.
  ///
  /// In en, this message translates to:
  /// **'Mark all read'**
  String get markAllRead;

  /// No description provided for @chooseCompletedVisit.
  ///
  /// In en, this message translates to:
  /// **'Completed visit'**
  String get chooseCompletedVisit;

  /// No description provided for @noCompletedVisit.
  ///
  /// In en, this message translates to:
  /// **'No completed visit yet. You can still write about a treatment.'**
  String get noCompletedVisit;

  /// No description provided for @escalatedOn.
  ///
  /// In en, this message translates to:
  /// **'Escalated'**
  String get escalatedOn;

  /// No description provided for @saveChanges.
  ///
  /// In en, this message translates to:
  /// **'Save changes'**
  String get saveChanges;

  /// No description provided for @myHealthHubTitle.
  ///
  /// In en, this message translates to:
  /// **'My Health Hub'**
  String get myHealthHubTitle;

  /// No description provided for @myHealthHubSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Therapy sessions, prescriptions, invoices, and documents.'**
  String get myHealthHubSubtitle;

  /// No description provided for @myTherapySessionsTitle.
  ///
  /// In en, this message translates to:
  /// **'My therapy sessions'**
  String get myTherapySessionsTitle;

  /// No description provided for @noTherapySessionsFound.
  ///
  /// In en, this message translates to:
  /// **'No therapy sessions recorded yet.'**
  String get noTherapySessionsFound;

  /// No description provided for @nextSessionLabel.
  ///
  /// In en, this message translates to:
  /// **'Next Session'**
  String get nextSessionLabel;

  /// No description provided for @myRegistrationSummaryTitle.
  ///
  /// In en, this message translates to:
  /// **'My registration summary'**
  String get myRegistrationSummaryTitle;

  /// No description provided for @uhidLabel.
  ///
  /// In en, this message translates to:
  /// **'UHID'**
  String get uhidLabel;

  /// No description provided for @prakritiLabel.
  ///
  /// In en, this message translates to:
  /// **'Prakriti'**
  String get prakritiLabel;

  /// No description provided for @vikritiLabel.
  ///
  /// In en, this message translates to:
  /// **'Vikriti'**
  String get vikritiLabel;

  /// No description provided for @allergiesLabel.
  ///
  /// In en, this message translates to:
  /// **'Allergies'**
  String get allergiesLabel;

  /// No description provided for @bloodGroupLabel.
  ///
  /// In en, this message translates to:
  /// **'Blood Group'**
  String get bloodGroupLabel;

  /// No description provided for @notRecorded.
  ///
  /// In en, this message translates to:
  /// **'Not recorded'**
  String get notRecorded;

  /// No description provided for @healthHubError.
  ///
  /// In en, this message translates to:
  /// **'Could not load health records.'**
  String get healthHubError;

  /// No description provided for @sessionsCount.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =1{1 session} other{{count} sessions}}'**
  String sessionsCount(int count);

  /// No description provided for @noPatientRecordLinked.
  ///
  /// In en, this message translates to:
  /// **'No patient record is linked to this login.'**
  String get noPatientRecordLinked;

  /// No description provided for @noPatientRecordLinkedHelp.
  ///
  /// In en, this message translates to:
  /// **'The clinical record must use the same email address. Please contact reception or update your profile to link your clinical record.'**
  String get noPatientRecordLinkedHelp;

  /// No description provided for @unlinkedRecordRetry.
  ///
  /// In en, this message translates to:
  /// **'Check again'**
  String get unlinkedRecordRetry;

  /// No description provided for @unlinkedRecordHelpAction.
  ///
  /// In en, this message translates to:
  /// **'Contact reception'**
  String get unlinkedRecordHelpAction;

  /// Link on the login form that opens the forgot-password screen
  ///
  /// In en, this message translates to:
  /// **'Forgot password?'**
  String get forgotPasswordLink;

  /// App bar title of the forgot-password screen
  ///
  /// In en, this message translates to:
  /// **'Reset password'**
  String get forgotPasswordTitle;

  /// No description provided for @forgotPasswordHeading.
  ///
  /// In en, this message translates to:
  /// **'Forgot your password?'**
  String get forgotPasswordHeading;

  /// Explains the request-reset step without revealing whether the email exists
  ///
  /// In en, this message translates to:
  /// **'Enter the email on your account. If it is registered, we will send a reset link.'**
  String get forgotPasswordBody;

  /// No description provided for @forgotPasswordSubmit.
  ///
  /// In en, this message translates to:
  /// **'Send reset link'**
  String get forgotPasswordSubmit;

  /// No description provided for @forgotPasswordBackToSignIn.
  ///
  /// In en, this message translates to:
  /// **'Back to sign in'**
  String get forgotPasswordBackToSignIn;

  /// Same wording whether or not the email is registered, matching the API
  ///
  /// In en, this message translates to:
  /// **'If an account with that email exists, a password reset link has been sent.'**
  String get forgotPasswordSuccess;

  /// No description provided for @forgotPasswordError.
  ///
  /// In en, this message translates to:
  /// **'Could not send a reset request. Check your connection and try again.'**
  String get forgotPasswordError;

  /// App bar title of the complete-reset screen
  ///
  /// In en, this message translates to:
  /// **'New password'**
  String get resetPasswordTitle;

  /// No description provided for @resetPasswordHeading.
  ///
  /// In en, this message translates to:
  /// **'Create a new password'**
  String get resetPasswordHeading;

  /// No description provided for @resetPasswordBody.
  ///
  /// In en, this message translates to:
  /// **'Enter the reset token from your email and choose a new password.'**
  String get resetPasswordBody;

  /// No description provided for @resetTokenLabel.
  ///
  /// In en, this message translates to:
  /// **'Reset token'**
  String get resetTokenLabel;

  /// No description provided for @resetTokenRequired.
  ///
  /// In en, this message translates to:
  /// **'Enter the reset token from your email'**
  String get resetTokenRequired;

  /// No description provided for @newPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'New password'**
  String get newPasswordLabel;

  /// No description provided for @confirmPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Confirm password'**
  String get confirmPasswordLabel;

  /// No description provided for @confirmPasswordRequired.
  ///
  /// In en, this message translates to:
  /// **'Please confirm your password'**
  String get confirmPasswordRequired;

  /// No description provided for @passwordsDoNotMatch.
  ///
  /// In en, this message translates to:
  /// **'Passwords do not match'**
  String get passwordsDoNotMatch;

  /// No description provided for @resetPasswordSubmit.
  ///
  /// In en, this message translates to:
  /// **'Reset password'**
  String get resetPasswordSubmit;

  /// No description provided for @resetPasswordSuccess.
  ///
  /// In en, this message translates to:
  /// **'Your password has been reset. You can now sign in with the new password.'**
  String get resetPasswordSuccess;

  /// No description provided for @resetPasswordError.
  ///
  /// In en, this message translates to:
  /// **'Could not reset the password. The token may be invalid or expired.'**
  String get resetPasswordError;

  /// No description provided for @resetPasswordSignInNow.
  ///
  /// In en, this message translates to:
  /// **'Sign in now'**
  String get resetPasswordSignInNow;

  /// Mirrors the Hospital.Api password policy
  ///
  /// In en, this message translates to:
  /// **'Password must contain at least one uppercase letter'**
  String get passwordNeedsUppercase;

  /// Mirrors the Hospital.Api password policy
  ///
  /// In en, this message translates to:
  /// **'Password must contain at least one lowercase letter'**
  String get passwordNeedsLowercase;

  /// Mirrors the Hospital.Api password policy
  ///
  /// In en, this message translates to:
  /// **'Password must contain at least one digit'**
  String get passwordNeedsDigit;

  /// Mirrors the Hospital.Api password policy
  ///
  /// In en, this message translates to:
  /// **'Password must contain at least one special character'**
  String get passwordNeedsSpecial;

  /// Semantics label for the reveal-password icon button
  ///
  /// In en, this message translates to:
  /// **'Show password'**
  String get showPassword;

  /// Semantics label for the hide-password icon button
  ///
  /// In en, this message translates to:
  /// **'Hide password'**
  String get hidePassword;

  /// Title of the physician directory and its navigation entry
  ///
  /// In en, this message translates to:
  /// **'Physicians'**
  String get doctorsTitle;

  /// Intro on the physician directory
  ///
  /// In en, this message translates to:
  /// **'Vaidyas who consult at the hospital.'**
  String get doctorsSubtitle;

  /// Subtitle on the home and profile links to the physician directory
  ///
  /// In en, this message translates to:
  /// **'Meet the vaidya team'**
  String get doctorsNavSubtitle;

  /// No description provided for @doctorsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search by name or specialty'**
  String get doctorsSearchHint;

  /// No description provided for @doctorsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No physicians are listed yet.'**
  String get doctorsEmpty;

  /// No description provided for @doctorsSearchEmpty.
  ///
  /// In en, this message translates to:
  /// **'No physicians match your search.'**
  String get doctorsSearchEmpty;

  /// No description provided for @doctorsLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load physicians.'**
  String get doctorsLoadError;

  /// No description provided for @doctorProfileLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load this physician.'**
  String get doctorProfileLoadError;

  /// No description provided for @doctorQualificationsLabel.
  ///
  /// In en, this message translates to:
  /// **'Qualifications'**
  String get doctorQualificationsLabel;

  /// No description provided for @doctorAboutLabel.
  ///
  /// In en, this message translates to:
  /// **'About'**
  String get doctorAboutLabel;

  /// No description provided for @healthHubUpcomingTab.
  ///
  /// In en, this message translates to:
  /// **'Upcoming'**
  String get healthHubUpcomingTab;

  /// No description provided for @healthHubTherapyTab.
  ///
  /// In en, this message translates to:
  /// **'Therapy'**
  String get healthHubTherapyTab;

  /// No description provided for @healthHubRegistrationTab.
  ///
  /// In en, this message translates to:
  /// **'Registration'**
  String get healthHubRegistrationTab;

  /// No description provided for @healthHubUpcomingEmpty.
  ///
  /// In en, this message translates to:
  /// **'No upcoming appointments.'**
  String get healthHubUpcomingEmpty;

  /// No description provided for @healthHubPrescriptionsTab.
  ///
  /// In en, this message translates to:
  /// **'Prescriptions'**
  String get healthHubPrescriptionsTab;

  /// No description provided for @healthHubInvoicesTab.
  ///
  /// In en, this message translates to:
  /// **'Invoices'**
  String get healthHubInvoicesTab;

  /// No description provided for @healthHubDocumentsTab.
  ///
  /// In en, this message translates to:
  /// **'Documents'**
  String get healthHubDocumentsTab;

  /// No description provided for @myPrescriptionsTitle.
  ///
  /// In en, this message translates to:
  /// **'My prescriptions'**
  String get myPrescriptionsTitle;

  /// No description provided for @prescriptionsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No prescriptions have been issued yet.'**
  String get prescriptionsEmpty;

  /// No description provided for @prescriptionsLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load prescriptions.'**
  String get prescriptionsLoadError;

  /// No description provided for @prescriptionIssuedOn.
  ///
  /// In en, this message translates to:
  /// **'Issued {date}'**
  String prescriptionIssuedOn(String date);

  /// No description provided for @prescriptionRevision.
  ///
  /// In en, this message translates to:
  /// **'Revision {number}'**
  String prescriptionRevision(int number);

  /// No description provided for @prescriptionStatusIssued.
  ///
  /// In en, this message translates to:
  /// **'Issued'**
  String get prescriptionStatusIssued;

  /// No description provided for @prescriptionStatusSuperseded.
  ///
  /// In en, this message translates to:
  /// **'Superseded'**
  String get prescriptionStatusSuperseded;

  /// No description provided for @prescriptionStatusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get prescriptionStatusCancelled;

  /// No description provided for @prescriptionStatusDraft.
  ///
  /// In en, this message translates to:
  /// **'Draft'**
  String get prescriptionStatusDraft;

  /// No description provided for @myInvoicesTitle.
  ///
  /// In en, this message translates to:
  /// **'My invoices'**
  String get myInvoicesTitle;

  /// No description provided for @invoicesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No invoices have been issued yet.'**
  String get invoicesEmpty;

  /// No description provided for @invoicesLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load invoices.'**
  String get invoicesLoadError;

  /// No description provided for @paymentHistoryTitle.
  ///
  /// In en, this message translates to:
  /// **'Payment history'**
  String get paymentHistoryTitle;

  /// No description provided for @paymentsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No payments recorded.'**
  String get paymentsEmpty;

  /// No description provided for @invoiceTotalLabel.
  ///
  /// In en, this message translates to:
  /// **'Total'**
  String get invoiceTotalLabel;

  /// No description provided for @amountPaidLabel.
  ///
  /// In en, this message translates to:
  /// **'Paid'**
  String get amountPaidLabel;

  /// No description provided for @balanceLabel.
  ///
  /// In en, this message translates to:
  /// **'Balance'**
  String get balanceLabel;

  /// No description provided for @invoiceStatusIssued.
  ///
  /// In en, this message translates to:
  /// **'Issued'**
  String get invoiceStatusIssued;

  /// No description provided for @invoiceStatusPaid.
  ///
  /// In en, this message translates to:
  /// **'Paid'**
  String get invoiceStatusPaid;

  /// No description provided for @invoiceStatusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get invoiceStatusCancelled;

  /// No description provided for @invoiceStatusDraft.
  ///
  /// In en, this message translates to:
  /// **'Draft'**
  String get invoiceStatusDraft;

  /// No description provided for @paymentMethodCash.
  ///
  /// In en, this message translates to:
  /// **'Cash'**
  String get paymentMethodCash;

  /// No description provided for @paymentMethodCard.
  ///
  /// In en, this message translates to:
  /// **'Card'**
  String get paymentMethodCard;

  /// No description provided for @paymentMethodBankTransfer.
  ///
  /// In en, this message translates to:
  /// **'Bank transfer'**
  String get paymentMethodBankTransfer;

  /// No description provided for @myDocumentsTitle.
  ///
  /// In en, this message translates to:
  /// **'My documents'**
  String get myDocumentsTitle;

  /// No description provided for @documentsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No documents have been uploaded yet.'**
  String get documentsEmpty;

  /// No description provided for @documentsLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load documents.'**
  String get documentsLoadError;

  /// No description provided for @documentView.
  ///
  /// In en, this message translates to:
  /// **'View'**
  String get documentView;

  /// No description provided for @documentDownload.
  ///
  /// In en, this message translates to:
  /// **'Download'**
  String get documentDownload;

  /// No description provided for @documentSaved.
  ///
  /// In en, this message translates to:
  /// **'Saved {fileName}'**
  String documentSaved(String fileName);

  /// No description provided for @documentDownloadError.
  ///
  /// In en, this message translates to:
  /// **'Could not download this document.'**
  String get documentDownloadError;

  /// No description provided for @documentOpenError.
  ///
  /// In en, this message translates to:
  /// **'Could not open this document.'**
  String get documentOpenError;

  /// No description provided for @documentPdfNotice.
  ///
  /// In en, this message translates to:
  /// **'This PDF is available to download.'**
  String get documentPdfNotice;

  /// No description provided for @documentFileNotice.
  ///
  /// In en, this message translates to:
  /// **'This file is available to download.'**
  String get documentFileNotice;

  /// No description provided for @documentCategoryLabReport.
  ///
  /// In en, this message translates to:
  /// **'Lab report'**
  String get documentCategoryLabReport;

  /// No description provided for @documentCategoryPrescriptionScan.
  ///
  /// In en, this message translates to:
  /// **'Prescription scan'**
  String get documentCategoryPrescriptionScan;

  /// No description provided for @documentCategoryDiagnosticScan.
  ///
  /// In en, this message translates to:
  /// **'Diagnostic scan'**
  String get documentCategoryDiagnosticScan;

  /// No description provided for @documentCategoryDischargeSummary.
  ///
  /// In en, this message translates to:
  /// **'Discharge summary'**
  String get documentCategoryDischargeSummary;

  /// No description provided for @documentCategoryTreatmentPlan.
  ///
  /// In en, this message translates to:
  /// **'Treatment plan'**
  String get documentCategoryTreatmentPlan;

  /// No description provided for @documentCategoryGeneral.
  ///
  /// In en, this message translates to:
  /// **'General'**
  String get documentCategoryGeneral;

  /// Shown only when the doctors API returns a real feedback average
  ///
  /// In en, this message translates to:
  /// **'{rating} from {count, plural, =1{1 review} other{{count} reviews}}'**
  String doctorRatingSummary(String rating, int count);

  /// No description provided for @appearanceSectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Appearance'**
  String get appearanceSectionTitle;

  /// Theme follows the device setting
  ///
  /// In en, this message translates to:
  /// **'System'**
  String get themeSystem;

  /// No description provided for @themeLight.
  ///
  /// In en, this message translates to:
  /// **'Light'**
  String get themeLight;

  /// No description provided for @themeDark.
  ///
  /// In en, this message translates to:
  /// **'Dark'**
  String get themeDark;

  /// No description provided for @privacySectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Privacy'**
  String get privacySectionTitle;

  /// No description provided for @privacyPolicyTitle.
  ///
  /// In en, this message translates to:
  /// **'Privacy policy'**
  String get privacyPolicyTitle;

  /// No description provided for @privacyPolicySubtitle.
  ///
  /// In en, this message translates to:
  /// **'How your care record is handled in this app.'**
  String get privacyPolicySubtitle;

  /// No description provided for @termsOfUseTitle.
  ///
  /// In en, this message translates to:
  /// **'Terms of use'**
  String get termsOfUseTitle;

  /// No description provided for @termsOfUseSubtitle.
  ///
  /// In en, this message translates to:
  /// **'The rules for using the patient app.'**
  String get termsOfUseSubtitle;

  /// Visible marker until the hospital supplies the approved legal copy
  ///
  /// In en, this message translates to:
  /// **'ASK ME for the real text'**
  String get legalPlaceholderBanner;

  /// No description provided for @legalPlaceholderHint.
  ///
  /// In en, this message translates to:
  /// **'Placeholder. Replace this screen with the hospital\'s approved wording.'**
  String get legalPlaceholderHint;

  /// No description provided for @privacyPolicyBody.
  ///
  /// In en, this message translates to:
  /// **'This is placeholder copy, not the hospital privacy policy.\n\nIt will describe how Smart Ayurveda Hospital uses your prakriti profile, appointments, prescriptions, invoices, and clinical documents inside this app.'**
  String get privacyPolicyBody;

  /// No description provided for @termsOfUseBody.
  ///
  /// In en, this message translates to:
  /// **'This is placeholder copy, not the hospital terms of use.\n\nIt will cover appointment requests, Charaka answers, and your responsibilities when using the patient app.'**
  String get termsOfUseBody;

  /// No description provided for @retentionTitle.
  ///
  /// In en, this message translates to:
  /// **'Data and chat retention'**
  String get retentionTitle;

  /// Placeholder until the hospital confirms how long records and Charaka chats are kept
  ///
  /// In en, this message translates to:
  /// **'Care records and Charaka chat messages are kept only for the period the hospital retention policy will set. That period is not written yet.'**
  String get retentionNote;

  /// Debug-only design system screen
  ///
  /// In en, this message translates to:
  /// **'Component gallery'**
  String get devGalleryTitle;

  /// No description provided for @devGalleryHint.
  ///
  /// In en, this message translates to:
  /// **'Debug only. These samples are not hospital records.'**
  String get devGalleryHint;

  /// No description provided for @devGallerySampleName.
  ///
  /// In en, this message translates to:
  /// **'Sample'**
  String get devGallerySampleName;

  /// No description provided for @devGallerySampleUhid.
  ///
  /// In en, this message translates to:
  /// **'UHID'**
  String get devGallerySampleUhid;

  /// No description provided for @devGallerySection.
  ///
  /// In en, this message translates to:
  /// **'Shared components'**
  String get devGallerySection;

  /// No description provided for @devGalleryOpen.
  ///
  /// In en, this message translates to:
  /// **'Open'**
  String get devGalleryOpen;

  /// No description provided for @devGalleryKicker.
  ///
  /// In en, this message translates to:
  /// **'Therapy session'**
  String get devGalleryKicker;

  /// No description provided for @devGalleryFact.
  ///
  /// In en, this message translates to:
  /// **'Date'**
  String get devGalleryFact;

  /// No description provided for @devGalleryFactValue.
  ///
  /// In en, this message translates to:
  /// **'From the appointment'**
  String get devGalleryFactValue;

  /// No description provided for @devGalleryPrice.
  ///
  /// In en, this message translates to:
  /// **'From catalogue'**
  String get devGalleryPrice;

  /// No description provided for @devGalleryDuration.
  ///
  /// In en, this message translates to:
  /// **'From catalogue'**
  String get devGalleryDuration;

  /// No description provided for @devGalleryCategory.
  ///
  /// In en, this message translates to:
  /// **'Therapy'**
  String get devGalleryCategory;

  /// No description provided for @devGalleryKpi.
  ///
  /// In en, this message translates to:
  /// **'Loaded count'**
  String get devGalleryKpi;

  /// No description provided for @devGalleryEmpty.
  ///
  /// In en, this message translates to:
  /// **'Nothing here'**
  String get devGalleryEmpty;

  /// No description provided for @devGalleryError.
  ///
  /// In en, this message translates to:
  /// **'Could not load this sample.'**
  String get devGalleryError;

  /// No description provided for @devGalleryStepTherapy.
  ///
  /// In en, this message translates to:
  /// **'Therapy'**
  String get devGalleryStepTherapy;

  /// No description provided for @devGalleryStepDate.
  ///
  /// In en, this message translates to:
  /// **'Date'**
  String get devGalleryStepDate;

  /// No description provided for @devGalleryStepTime.
  ///
  /// In en, this message translates to:
  /// **'Time'**
  String get devGalleryStepTime;

  /// No description provided for @devGalleryStepConfirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm'**
  String get devGalleryStepConfirm;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'si'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'si':
      return AppLocalizationsSi();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
