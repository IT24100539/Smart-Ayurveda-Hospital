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
  String get dateOfBirthLabel => 'උපන් දිනය';

  @override
  String get dateOfBirthRequired => 'ඔබේ උපන් දිනය ඇතුළත් කරන්න';

  @override
  String get genderLabel => 'ස්ත්‍රී පුරුෂ භාවය';

  @override
  String get genderRequired => 'ස්ත්‍රී පුරුෂ භාවය තෝරන්න';

  @override
  String get genderFemale => 'ස්ත්‍රී';

  @override
  String get genderMale => 'පුරුෂ';

  @override
  String get genderOther => 'වෙනත්';

  @override
  String get genderUnspecified => 'නිශ්චිත නොවේ';

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
  String get registrationUnavailableMessage =>
      'ලබා දුන් විස්තරවලින් ගිණුමක් සෑදිය නොහැක.';

  @override
  String get accountTemporarilyLocked =>
      'මෙම ගිණුම තාවකාලිකව අගුළු දමා ඇත. කරුණාකර පසුව නැවත උත්සාහ කරන්න.';

  @override
  String get tooManyAttempts =>
      'උත්සාහයන් වැඩියි. කරුණාකර පසුව නැවත උත්සාහ කරන්න.';

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
      'උපදේශන වෙන් කරවා ගන්න, ඔබේ ප්‍රතිකාර කාලසටහන බලන්න, සහ ලියාපදිංචි සාරාංශය පරීක්ෂා කරන්න.';

  @override
  String get homeHospitalName => 'ස්මාර්ට් ආයුර්වේද රෝහල';

  @override
  String get homeHospitalAddress => 'පල්ලෙකැලේ, කුණ්ඩසාලේ 20168';

  @override
  String get homeHospitalPhone => '+94 81 242 0541';

  @override
  String get homeHospitalHours => 'සඳු-සිකු 8:00 පෙ.ව. - 5:30 ප.ව.';

  @override
  String get homeLoadError =>
      'රෝහල් තොරතුරු පූරණය කළ නොහැක. නැවත උත්සාහ කරන්න.';

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
      'සම්පූර්ණ වූ පැමිණීමක් හෝ ප්‍රතිකාරයක් තෝරන්න.';

  @override
  String get writeFeedback => 'ප්‍රතිචාරයක් ලියන්න';

  @override
  String get hubCommunity => 'ප්‍රජාව';

  @override
  String get hubMine => 'මගේ ප්‍රතිචාර';

  @override
  String get hubComplaints => 'පැමිණිලි';

  @override
  String get hubNotifications => 'දැනුම්දීම්';

  @override
  String get linkToVisit => 'සම්පූර්ණ වූ පැමිණීම';

  @override
  String get linkToTreatment => 'ප්‍රතිකාරය';

  @override
  String get chooseTreatment => 'ප්‍රතිකාරයක් තෝරන්න';

  @override
  String editTimeRemaining(int hours, int minutes) {
    return 'සංස්කරණයට හෝ ඉවත් කිරීමට පැය $hoursයි මිනිත්තු $minutesක් ඉතිරිය';
  }

  @override
  String get feedbackStatusPending => 'සමාලෝචනය බලාපොරොත්තුවෙන්';

  @override
  String get feedbackStatusVisible => 'පෙනේ';

  @override
  String get feedbackStatusHidden => 'සඟවා ඇත';

  @override
  String get feedbackStatusWithdrawn => 'ඉවත් කරන ලදි';

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
  String get notificationAppointmentApproved => 'වෙන්කිරීම අනුමතයි';

  @override
  String get notificationAppointmentRejected => 'වෙන්කිරීම අනුමත නොවීය';

  @override
  String get notificationAppointmentRescheduled => 'වෙන්කිරීම නැවත සකසන ලදි';

  @override
  String get notificationAppointmentCancelled => 'වෙන්කිරීම අවලංගුයි';

  @override
  String get notificationPrescriptionIssued => 'බෙහෙත් වට්ටෝරුව නිකුත් කළා';

  @override
  String get notificationInvoiceIssued => 'බිල්පත නිකුත් කළා';

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
      'සම්පූර්ණ වූ පැමිණීමක් තවම නැත. ඔබට ප්‍රතිකාරයක් ගැන ලිවිය හැක.';

  @override
  String get escalatedOn => 'ඉහළ නංවා ඇත';

  @override
  String get saveChanges => 'වෙනස්කම් සුරකින්න';

  @override
  String get myHealthHubTitle => 'මගේ සෞඛ්‍ය කේන්ද්‍රය';

  @override
  String get myHealthHubSubtitle =>
      'ඔබගේ ප්‍රතිකාර සැසි, බෙහෙත් වට්ටෝරු, බිල්පත් සහ ලේඛන.';

  @override
  String get myTherapySessionsTitle => 'මගේ ප්‍රතිකාර සැසි';

  @override
  String get noTherapySessionsFound => 'තවම ප්‍රතිකාර සැසි වාර්තා වී නොමැත.';

  @override
  String get nextSessionLabel => 'මීළඟ සැසිය';

  @override
  String get myRegistrationSummaryTitle => 'මගේ ලියාපදිංචි සාරාංශය';

  @override
  String get uhidLabel => 'UHID අංකය';

  @override
  String get prakritiLabel => 'ප්‍රකෘතිය';

  @override
  String get vikritiLabel => 'වික්‍රිතිය';

  @override
  String get allergiesLabel => 'ආසාත්මිකතා';

  @override
  String get bloodGroupLabel => 'රුධිර වර්ගය';

  @override
  String get notRecorded => 'වාර්තා වී නැත';

  @override
  String get healthHubError => 'සෞඛ්‍ය වාර්තා ලබාගත නොහැක.';

  @override
  String sessionsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'සැසි $count',
      one: 'සැසි 1',
    );
    return '$_temp0';
  }

  @override
  String get noPatientRecordLinked =>
      'මෙම ගිණුමට සම්බන්ධ රෝගී වාර්තාවක් හමු නොවීය.';

  @override
  String get noPatientRecordLinkedHelp =>
      'සායනික වාර්තාව එකම විද්‍යුත් තැපැල් ලිපිනය භාවිතා කළ යුතුය. කරුණාකර වාර්තාව සම්බන්ධ කිරීමට රෝහල් පිළිගැනීමේ කවුන්ටරය අමතන්න.';

  @override
  String get unlinkedRecordRetry => 'නැවත පරීක්ෂා කරන්න';

  @override
  String get unlinkedRecordHelpAction => 'පිළිගැනීමේ කවුන්ටරය අමතන්න';

  @override
  String get forgotPasswordLink => 'රහස් පදය අමතකද?';

  @override
  String get forgotPasswordTitle => 'රහස් පදය යළි සකසන්න';

  @override
  String get forgotPasswordHeading => 'ඔබේ රහස් පදය අමතක වුණාද?';

  @override
  String get forgotPasswordBody =>
      'ඔබේ විද්‍යුත් තැපැල් ලිපිනය ඇතුළත් කරන්න. එය ලියාපදිංචි වී ඇත්නම්, යළි සැකසුම් සබැඳියක් යවනු ලැබේ.';

  @override
  String get forgotPasswordSubmit => 'යළි සැකසුම් සබැඳිය යවන්න';

  @override
  String get forgotPasswordBackToSignIn => 'පිවිසීමට ආපසු';

  @override
  String get forgotPasswordSuccess =>
      'එම විද්‍යුත් තැපෑලට ගිණුමක් තිබේ නම්, රහස් පදය යළි සැකසීමේ සබැඳියක් යවා ඇත.';

  @override
  String get forgotPasswordError =>
      'යළි සැකසුම් ඉල්ලීම යැවිය නොහැකි විය. ඔබේ සම්බන්ධතාවය පරීක්ෂා කරන්න.';

  @override
  String get resetPasswordTitle => 'නව රහස් පදය';

  @override
  String get resetPasswordHeading => 'නව රහස් පදයක් සාදන්න';

  @override
  String get resetPasswordBody =>
      'ඔබේ විද්‍යුත් තැපෑලෙන් ලැබුණු යළි සැකසුම් ටෝකනය සහ නව රහස් පදය ඇතුළත් කරන්න.';

  @override
  String get resetTokenLabel => 'යළි සැකසුම් ටෝකනය';

  @override
  String get resetTokenRequired =>
      'ඔබේ විද්‍යුත් තැපෑලෙන් ලැබුණු යළි සැකසුම් ටෝකනය ඇතුළත් කරන්න';

  @override
  String get newPasswordLabel => 'නව රහස් පදය';

  @override
  String get confirmPasswordLabel => 'රහස් පදය තහවුරු කරන්න';

  @override
  String get confirmPasswordRequired => 'ඔබේ රහස් පදය තහවුරු කරන්න';

  @override
  String get passwordsDoNotMatch => 'රහස් පද නොගැලපේ';

  @override
  String get resetPasswordSubmit => 'රහස් පදය යළි සකසන්න';

  @override
  String get resetPasswordSuccess =>
      'ඔබේ රහස් පදය යළි සකසා ඇත. දැන් නව රහස් පදයෙන් පිවිසිය හැක.';

  @override
  String get resetPasswordError =>
      'රහස් පදය යළි සකසිය නොහැකි විය. ටෝකනය වලංගු නොවිය හැක හෝ කල් ඉකුත් වී ඇත.';

  @override
  String get resetPasswordSignInNow => 'දැන් පිවිසෙන්න';

  @override
  String get passwordNeedsUppercase =>
      'රහස් පදයේ අවම වශයෙන් එක් ලොකු අකුරක් තිබිය යුතුය';

  @override
  String get passwordNeedsLowercase =>
      'රහස් පදයේ අවම වශයෙන් එක් කුඩා අකුරක් තිබිය යුතුය';

  @override
  String get passwordNeedsDigit =>
      'රහස් පදයේ අවම වශයෙන් එක් ඉලක්කමක් තිබිය යුතුය';

  @override
  String get passwordNeedsSpecial =>
      'රහස් පදයේ අවම වශයෙන් එක් විශේෂ අක්ෂරයක් තිබිය යුතුය';

  @override
  String get showPassword => 'රහස් පදය පෙන්වන්න';

  @override
  String get hidePassword => 'රහස් පදය සඟවන්න';

  @override
  String get doctorsTitle => 'වෛද්‍යවරු';

  @override
  String get doctorsSubtitle => 'රෝහලේ උපදේශන ලබා දෙන වෛද්‍යවරු.';

  @override
  String get doctorsNavSubtitle => 'වෛද්‍ය කණ්ඩායම හමුවන්න';

  @override
  String get doctorsSearchHint => 'නම හෝ විශේෂඥතාව අනුව සොයන්න';

  @override
  String get doctorsEmpty => 'තවම වෛද්‍යවරු ලැයිස්තුගත කර නැත.';

  @override
  String get doctorsSearchEmpty => 'ඔබේ සෙවුමට ගැලපෙන වෛද්‍යවරු නැත.';

  @override
  String get doctorsLoadError => 'වෛද්‍යවරු පූරණය කළ නොහැක.';

  @override
  String get doctorProfileLoadError => 'මෙම වෛද්‍යවරයා පූරණය කළ නොහැක.';

  @override
  String get doctorQualificationsLabel => 'සුදුසුකම්';

  @override
  String get doctorAboutLabel => 'පිළිබඳව';

  @override
  String get healthHubUpcomingTab => 'ඉදිරි';

  @override
  String get healthHubTherapyTab => 'ප්‍රතිකාර';

  @override
  String get healthHubRegistrationTab => 'ලියාපදිංචිය';

  @override
  String get healthHubUpcomingEmpty => 'ඉදිරි හමුවීම් නොමැත.';

  @override
  String get healthHubPrescriptionsTab => 'බෙහෙත්';

  @override
  String get healthHubInvoicesTab => 'බිල්පත්';

  @override
  String get healthHubDocumentsTab => 'ලේඛන';

  @override
  String get myPrescriptionsTitle => 'මගේ බෙහෙත් වට්ටෝරු';

  @override
  String get prescriptionsEmpty => 'තවම බෙහෙත් වට්ටෝරු නිකුත් කර නැත.';

  @override
  String get prescriptionsLoadError => 'බෙහෙත් වට්ටෝරු පූරණය කළ නොහැක.';

  @override
  String prescriptionIssuedOn(String date) {
    return 'නිකුත් කළේ $date';
  }

  @override
  String prescriptionRevision(int number) {
    return 'සංශෝධනය $number';
  }

  @override
  String get prescriptionStatusIssued => 'නිකුත් කළ';

  @override
  String get prescriptionStatusSuperseded => 'ආදේශ කළ';

  @override
  String get prescriptionStatusCancelled => 'අවලංගු කළ';

  @override
  String get prescriptionStatusDraft => 'කෙටුම්පත';

  @override
  String get myInvoicesTitle => 'මගේ බිල්පත්';

  @override
  String get invoicesEmpty => 'තවම බිල්පත් නිකුත් කර නැත.';

  @override
  String get invoicesLoadError => 'බිල්පත් පූරණය කළ නොහැක.';

  @override
  String get paymentHistoryTitle => 'ගෙවීම් ඉතිහාසය';

  @override
  String get paymentsEmpty => 'ගෙවීම් වාර්තා වී නැත.';

  @override
  String get invoiceTotalLabel => 'එකතුව';

  @override
  String get amountPaidLabel => 'ගෙවූ මුදල';

  @override
  String get balanceLabel => 'ශේෂය';

  @override
  String get invoiceStatusIssued => 'නිකුත් කළ';

  @override
  String get invoiceStatusPaid => 'ගෙවූ';

  @override
  String get invoiceStatusCancelled => 'අවලංගු';

  @override
  String get invoiceStatusDraft => 'කෙටුම්පත';

  @override
  String get paymentMethodCash => 'මුදල්';

  @override
  String get paymentMethodCard => 'කාඩ්පත';

  @override
  String get paymentMethodBankTransfer => 'බැංකු මාරුව';

  @override
  String get myDocumentsTitle => 'මගේ ලේඛන';

  @override
  String get documentsEmpty => 'තවම ලේඛන උඩුගත කර නැත.';

  @override
  String get documentsLoadError => 'ලේඛන පූරණය කළ නොහැක.';

  @override
  String get documentView => 'බලන්න';

  @override
  String get documentDownload => 'බාගත කරන්න';

  @override
  String documentSaved(String fileName) {
    return '$fileName සුරකින ලදි';
  }

  @override
  String get documentDownloadError => 'මෙම ලේඛනය බාගත කළ නොහැක.';

  @override
  String get documentOpenError => 'මෙම ලේඛනය විවෘත කළ නොහැක.';

  @override
  String get documentPdfNotice => 'මෙම PDF ගොනුව බාගත කළ හැක.';

  @override
  String get documentFileNotice => 'මෙම ගොනුව බාගත කළ හැක.';

  @override
  String get documentCategoryLabReport => 'පරීක්ෂණ වාර්තාව';

  @override
  String get documentCategoryPrescriptionScan => 'බෙහෙත් වට්ටෝරුව';

  @override
  String get documentCategoryDiagnosticScan => 'පරීක්ෂණ ස්කෑනය';

  @override
  String get documentCategoryDischargeSummary => 'නිදහස් කිරීමේ සාරාංශය';

  @override
  String get documentCategoryTreatmentPlan => 'ප්‍රතිකාර සැලැස්ම';

  @override
  String get documentCategoryGeneral => 'සාමාන්‍ය';

  @override
  String doctorRatingSummary(String rating, int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'සමාලෝචන $count',
      one: 'සමාලෝචන 1',
    );
    return '$rating · $_temp0';
  }

  @override
  String get appearanceSectionTitle => 'පෙනුම';

  @override
  String get themeSystem => 'පද්ධතිය';

  @override
  String get themeLight => 'එළිය';

  @override
  String get themeDark => 'අඳුර';

  @override
  String get privacySectionTitle => 'රහස්‍යතාව';

  @override
  String get privacyPolicyTitle => 'රහස්‍යතා ප්‍රතිපත්තිය';

  @override
  String get privacyPolicySubtitle =>
      'මෙම යෙදුම ඔබේ ප්‍රතිකාර වාර්තාව හසුරුවන ආකාරය.';

  @override
  String get termsOfUseTitle => 'භාවිත නියම';

  @override
  String get termsOfUseSubtitle => 'රෝගී යෙදුම භාවිතා කිරීමේ නීති.';

  @override
  String get legalPlaceholderBanner =>
      'ASK ME for the real text. සැබෑ පෙළ සඳහා මගෙන් අසන්න.';

  @override
  String get legalPlaceholderHint =>
      'තාවකාලිකයි. මෙම තිරය රෝහල අනුමත කළ පෙළෙන් ප්‍රතිස්ථාපනය කරන්න.';

  @override
  String get privacyPolicyBody =>
      'මෙය තාවකාලික පෙළකි. මෙය රෝහලේ රහස්‍යතා ප්‍රතිපත්තිය නොවේ.\n\nමෙම යෙදුම තුළ ඔබේ ප්‍රකෘති විස්තර, හමුවීම්, බෙහෙත් වට්ටෝරු, බිල්පත් සහ වෛද්‍ය ලේඛන භාවිතා වන ආකාරය මෙහි විස්තර කෙරේ.';

  @override
  String get termsOfUseBody =>
      'මෙය තාවකාලික පෙළකි. මෙය රෝහලේ භාවිත නියම නොවේ.\n\nහමුවීම් ඉල්ලීම්, චරක පිළිතුරු සහ රෝගී යෙදුම භාවිතා කිරීමේදී ඔබේ වගකීම් මෙහි ඇතුළත් වේ.';

  @override
  String get retentionTitle => 'දත්ත සහ කතාබස් තබා ගැනීම';

  @override
  String get retentionNote =>
      'ප්‍රතිකාර වාර්තා සහ චරක කතාබස් පණිවිඩ තබා ගනු ලබන්නේ රෝහලේ තබා ගැනීමේ ප්‍රතිපත්තියෙන් නියම කරන කාලයට පමණි. එම කාලය තවම ලියා නැත.';

  @override
  String get devGalleryTitle => 'සංරචක ගැලරිය';

  @override
  String get devGalleryHint =>
      'නිදොස් කිරීම සඳහා පමණි. මේවා රෝහල් වාර්තා නොවේ.';

  @override
  String get devGallerySampleName => 'ආදර්ශය';

  @override
  String get devGallerySampleUhid => 'UHID';

  @override
  String get devGallerySection => 'පොදු සංරචක';

  @override
  String get devGalleryOpen => 'විවෘත කරන්න';

  @override
  String get devGalleryKicker => 'ප්‍රතිකාර සැසිය';

  @override
  String get devGalleryFact => 'දිනය';

  @override
  String get devGalleryFactValue => 'හමුවීමෙන්';

  @override
  String get devGalleryPrice => 'ලැයිස්තුවෙන්';

  @override
  String get devGalleryDuration => 'ලැයිස්තුවෙන්';

  @override
  String get devGalleryCategory => 'ප්‍රතිකාරය';

  @override
  String get devGalleryKpi => 'පූරණය වූ ගණන';

  @override
  String get devGalleryEmpty => 'මෙහි කිසිවක් නැත';

  @override
  String get devGalleryError => 'මෙම ආදර්ශය පූරණය කළ නොහැක.';

  @override
  String get devGalleryStepTherapy => 'ප්‍රතිකාරය';

  @override
  String get devGalleryStepDate => 'දිනය';

  @override
  String get devGalleryStepTime => 'වේලාව';

  @override
  String get devGalleryStepConfirm => 'තහවුරු කරන්න';
}
