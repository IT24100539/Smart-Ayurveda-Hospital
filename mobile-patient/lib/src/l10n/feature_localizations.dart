import 'package:flutter/widgets.dart';
import 'package:intl/intl.dart';

/// Locale-aware copy for the appointment and ward modules.
///
/// Kept as a small typed facade so feature screens never contain hard-coded
/// language checks and all patient-visible copy changes with the app locale.
class FeatureLocalizations {
  const FeatureLocalizations._(this._si);

  final bool _si;

  factory FeatureLocalizations.of(BuildContext context) =>
      FeatureLocalizations._(
        Localizations.localeOf(context).languageCode == 'si',
      );

  String text(String en, String si) => _si ? si : en;
  String date(String pattern, DateTime value) =>
      DateFormat(pattern, _si ? 'si' : 'en').format(value);

  String get bookAppointment =>
      text('Book appointment', 'හමුවක් වෙන්කරවා ගන්න');
  String get therapyStep => text('Therapy', 'ප්‍රතිකාරය');
  String get confirmStep => text('Confirm', 'තහවුරු');
  String get chooseTherapy =>
      text('Choose a therapy', 'ප්‍රතිකාරයක් තෝරන්න');
  String get noTherapies => text(
    'No therapies are available to book right now.',
    'මේ මොහොතේ වෙන්කරවා ගැනීමට ප්‍රතිකාර නොමැත.',
  );
  String get loadTherapiesError =>
      text('Could not load therapies.', 'ප්‍රතිකාර පූරණය කළ නොහැක.');
  String get chooseDate => text('Choose a date', 'දිනයක් තෝරන්න');
  String get unavailableGrey => text(
    'Unavailable days are shown in grey.',
    'ලබාගත නොහැකි දින අළු පැහැයෙන් පෙන්වයි.',
  );
  String get chooseTime => text('Choose a time', 'වේලාවක් තෝරන්න');
  String get noSlots => text(
    'No appointment slots remain for this date. Please choose another day.',
    'මෙම දිනය සඳහා හමුවීම් වේලා නොමැත. වෙනත් දිනයක් තෝරන්න.',
  );
  String get back => text('Back', 'ආපසු');
  String get review => text('Review', 'සමාලෝචනය');
  String get confirmRequest => text('Confirm request', 'ඉල්ලීම තහවුරු කරන්න');
  String get treatment => text('Treatment', 'ප්‍රතිකාරය');
  String get dateLabel => text('Date', 'දිනය');
  String get timeLabel => text('Time', 'වේලාව');
  String get doctor => text('Doctor', 'වෛද්‍යවරයා');
  String duration(int minutes) => text('$minutes minutes', 'මිනිත්තු $minutes');
  String get appointmentReviewNote => text(
    'This is an appointment request. Hospital staff must approve it before it is confirmed.',
    'මෙය හමුවීමක් සඳහා ඉල්ලීමකි. එය තහවුරු වීමට පෙර රෝහල් කාර්ය මණ්ඩලය අනුමත කළ යුතුය.',
  );
  String get requestAppointment =>
      text('Request appointment', 'හමුවීම ඉල්ලන්න');
  String get profileUnavailable => text(
    'Your patient profile is unavailable. Please sign in again.',
    'ඔබගේ රෝගී පැතිකඩ ලබාගත නොහැක. නැවත පුරනය වන්න.',
  );
  String get appointmentRequested =>
      text('Appointment requested', 'හමුවීම ඉල්ලා ඇත');
  String get pendingApproval => text(
    'Request sent - pending staff approval',
    'ඉල්ලීම යවා ඇත - කාර්ය මණ්ඩල අනුමැතිය බලාපොරොත්තුවෙන්',
  );
  String get pendingExplanation => text(
    'Your request was sent successfully. It is not confirmed yet; you can track its status in My appointments.',
    'ඔබගේ ඉල්ලීම සාර්ථකව යවා ඇත. එය තවම තහවුරු කර නැත; මගේ හමුවීම් තුළ එහි තත්ත්වය බලන්න.',
  );
  String get done => text('Done', 'අවසන්');

  String get myAppointments => text('My appointments', 'මගේ හමුවීම්');
  String loadAppointmentsError(Object error) => text(
    'Could not load appointments.\n$error',
    'හමුවීම් පූරණය කළ නොහැක.\n$error',
  );
  String get tryAgain => text('Try again', 'නැවත උත්සාහ කරන්න');
  String get noAppointments => text('No appointments yet', 'තවම හමුවීම් නොමැත');
  String get chooseTreatmentFirst => text(
    'Choose a treatment to request your first appointment.',
    'ඔබගේ පළමු හමුවීම ඉල්ලීමට ප්‍රතිකාරයක් තෝරන්න.',
  );
  String get bookNewAppointment =>
      text('Book appointment', 'හමුවීමක් වෙන්කරවා ගන්න');
  String get cancelAppointment =>
      text('Cancel appointment', 'හමුවීම අවලංගු කරන්න');
  String get cancelQuestion =>
      text('Cancel appointment?', 'හමුවීම අවලංගු කරන්නද?');
  String cancelExplanation(String treatment) => text(
    'Cancel your $treatment request? This cannot be undone.',
    '$treatment සඳහා ඔබගේ ඉල්ලීම අවලංගු කරන්නද? මෙය ආපසු හැරවිය නොහැක.',
  );
  String get keep => text('Keep', 'තබා ගන්න');
  String get appointmentCancelled =>
      text('Appointment cancelled.', 'හමුවීම අවලංගු කරන ලදී.');
  String cancelError(Object error) => text(
    'Could not cancel appointment: $error',
    'හමුවීම අවලංගු කළ නොහැක: $error',
  );
  String get tapForDetails =>
      text('Tap for details', 'විස්තර සඳහා තට්ටු කරන්න');
  String status(String value) => switch (value) {
    'pending' => text('Pending', 'බලාපොරොත්තුවෙන්'),
    'approved' => text('Approved', 'අනුමතයි'),
    'rejected' => text('Rejected', 'ප්‍රතික්ෂේපයි'),
    'completed' => text('Completed', 'සම්පූර්ණයි'),
    'cancelled' => text('Cancelled', 'අවලංගුයි'),
    _ => value,
  };

  String get wardAvailability =>
      text('Ward availability', 'වාට්ටු ලබාගත හැකියාව');
  String wardLoadError(Object error) =>
      text('Could not load wards.\n$error', 'වාට්ටු පූරණය කළ නොහැක.\n$error');
  String get currentAvailability =>
      text('Current availability', 'වත්මන් ලබාගත හැකියාව');
  String get privacyNote => text(
    'Only occupancy totals are shown to protect patient privacy.',
    'රෝගීන්ගේ පෞද්ගලිකත්වය ආරක්ෂා කිරීම සඳහා පෙන්වන්නේ සමස්ත සංඛ්‍යා පමණි.',
  );
  String get requestAdmission =>
      text('Request admission', 'නේවාසික ඇතුළත් කිරීම ඉල්ලන්න');
  String get admissionReviewNote => text(
    'Admission requests are subject to staff review. Availability can change, and selecting a ward does not reserve a bed.',
    'ඇතුළත් කිරීමේ ඉල්ලීම් කාර්ය මණ්ඩල සමාලෝචනයට යටත් වේ. ලබාගත හැකියාව වෙනස් විය හැකි අතර වාට්ටුවක් තේරීමෙන් ඇඳක් වෙන් නොවේ.',
  );
  String get preferredWard => text('Preferred ward', 'කැමති වාට්ටුව');
  String available(int count) =>
      text('$count available', '$count ක් ලබාගත හැක');
  String get chooseWard => text('Please choose a ward', 'වාට්ටුවක් තෝරන්න');
  String get admissionReason =>
      text('Reason for admission', 'ඇතුළත් වීමට හේතුව');
  String get reasonRequired => text(
    'Please explain why admission is needed',
    'ඇතුළත් වීම අවශ්‍ය වන්නේ මන්දැයි සඳහන් කරන්න',
  );
  String get preferredDate => text('Preferred date', 'කැමති දිනය');
  String get chooseDateError => text('Please choose a date', 'දිනයක් තෝරන්න');
  String get selectDate => text('Select a date', 'දිනයක් තෝරන්න');
  String get sendAdmissionRequest =>
      text('Send admission request', 'ඇතුළත් කිරීමේ ඉල්ලීම යවන්න');
  String get requestPendingReview =>
      text('Request pending review', 'ඉල්ලීම සමාලෝචනය බලාපොරොත්තුවෙන්');
  String get admissionPendingExplanation => text(
    'Your admission request was sent to hospital staff. A bed is not reserved until staff approve the request.',
    'ඔබගේ ඇතුළත් කිරීමේ ඉල්ලීම රෝහල් කාර්ය මණ්ඩලයට යවා ඇත. ඔවුන් අනුමත කරන තුරු ඇඳක් වෙන් නොවේ.',
  );
  String get understood => text('Understood', 'තේරුණා');
  String admissionError(Object error) =>
      text('Could not send request: $error', 'ඉල්ලීම යැවිය නොහැක: $error');
  String get full => text('Full', 'පිරී ඇත');
  String occupied(int occupied, int total) => text(
    '$occupied of $total beds occupied',
    'ඇඳන් $total න් $occupied ක් භාවිතයේ ඇත',
  );

  String get askAboutTreatments =>
      text('Ask about treatments', 'ප්‍රතිකාර ගැන අසන්න');
  String get askAboutTreatmentsHint => text(
    'Ask when a therapy is offered or what it costs. Answers come from the catalogue only and are not medical advice.',
    'ප්‍රතිකාරයක් ලබා දෙන දින හෝ ගාස්තු ගැන අසන්න. පිළිතුරු ලැයිස්තුවෙන් පමණක් ලැබෙන අතර වෛද්‍ය උපදෙස් නොවේ.',
  );
  String get askQuestionLabel => text('Your question', 'ඔබේ ප්‍රශ්නය');
  String get askQuestionPlaceholder => text(
    'Example: When is Panchakarma available and what is the fee?',
    'උදාහරණය: පංචකර්ම ලබා දෙන්නේ කවදාද සහ ගාස්තුව කීයද?',
  );
  String get askButton => text('Ask', 'අසන්න');
  String get asking => text('Asking…', 'අසමින්…');
  String get askSignInRequired => text(
    'Sign in to ask about therapies, days, and fees.',
    'ප්‍රතිකාර, දින සහ ගාස්තු ගැන අසන්නට පුරනය වන්න.',
  );
  String get askSignIn => text('Sign in', 'පුරනය වන්න');
  String get medicalAdviceRefused =>
      text('Medical advice refused', 'වෛද්‍ය උපදෙස් ප්‍රතික්ෂේපයි');
  String get askUnavailable => text(
    'The treatment information assistant is unavailable. Please try again.',
    'ප්‍රතිකාර තොරතුරු සහායකයා ලබාගත නොහැක. නැවත උත්සාහ කරන්න.',
  );
  String get charakaChatTitle =>
      text('Charaka AI Assistant', 'චරක AI සහායකයා');
  String get charakaDisclaimer => text(
    'Charaka provides general information on hospital therapies and administrative records. It does not provide medical advice or diagnoses. Always consult a qualified physician.',
    'චරක රෝහල් ප්‍රතිකාර සහ පරිපාලන වාර්තා පිළිබඳ සාමාන්‍ය තොරතුරු සපයයි. එය වෛද්‍ය උපදෙස් හෝ රෝග විනිශ්චය ලබා නොදේ. සෑමවිටම සුදුසුකම් ලත් වෛද්‍යවරයෙකුගෙන් උපදෙස් ලබාගන්න.',
  );
  String get charakaOfflineBanner => text(
    'AI assistant is currently unreachable. Please check your network connection.',
    'AI සහායකයා මේ අවස්ථාවේ සම්බන්ධ කරගත නොහැක. ඔබගේ ජාල සබඳතාව පරීක්ෂා කරන්න.',
  );
  String get charakaTyping =>
      text('Charaka is checking hospital records…', 'චරක රෝහල් වාර්තා පරීක්ෂා කරමින්…');
  String get charakaRetry => text('Retry', 'නැවත උත්සාහ කරන්න');
  String get charakaInputHint =>
      text('Ask a question about therapies or your records…', 'ප්‍රතිකාර හෝ ඔබගේ වාර්තා ගැන ප්‍රශ්නයක් අසන්න…');
  String get charakaTopicTreatments =>
      text('Therapies & Fees', 'ප්‍රතිකාර සහ ගාස්තු');
  String get charakaTopicPatient =>
      text('My Patient Info', 'මගේ රෝගී තොරතුරු');
  String get charakaWelcomeTreatments => text(
    'Ayubowan! I am Charaka, your hospital assistant. You can ask about hospital therapies, fees, schedules, and treatment durations.',
    'ආයුබෝවන්! මම ඔබගේ රෝහල් සහායක චරක. ඔබට අපගේ ආයුර්වේද ප්‍රතිකාර, ගාස්තු, කාලසටහන් සහ ප්‍රතිකාර කාලසීමාවන් ගැන විමසිය හැක.',
  );
  String get charakaWelcomePatient => text(
    'Ayubowan! You can ask me about your administrative details such as registered UHID, address, contact number, or constitution type.',
    'ආයුබෝවන්! ලියාපදිංචි UHID අංකය, ලිපිනය, දුරකථන අංකය හෝ ප්‍රකෘති වර්ගය වැනි ඔබගේ පරිපාලන තොරතුරු විමසිය හැක.',
  );
}
