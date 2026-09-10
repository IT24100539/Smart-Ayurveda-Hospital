// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Sinhala Sinhalese (`si`).
class AppLocalizationsSi extends AppLocalizations {
  AppLocalizationsSi([String locale = 'si']) : super(locale);

  @override
  String get appTitle => 'ස්මාර්ට් ආයුර්වේද';

  @override
  String get splashTagline => 'ඔබේ ප්‍රකෘතියට අනුව ආයුර්වේද ප්‍රතිකාර';

  @override
  String get chooseLanguage => 'ඔබේ භාෂාව තෝරන්න';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get languageEnglish => 'English';

  @override
  String get continueLabel => 'ඉදිරියට';

  @override
  String get signIn => 'පිවිසෙන්න';

  @override
  String get register => 'ලියාපදිංචි වන්න';

  @override
  String get signInSubtitle => 'ඔබේ ප්‍රතිකාර සැලසුම බැලීමට පිවිසෙන්න';

  @override
  String get registerSubtitle => 'පළමු උපදේශනය වෙන් කරවා ගැනීමට ගිණුමක් සාදන්න';

  @override
  String get needAccount => 'නව රෝගියෙක්ද? ලියාපදිංචි වන්න';

  @override
  String get haveAccount => 'දැනටමත් ලියාපදිංචිද? පිවිසෙන්න';

  @override
  String get fullNameLabel => 'සම්පූර්ණ නම';

  @override
  String get emailLabel => 'විද්‍යුත් තැපෑල';

  @override
  String get phoneNumberLabel => 'දුරකථන අංකය';

  @override
  String get passwordLabel => 'රහස් පදය';

  @override
  String get fullNameRequired => 'ඔබේ සම්පූර්ණ නම ඇතුළත් කරන්න';

  @override
  String get emailRequired => 'ඔබේ විද්‍යුත් තැපැල් ලිපිනය ඇතුළත් කරන්න';

  @override
  String get emailInvalid => 'වලංගු විද්‍යුත් තැපැල් ලිපිනයක් ඇතුළත් කරන්න';

  @override
  String get phoneNumberRequired => 'ඔබේ දුරකථන අංකය ඇතුළත් කරන්න';

  @override
  String get passwordRequired => 'ඔබේ රහස් පදය ඇතුළත් කරන්න';

  @override
  String passwordTooShort(int minLength) {
    return 'රහස් පදය අවම වශයෙන් අක්ෂර $minLengthක් විය යුතුය';
  }

  @override
  String passwordTooLong(int maxLength) {
    return 'රහස් පදය උපරිම වශයෙන් අක්ෂර $maxLengthක් විය යුතුය';
  }

  @override
  String get invalidCredentialsMessage => 'විද්‍යුත් තැපෑල හෝ රහස් පදය වැරදිය.';

  @override
  String get emailAlreadyRegisteredMessage =>
      'මෙම විද්‍යුත් තැපෑල සඳහා ගිණුමක් දැනටමත් තිබේ.';

  @override
  String get genericErrorMessage => 'දෝෂයක් සිදු විය. නැවත උත්සාහ කරන්න.';

  @override
  String get networkErrorMessage =>
      'රෝහල් සේවාදායකයට සම්බන්ධ විය නොහැක. ඔබේ සම්බන්ධතාවය පරීක්ෂා කරන්න.';

  @override
  String get sessionExpiredMessage =>
      'ඔබේ සැසිය කල් ඉකුත් වී ඇත. නැවත පිවිසෙන්න.';

  @override
  String get navHome => 'මුල් පිටුව';

  @override
  String get navTreatments => 'ප්‍රතිකාර';

  @override
  String get navAppointments => 'වෙන්කිරීම්';

  @override
  String get navBilling => 'බිල්පත්';

  @override
  String get navProfile => 'පැතිකඩ';

  @override
  String homeGreeting(String name) {
    return 'ආයුබෝවන්, $name';
  }

  @override
  String get homeGreetingGeneric => 'ආයුබෝවන්';

  @override
  String get homeSubtitle =>
      'උපදේශන වෙන් කරවා ගන්න, ඔබේ පංචකර්ම සැලසුම අනුගමනය කරන්න, සහ ඔබේ ඔසු පිළිබඳ තොරතුරු බලන්න.';

  @override
  String get comingSoonTitle => 'ඉක්මනින් පැමිණේ';

  @override
  String get comingSoonBody =>
      'ඔබේ ප්‍රතිකාර වාර්තාවේ මෙම කොටස තවමත් සකස් වෙමින් පවතී.';

  @override
  String get treatmentsPlaceholder =>
      'ඔබේ පංචකර්ම සහ ප්‍රතිකාර ඉතිහාසය මෙහි දැක්වේ.';

  @override
  String get appointmentsPlaceholder =>
      'ඔබේ නාඩි පරීක්ෂා සහ උපදේශන වෙන්කිරීම් මෙහි දැක්වේ.';

  @override
  String get billingPlaceholder =>
      'ඔබේ බිල්පත් සහ ප්‍රතිකාර පිළිබඳ ප්‍රතිචාර මෙහි දැක්වේ.';

  @override
  String get profilePlaceholder =>
      'ඔබේ ප්‍රකෘති තොරතුරු සහ ගිණුම් සැකසුම් මෙහි දැක්වේ.';

  @override
  String get languageSectionTitle => 'භාෂාව';

  @override
  String get signOut => 'ඉවත් වන්න';
}
