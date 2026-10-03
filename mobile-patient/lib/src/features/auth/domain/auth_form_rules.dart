import '../../../core/config/app_config.dart';
import '../../../l10n/app_localizations.dart';

/// Intentionally permissive; `EmailValidator` on the API is the real gate.
final emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

/// Special characters accepted by `PasswordPolicy` on Hospital.Api.
final passwordSpecialPattern = RegExp(r'''[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]''');

String? validateEmailField(String? value, AppLocalizations l10n) {
  final email = value?.trim() ?? '';
  if (email.isEmpty) return l10n.emailRequired;
  if (!emailPattern.hasMatch(email)) return l10n.emailInvalid;
  return null;
}

/// Client-side mirror of `RegisterRequestValidator` / complete-reset policy.
String? validateNewPassword(String? value, AppLocalizations l10n) {
  final password = value ?? '';
  if (password.isEmpty) return l10n.passwordRequired;
  if (password.length < AppConfig.minPasswordLength) {
    return l10n.passwordTooShort(AppConfig.minPasswordLength);
  }
  if (password.length > AppConfig.maxPasswordLength) {
    return l10n.passwordTooLong(AppConfig.maxPasswordLength);
  }
  if (AppConfig.passwordRequireUppercase &&
      !RegExp(r'[A-Z]').hasMatch(password)) {
    return l10n.passwordNeedsUppercase;
  }
  if (AppConfig.passwordRequireLowercase &&
      !RegExp(r'[a-z]').hasMatch(password)) {
    return l10n.passwordNeedsLowercase;
  }
  if (AppConfig.passwordRequireDigit && !RegExp(r'[0-9]').hasMatch(password)) {
    return l10n.passwordNeedsDigit;
  }
  if (AppConfig.passwordRequireSpecial &&
      !passwordSpecialPattern.hasMatch(password)) {
    return l10n.passwordNeedsSpecial;
  }
  return null;
}

/// Shown under the register password field so the rules are visible before submit.
String registerPasswordHint(AppLocalizations l10n) {
  final lines = <String>[
    l10n.passwordTooShort(AppConfig.minPasswordLength),
    if (AppConfig.passwordRequireUppercase) l10n.passwordNeedsUppercase,
    if (AppConfig.passwordRequireLowercase) l10n.passwordNeedsLowercase,
    if (AppConfig.passwordRequireDigit) l10n.passwordNeedsDigit,
    if (AppConfig.passwordRequireSpecial) l10n.passwordNeedsSpecial,
  ];
  return lines.join('\n');
}

String? validatePasswordConfirmation(
  String? value,
  String newPassword,
  AppLocalizations l10n,
) {
  if (value == null || value.isEmpty) return l10n.confirmPasswordRequired;
  if (value != newPassword) return l10n.passwordsDoNotMatch;
  return null;
}
