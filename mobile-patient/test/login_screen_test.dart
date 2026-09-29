import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';
import 'package:patient_app/src/theme/app_theme.dart';

/// Pumps [LoginScreen] on its own.
///
/// The token storage is overridden so the test never reaches
/// `flutter_secure_storage`'s platform channel.
Future<void> pumpLoginScreen(
  WidgetTester tester, {
  Locale locale = defaultLocale,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        tokenStorageProvider.overrideWithValue(InMemoryTokenStorage()),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        locale: locale,
        supportedLocales: supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: const LoginScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  test(
    'Sri Lankan mobile number validation accepts only supported formats',
    () {
      expect(isValidSriLankanMobileNumber('0771234567'), isTrue);
      expect(isValidSriLankanMobileNumber('+94 77 123 4567'), isTrue);
      expect(isValidSriLankanMobileNumber('12345'), isFalse);
      expect(isValidSriLankanMobileNumber('071123456'), isFalse);
      expect(isValidSriLankanMobileNumber('+94791234567'), isFalse);
    },
  );

  testWidgets('login screen renders email, password and submit button', (
    tester,
  ) async {
    await pumpLoginScreen(tester);

    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.byKey(LoginScreenKeys.email), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.password), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);

    // Registration-only fields stay hidden in login mode.
    expect(find.byKey(LoginScreenKeys.fullName), findsNothing);
    expect(find.byKey(LoginScreenKeys.phoneNumber), findsNothing);
  });

  testWidgets('login screen defaults to Sinhala labels', (tester) async {
    await pumpLoginScreen(tester);

    final si = await AppLocalizations.delegate.load(const Locale('si'));
    expect(find.text(si.emailLabel), findsOneWidget);
    expect(find.text(si.passwordLabel), findsOneWidget);
    expect(find.text(si.signIn), findsWidgets);
    expect(find.text(si.chooseLanguage), findsOneWidget);
    expect(find.text(si.languageSinhala), findsOneWidget);
    expect(find.text(si.languageEnglish), findsOneWidget);
  });

  testWidgets('login language switcher changes the form to English', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          tokenStorageProvider.overrideWithValue(InMemoryTokenStorage()),
        ],
        child: const _LocaleLoginApp(),
      ),
    );
    await tester.pumpAndSettle();

    final en = await AppLocalizations.delegate.load(const Locale('en'));
    await tester.tap(find.text(en.languageEnglish));
    await tester.pumpAndSettle();

    expect(find.text(en.chooseLanguage), findsOneWidget);
    expect(find.text(en.signIn), findsWidgets);
    expect(find.text(en.emailLabel), findsOneWidget);
  });

  testWidgets('switching to register mode reveals name and phone fields', (
    tester,
  ) async {
    await pumpLoginScreen(tester);

    await tester.tap(find.byKey(LoginScreenKeys.modeToggle));
    await tester.pumpAndSettle();

    expect(find.byType(TextFormField), findsNWidgets(5));
    expect(find.byKey(LoginScreenKeys.fullName), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.phoneNumber), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });
}

class _LocaleLoginApp extends ConsumerWidget {
  const _LocaleLoginApp();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeControllerProvider);
    return MaterialApp(
      theme: AppTheme.light,
      locale: locale,
      supportedLocales: supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: const LoginScreen(),
    );
  }
}
