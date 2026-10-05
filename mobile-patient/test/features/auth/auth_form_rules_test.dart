import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/auth/domain/auth_form_rules.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';

void main() {
  late AppLocalizations l10n;

  setUpAll(() async {
    l10n = await AppLocalizations.delegate.load(const Locale('en'));
  });

  test('validateEmailField rejects empty and malformed addresses', () {
    expect(validateEmailField('', l10n), l10n.emailRequired);
    expect(validateEmailField('not-an-email', l10n), l10n.emailInvalid);
    expect(validateEmailField('patient@hospital.lk', l10n), isNull);
  });

  test('validateNewPassword mirrors the Hospital.Api policy', () {
    expect(validateNewPassword('', l10n), l10n.passwordRequired);
    expect(validateNewPassword('Ab1!', l10n), l10n.passwordTooShort(8));
    expect(validateNewPassword('abcdefgh', l10n), l10n.passwordNeedsUppercase);
    expect(validateNewPassword('ABCDEFGH1!', l10n), l10n.passwordNeedsLowercase);
    expect(validateNewPassword('Abcdefgh!', l10n), l10n.passwordNeedsDigit);
    expect(validateNewPassword('Abcdefg1', l10n), l10n.passwordNeedsSpecial);
    expect(validateNewPassword('Abcdefg1!', l10n), isNull);
  });

  test('validatePasswordConfirmation requires a matching value', () {
    expect(
      validatePasswordConfirmation('', 'Abcdefg1!', l10n),
      l10n.confirmPasswordRequired,
    );
    expect(
      validatePasswordConfirmation('other', 'Abcdefg1!', l10n),
      l10n.passwordsDoNotMatch,
    );
    expect(validatePasswordConfirmation('Abcdefg1!', 'Abcdefg1!', l10n), isNull);
  });
}
