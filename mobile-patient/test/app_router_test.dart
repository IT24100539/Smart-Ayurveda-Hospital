import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/app.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';

Future<AppLocalizations> _si() =>
    AppLocalizations.delegate.load(const Locale('si'));

class _LoadingAuth extends AuthController {
  @override
  AuthState build() => const AuthState.loading();
}

class _UnauthenticatedAuth extends AuthController {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);
}

Future<void> _pumpApp(
  WidgetTester tester, {
  String? storedToken,
  List<Override> extraOverrides = const [],
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        unauthorizedOverride,
        tokenStorageProvider.overrideWithValue(
          InMemoryTokenStorage(storedToken),
        ),
        treatmentsProvider.overrideWith((ref) async => const []),
        ...extraOverrides,
      ],
      child: const PatientApp(),
    ),
  );
}

void main() {
  testWidgets('does not redirect to login while restoring the session', (
    tester,
  ) async {
    await _pumpApp(
      tester,
      extraOverrides: [
        authControllerProvider.overrideWith(_LoadingAuth.new),
      ],
    );
    await tester.pump();

    final si = await _si();
    expect(find.text(si.appTitle), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.submit), findsNothing);
    expect(find.byType(NavigationBar), findsNothing);
  });

  testWidgets('redirects to login only when unauthenticated', (tester) async {
    await _pumpApp(
      tester,
      extraOverrides: [
        authControllerProvider.overrideWith(_UnauthenticatedAuth.new),
      ],
    );
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.email), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.password), findsOneWidget);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
  });

  testWidgets('without a stored token, restore lands on the login screen', (
    tester,
  ) async {
    await _pumpApp(tester);
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });

  testWidgets('with a stored token, restore lands on Home in the five-tab shell', (
    tester,
  ) async {
    await _pumpApp(tester, storedToken: 'stored.jwt.value');
    await tester.pumpAndSettle();

    final si = await _si();
    final navBar = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(navBar.destinations, hasLength(5));
    expect(find.text(si.homeGreetingGeneric), findsOneWidget);
    expect(find.text(si.homeHospitalAddress), findsOneWidget);
    expect(find.text(si.navFeedback), findsWidgets);

    await tester.tap(find.text(si.navProfile));
    await tester.pumpAndSettle();
    expect(find.text(si.signOut), findsOneWidget);
  });

  testWidgets('signing out routes back to the login screen', (tester) async {
    await _pumpApp(tester, storedToken: 'stored.jwt.value');
    final si = await _si();

    await tester.pumpAndSettle();
    await tester.tap(find.text(si.navProfile));
    await tester.pumpAndSettle();

    await tester.tap(find.text(si.signOut));
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
  });

  testWidgets('cold start shows the splash language switcher while loading', (
    tester,
  ) async {
    await _pumpApp(
      tester,
      extraOverrides: [
        authControllerProvider.overrideWith(_LoadingAuth.new),
      ],
    );
    await tester.pump();
    final si = await _si();

    expect(find.text(si.appTitle), findsOneWidget);
    expect(find.text(si.chooseLanguage), findsOneWidget);
    expect(find.byType(SegmentedButton<String>), findsOneWidget);
  });
}
