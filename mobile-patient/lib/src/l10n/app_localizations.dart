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

  /// Shown when register returns 409
  ///
  /// In en, this message translates to:
  /// **'An account already exists for this email.'**
  String get emailAlreadyRegisteredMessage;

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
  /// **'Book consultations, follow your panchakarma plan, and track your herbal medicines.'**
  String get homeSubtitle;

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
  /// **'Open this from a completed appointment so the visit can be attached.'**
  String get feedbackNeedsLink;

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
  /// **'No completed visit is available for feedback yet.'**
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
