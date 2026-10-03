import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:patient_app/src/app.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/features/doctors/application/doctors_provider.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/onboarding/data/onboarding_storage.dart';
import 'package:patient_app/src/features/profile/presentation/legal_document_screen.dart';
import 'package:patient_app/src/features/profile/presentation/profile_screen.dart';
import 'package:patient_app/src/features/profile/presentation/widgets/theme_mode_switcher.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/router/app_routes.dart';
import 'package:patient_app/src/session_cache.dart';
import 'package:patient_app/src/theme/app_theme.dart';
import 'package:patient_app/src/theme/theme_mode_controller.dart';
import 'package:patient_app/src/theme/theme_mode_storage.dart';

class _SignedInAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
    status: AuthStatus.authenticated,
    user: AuthUser(
      id: '11111111-1111-1111-1111-111111111111',
      fullName: 'Meera Nair',
      email: 'meera.nair@example.local',
      phoneNumber: '9876500001',
      role: UserRole.patient,
    ),
  );
}

Future<void> _pumpProfile(WidgetTester tester, Widget widget) async {
  tester.view.physicalSize = const Size(900, 2600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(widget);
  await tester.pumpAndSettle();
}

Widget _profileHarness({
  required Locale locale,
  required InMemoryThemeModeStorage themeStorage,
  List<Override> extraOverrides = const [],
}) {
  final router = GoRouter(
    initialLocation: AppRoutes.profile,
    routes: [
      GoRoute(
        path: AppRoutes.profile,
        builder: (context, state) => const ProfileScreen(),
        routes: [
          GoRoute(
            path: 'privacy',
            builder: (context, state) => const PrivacyPolicyScreen(),
          ),
          GoRoute(
            path: 'terms',
            builder: (context, state) => const TermsOfUseScreen(),
          ),
        ],
      ),
    ],
  );

  return ProviderScope(
    overrides: [
      authControllerProvider.overrideWith(_SignedInAuth.new),
      tokenStorageProvider.overrideWithValue(InMemoryTokenStorage('token')),
      themeModeStorageProvider.overrideWithValue(themeStorage),
      ...extraOverrides,
    ],
    child: Consumer(
      builder: (context, ref, _) {
        final themeMode = ref.watch(themeControllerProvider);
        return MaterialApp.router(
          theme: AppTheme.light,
          darkTheme: AppTheme.dark,
          themeMode: themeMode,
          locale: locale,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
          routerConfig: router,
        );
      },
    ),
  );
}

void main() {
  test('privacy and terms stay behind sign-in', () {
    expect(AppRoutes.isPublic(AppRoutes.privacyPolicy), isFalse);
    expect(AppRoutes.isPublic(AppRoutes.termsOfUse), isFalse);
    expect(AppRoutes.privacyPolicy, '/profile/privacy');
    expect(AppRoutes.termsOfUse, '/profile/terms');
  });

  test('theme choice is restored from local storage', () async {
    final storage = InMemoryThemeModeStorage(ThemeMode.dark);
    final container = ProviderContainer(
      overrides: [themeModeStorageProvider.overrideWithValue(storage)],
    );
    addTearDown(container.dispose);

    container.read(themeControllerProvider);
    await Future<void>.delayed(Duration.zero);

    expect(container.read(themeControllerProvider), ThemeMode.dark);

    await container
        .read(themeControllerProvider.notifier)
        .setMode(ThemeMode.light);
    expect(await storage.read(), ThemeMode.light);

    final restored = ProviderContainer(
      overrides: [themeModeStorageProvider.overrideWithValue(storage)],
    );
    addTearDown(restored.dispose);
    restored.read(themeControllerProvider);
    await Future<void>.delayed(Duration.zero);

    expect(restored.read(themeControllerProvider), ThemeMode.light);
  });

  test('clearing the session drops cached patient data and images', () async {
    TestWidgetsFlutterBinding.ensureInitialized();
    var notificationLoads = 0;
    final container = ProviderContainer(
      overrides: [
        notificationsProvider.overrideWith((ref) async {
          notificationLoads += 1;
          return const <PatientNotification>[];
        }),
        myFeedbackProvider.overrideWith(
          (ref) async => const <PatientFeedback>[],
        ),
        myComplaintsProvider.overrideWith(
          (ref) async => const <PatientComplaint>[],
        ),
        publicFeedProvider.overrideWith(
          (ref) async => const <PublicFeedback>[],
        ),
        treatmentsProvider.overrideWith((ref) async => const []),
      ],
    );
    addTearDown(container.dispose);

    await container.read(notificationsProvider.future);
    container.read(treatmentsSearchQueryProvider.notifier).state = 'shirodhara';
    container.read(doctorsSearchQueryProvider.notifier).state = 'nadi';
    expect(notificationLoads, 1);

    container.read(sessionCacheProvider).clear();

    expect(container.read(treatmentsSearchQueryProvider), isEmpty);
    expect(container.read(doctorsSearchQueryProvider), isEmpty);
    expect(PaintingBinding.instance.imageCache.currentSize, 0);

    await container.read(notificationsProvider.future);
    expect(notificationLoads, 2);
  });

  testWidgets('profile shows legal links, retention note, and theme choices', (
    tester,
  ) async {
    await _pumpProfile(
      tester,
      _profileHarness(
        locale: const Locale('en'),
        themeStorage: InMemoryThemeModeStorage(),
      ),
    );

    expect(find.text('Privacy policy'), findsOneWidget);
    expect(find.text('Terms of use'), findsOneWidget);
    expect(find.text('ASK ME for the real text'), findsOneWidget);
    expect(find.text('Data and chat retention'), findsOneWidget);
    expect(
      find.textContaining('That period is not written yet.'),
      findsOneWidget,
    );
    expect(find.byKey(ThemeModeSwitcherKeys.control), findsOneWidget);
    expect(find.text('System'), findsOneWidget);
    expect(find.text('Light'), findsOneWidget);
    expect(find.text('Dark'), findsOneWidget);
    expect(find.byKey(ProfileKeys.signOut), findsOneWidget);
  });

  testWidgets('privacy and terms screens are marked as placeholders', (
    tester,
  ) async {
    await _pumpProfile(
      tester,
      _profileHarness(
        locale: const Locale('en'),
        themeStorage: InMemoryThemeModeStorage(),
      ),
    );

    await tester.tap(find.text('Privacy policy'));
    await tester.pumpAndSettle();

    expect(find.byKey(LegalDocumentKeys.placeholderBanner), findsOneWidget);
    expect(find.text('ASK ME for the real text'), findsWidgets);
    expect(
      find.textContaining('not the hospital privacy policy'),
      findsOneWidget,
    );
    expect(find.textContaining('Charaka chat messages'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();
    await tester.tap(find.text('Terms of use'));
    await tester.pumpAndSettle();

    expect(find.byKey(LegalDocumentKeys.placeholderBanner), findsOneWidget);
    expect(
      find.textContaining('not the hospital terms of use'),
      findsOneWidget,
    );
  });

  testWidgets(
    'Sinhala profile shows the placeholder marker and retention note',
    (tester) async {
      await _pumpProfile(
        tester,
        _profileHarness(
          locale: const Locale('si'),
          themeStorage: InMemoryThemeModeStorage(),
        ),
      );

      expect(find.textContaining('ASK ME for the real text'), findsOneWidget);
      expect(find.text('රහස්‍යතා ප්‍රතිපත්තිය'), findsOneWidget);
      expect(find.text('භාවිත නියම'), findsOneWidget);
      expect(find.text('දත්ත සහ කතාබස් තබා ගැනීම'), findsOneWidget);
      expect(find.text('පද්ධතිය'), findsOneWidget);
      expect(find.text('එළිය'), findsOneWidget);
      expect(find.text('අඳුර'), findsOneWidget);
    },
  );

  testWidgets('theme choices fit a phone-width profile', (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      _profileHarness(
        locale: const Locale('si'),
        themeStorage: InMemoryThemeModeStorage(),
      ),
    );
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(
      find.byKey(ThemeModeSwitcherKeys.control),
      300,
    );
    expect(find.text('පද්ධතිය'), findsOneWidget);
    expect(find.text('එළිය'), findsOneWidget);
    expect(find.text('අඳුර'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('theme toggle applies dark mode and keeps the choice', (
    tester,
  ) async {
    final storage = InMemoryThemeModeStorage();
    await _pumpProfile(
      tester,
      _profileHarness(locale: const Locale('en'), themeStorage: storage),
    );

    await tester.tap(find.text('Dark'));
    await tester.pumpAndSettle();

    final context = tester.element(find.byType(ProfileScreen));
    expect(Theme.of(context).brightness, Brightness.dark);
    expect(await storage.read(), ThemeMode.dark);
  });

  testWidgets('log out clears the token and patient caches', (tester) async {
    final storage = InMemoryTokenStorage('stored.jwt.value');
    var notificationLoads = 0;

    tester.view.physicalSize = const Size(900, 2600);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          unauthorizedOverride,
          tokenStorageProvider.overrideWithValue(storage),
          onboardingStorageProvider.overrideWithValue(
            InMemoryOnboardingStorage(seen: true),
          ),
          themeModeStorageProvider.overrideWithValue(
            InMemoryThemeModeStorage(),
          ),
          treatmentsProvider.overrideWith((ref) async => const []),
          notificationsProvider.overrideWith((ref) async {
            notificationLoads += 1;
            return const <PatientNotification>[];
          }),
          myFeedbackProvider.overrideWith(
            (ref) async => const <PatientFeedback>[],
          ),
          myComplaintsProvider.overrideWith(
            (ref) async => const <PatientComplaint>[],
          ),
          publicFeedProvider.overrideWith(
            (ref) async => const <PublicFeedback>[],
          ),
        ],
        child: const PatientApp(),
      ),
    );
    await tester.pumpAndSettle();

    final context = tester.element(find.byType(PatientApp));
    final container = ProviderScope.containerOf(context);
    await container.read(notificationsProvider.future);
    container.read(treatmentsSearchQueryProvider.notifier).state = 'abhyanga';
    expect(notificationLoads, 1);

    final si = await AppLocalizations.delegate.load(const Locale('si'));
    await tester.tap(find.text(si.navProfile));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(ProfileKeys.signOut));
    await tester.pumpAndSettle();

    expect(find.byKey(LoginScreenKeys.submit), findsOneWidget);
    expect(await storage.readToken(), isNull);
    expect(container.read(treatmentsSearchQueryProvider), isEmpty);
    await container.read(notificationsProvider.future);
    expect(notificationLoads, 2);
  });
}
