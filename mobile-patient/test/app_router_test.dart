import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/app.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';

Future<AppLocalizations> _si() =>
    AppLocalizations.delegate.load(const Locale('si'));

Future<void> _pumpApp(WidgetTester tester, {String? storedToken}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        unauthorizedOverride,
        tokenStorageProvider.overrideWithValue(
          InMemoryTokenStorage(storedToken),
        ),
      ],
      child: const PatientApp(),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'cold start lands on the splash screen with a language switcher',
    (tester) async {
      await _pumpApp(tester);
      final si = await _si();

      expect(find.text(si.appTitle), findsOneWidget);
      expect(find.text(si.chooseLanguage), findsOneWidget);
      expect(find.byType(SegmentedButton<String>), findsOneWidget);
    },
  );

  testWidgets('language switcher swaps the UI to English', (tester) async {
    await _pumpApp(tester);
    final si = await _si();

    await tester.tap(find.text('English'));
    await tester.pumpAndSettle();

    expect(find.text('Continue'), findsOneWidget);
    expect(find.text(si.chooseLanguage), findsNothing);
  });

  testWidgets('without a stored token, continuing goes to the login screen', (
    tester,
  ) async {
    await _pumpApp(tester);
    final si = await _si();

    await tester.tap(find.text(si.continueLabel));
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.email), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.password), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });

  testWidgets('with a stored token, continuing goes to the five-tab shell', (
    tester,
  ) async {
    await _pumpApp(tester, storedToken: 'stored.jwt.value');
    final si = await _si();

    await tester.tap(find.text(si.continueLabel));
    await tester.pumpAndSettle();

    final navBar = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(navBar.destinations, hasLength(5));
    expect(find.text(si.homeGreetingGeneric), findsOneWidget);

    await tester.tap(find.text(si.navProfile));
    await tester.pumpAndSettle();
    expect(find.text(si.signOut), findsOneWidget);
  });

  testWidgets('signing out routes back to the login screen', (tester) async {
    await _pumpApp(tester, storedToken: 'stored.jwt.value');
    final si = await _si();

    await tester.tap(find.text(si.continueLabel));
    await tester.pumpAndSettle();
    await tester.tap(find.text(si.navProfile));
    await tester.pumpAndSettle();

    await tester.tap(find.text(si.signOut));
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
  });
}
