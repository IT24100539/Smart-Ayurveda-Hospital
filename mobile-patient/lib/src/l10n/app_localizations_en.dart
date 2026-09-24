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
  String get navBilling => 'Invoices';

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
  String get billingPlaceholder =>
      'Your invoices and treatment feedback will appear here.';

  @override
  String get profilePlaceholder =>
      'Your prakriti profile and account settings will appear here.';

  @override
  String get languageSectionTitle => 'Language';

  @override
  String get signOut => 'Sign out';
}
