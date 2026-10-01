import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/app.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';
import 'package:patient_app/src/features/onboarding/application/onboarding_controller.dart';
import 'package:patient_app/src/features/onboarding/data/onboarding_storage.dart';
import 'package:patient_app/src/features/onboarding/presentation/onboarding_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';

class _UnauthenticatedAuth extends AuthController {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);
}

class _MockOnboardingController extends OnboardingController {
  _MockOnboardingController(this._initialSeen);
  final bool _initialSeen;

  @override
  OnboardingState build() =>
      OnboardingState(isResolved: true, hasSeenOnboarding: _initialSeen);
}

class _EnLocaleController extends LocaleController {
  @override
  Locale build() => const Locale('en');
}

Widget _buildApp({required OnboardingStorage onboardingStorage, required bool seen}) {
  return ProviderScope(
    overrides: [
      unauthorizedOverride,
      localeControllerProvider.overrideWith(_EnLocaleController.new),
      tokenStorageProvider.overrideWithValue(InMemoryTokenStorage(null)),
      onboardingStorageProvider.overrideWithValue(onboardingStorage),
      onboardingControllerProvider.overrideWith(
        () => _MockOnboardingController(seen),
      ),
      authControllerProvider.overrideWith(_UnauthenticatedAuth.new),
      treatmentsProvider.overrideWith((ref) async => const []),
    ],
    child: const PatientApp(),
  );
}

void main() {
  testWidgets('first-launch user lands on onboarding carousel with 3 slides', (
    tester,
  ) async {
    final storage = InMemoryOnboardingStorage(seen: false);
    await tester.pumpWidget(_buildApp(onboardingStorage: storage, seen: false));
    await tester.pumpAndSettle();

    // Verify Onboarding carousel is shown
    expect(find.byKey(OnboardingKeys.pageView), findsOneWidget);
    expect(find.byKey(OnboardingKeys.skipButton), findsOneWidget);
    expect(find.byKey(OnboardingKeys.nextButton), findsOneWidget);
    expect(find.byKey(OnboardingKeys.page(0)), findsOneWidget);

    // Slide 1 content check
    expect(find.textContaining('Ayurvedic Care'), findsOneWidget);

    // Tap Next to advance to slide 2
    await tester.tap(find.byKey(OnboardingKeys.nextButton));
    await tester.pumpAndSettle();

    expect(find.textContaining('Appointments & Health Hub'), findsOneWidget);
    expect(find.byKey(OnboardingKeys.nextButton), findsOneWidget);

    // Tap Next to advance to slide 3
    await tester.tap(find.byKey(OnboardingKeys.nextButton));
    await tester.pumpAndSettle();

    expect(find.textContaining('Charaka Hospital Assistant'), findsOneWidget);
    expect(find.byKey(OnboardingKeys.getStartedButton), findsOneWidget);
    expect(find.byKey(OnboardingKeys.skipButton), findsNothing);

    // Tap Get Started
    await tester.tap(find.byKey(OnboardingKeys.getStartedButton));
    await tester.pumpAndSettle();

    // Verify marked seen and navigated to login
    expect(await storage.hasSeenOnboarding(), isTrue);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });

  testWidgets('tapping Skip marks onboarding as seen and routes to login', (
    tester,
  ) async {
    final storage = InMemoryOnboardingStorage(seen: false);
    await tester.pumpWidget(_buildApp(onboardingStorage: storage, seen: false));
    await tester.pumpAndSettle();

    expect(find.byKey(OnboardingKeys.skipButton), findsOneWidget);
    await tester.tap(find.byKey(OnboardingKeys.skipButton));
    await tester.pumpAndSettle();

    expect(await storage.hasSeenOnboarding(), isTrue);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });

  testWidgets('returning user bypasses onboarding and lands directly on login', (
    tester,
  ) async {
    final storage = InMemoryOnboardingStorage(seen: true);
    await tester.pumpWidget(_buildApp(onboardingStorage: storage, seen: true));
    await tester.pumpAndSettle();

    // Onboarding should not be shown
    expect(find.byKey(OnboardingKeys.pageView), findsNothing);
    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
  });
}
