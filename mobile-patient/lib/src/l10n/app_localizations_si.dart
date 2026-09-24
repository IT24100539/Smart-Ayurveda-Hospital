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
  String get phoneNumberInvalid =>
      'වලංගු ශ්‍රී ලාංකික ජංගම දුරකථන අංකයක් ඇතුළත් කරන්න (07XXXXXXXX හෝ +947XXXXXXXX)';

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
  String get navFeedback => 'ප්‍රතිචාර';

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
  String get profilePlaceholder =>
      'ඔබේ ප්‍රකෘති තොරතුරු සහ ගිණුම් සැකසුම් මෙහි දැක්වේ.';

  @override
  String get languageSectionTitle => 'භාෂාව';

  @override
  String get signOut => 'ඉවත් වන්න';

  @override
  String get leaveFeedback => 'ප්‍රතිචාරයක් දෙන්න';

  @override
  String get submitFeedbackTitle => 'ඔබේ අත්දැකීම';

  @override
  String get ratingLabel => 'මෙම පැමිණීම කෙසේද?';

  @override
  String get commentLabel => 'අදහස';

  @override
  String get commentHint =>
      'ප්‍රතිකාර කණ්ඩායම මෙම පැමිණීම ගැන දැනගත යුත්තේ කුමක්ද?';

  @override
  String get commentRequired => 'කරුණාකර කෙටි අදහසක් ලියන්න';

  @override
  String get ratingRequired => 'තරු ශ්‍රේණියක් තෝරන්න';

  @override
  String get anonymousLabel => 'නිර්නාමිකව පළ කරන්න';

  @override
  String get anonymousHelp => 'ඔබේ නම පොදු පුවරුවේ නොපෙන්වයි.';

  @override
  String get postedAsLabel => 'පළ වන නම';

  @override
  String get yourName => 'ඔබේ නම';

  @override
  String get submitFeedback => 'ප්‍රතිචාරය යවන්න';

  @override
  String get feedbackSent =>
      'ස්තූතියි. ඔබේ සටහන ප්‍රතිකාර කණ්ඩායම සමාලෝචනය කිරීමෙන් පසු පෙනෙනු ඇත.';

  @override
  String get linkedVisit => 'මෙම සම්පූර්ණ වූ පැමිණීමට සම්බන්ධයි';

  @override
  String get feedbackNeedsLink =>
      'පැමිණීම සම්බන්ධ කිරීමට සම්පූර්ණ වූ වෙන්කිරීමකින් මෙය විවෘත කරන්න.';

  @override
  String get publicFeedTitle => 'රෝගී ප්‍රතිචාර';

  @override
  String get publicFeedEmpty =>
      'තවම අනුමත සටහන් නැත. සමාලෝචනයෙන් පසු ඒවා මෙහි දිස් වේ.';

  @override
  String get anonymousPatient => 'නිර්නාමික රෝගියා';

  @override
  String get helpful => 'ප්‍රයෝජනවත්';

  @override
  String get notHelpful => 'ප්‍රයෝජනවත් නැත';

  @override
  String get repliesHeading => 'පිළිතුරු';

  @override
  String get noReplies => 'තවම පිළිතුරු නැත.';

  @override
  String get careTeam => 'ප්‍රතිකාර කණ්ඩායම';

  @override
  String get patientRole => 'රෝගියා';

  @override
  String get reactionFailed => 'ඔබේ ප්‍රතිචාරය සුරැකිය නොහැකි විය.';

  @override
  String get complaintsTitle => 'මගේ පැමිණිලි';

  @override
  String get submitComplaintTitle => 'ගැටලුවක් දන්වන්න';

  @override
  String get newComplaint => 'නව පැමිණිල්ල';

  @override
  String get subjectLabel => 'මාතෘකාව';

  @override
  String get descriptionLabel => 'විස්තරය';

  @override
  String get subjectRequired => 'මාතෘකාවක් ඇතුළත් කරන්න';

  @override
  String get descriptionRequired => 'සිදු වූ දේ විස්තර කරන්න';

  @override
  String get priorityLabel => 'ප්‍රමුඛතාව';

  @override
  String get priorityNormal => 'සාමාන්‍ය';

  @override
  String get priorityHigh => 'ඉහළ';

  @override
  String get complaintSent => 'ඔබේ ගැටලුව ලැබුණි.';

  @override
  String get complaintsEmpty => 'ඔබ තවම ගැටලුවක් දන්වා නැත.';

  @override
  String get statusOpen => 'විවෘතයි';

  @override
  String get statusInProgress => 'සැකසෙමින්';

  @override
  String get statusEscalated => 'ඉහළ නංවා ඇත';

  @override
  String get statusResolved => 'විසඳා ඇත';

  @override
  String get notificationsTitle => 'දැනුම්දීම්';

  @override
  String get unreadLabel => 'නොකියවූ';

  @override
  String get notificationsEmpty => 'ඔබ යාවත්කාලීනයි.';

  @override
  String get notificationReply => 'පිළිතුර';

  @override
  String get notificationStatus => 'තත්ත්ව යාවත්කාලීනය';

  @override
  String get notificationEscalated => 'ඉහළ නැංවීම';

  @override
  String get notificationGeneral => 'දැනුම්දීම';

  @override
  String get retry => 'නැවත උත්සාහ කරන්න';

  @override
  String get myFeedbackTitle => 'මගේ ප්‍රතිචාර';

  @override
  String get myFeedbackEmpty => 'ඔබ තවම ප්‍රතිචාරයක් බෙදා නැත.';

  @override
  String get editFeedback => 'සංස්කරණය';

  @override
  String get withdrawFeedback => 'ඉවත් කරන්න';

  @override
  String get withdrawConfirm =>
      'මෙම ප්‍රතිචාරය ඉවත් කරන්නද? එය පොදු පුවරුවෙන් සඟවනු ලැබේ.';

  @override
  String get feedbackUpdated => 'ඔබේ ප්‍රතිචාරය යාවත්කාලීන විය.';

  @override
  String get feedbackWithdrawn => 'ඔබේ ප්‍රතිචාරය ඉවත් කරන ලදි.';

  @override
  String get canStillEdit => 'යැවූ පසු පැය 24ක් ඇතුළත සංස්කරණය කළ හැක.';

  @override
  String get editingClosed => 'පැය 24ක සංස්කරණ කාලය අවසන්.';

  @override
  String get replyHint => 'මෙම සටහනට පිළිතුරු දෙන්න';

  @override
  String get sendReply => 'පිළිතුර යවන්න';

  @override
  String get replySent => 'ඔබේ පිළිතුර පළ විය.';

  @override
  String get markAllRead => 'සියල්ල කියවූ ලෙස සලකුණු කරන්න';

  @override
  String get chooseCompletedVisit => 'සම්පූර්ණ වූ පැමිණීම';

  @override
  String get noCompletedVisit =>
      'ප්‍රතිචාරය සඳහා සම්පූර්ණ වූ පැමිණීමක් තවම නැත.';

  @override
  String get escalatedOn => 'ඉහළ නංවා ඇත';

  @override
  String get saveChanges => 'වෙනස්කම් සුරකින්න';
}
