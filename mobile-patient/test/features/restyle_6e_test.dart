import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/features/contact/presentation/contact_location_screen.dart';
import 'package:patient_app/src/features/faq/presentation/faq_screen.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_hub_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_keys.dart';
import 'package:patient_app/src/features/feedback/presentation/my_complaints_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/my_feedback_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/notifications_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/public_feedback_feed_screen.dart';
import 'package:patient_app/src/features/home/presentation/home_screen.dart';
import 'package:patient_app/src/features/onboarding/presentation/onboarding_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart';
import 'package:patient_app/src/features/wards/data/ward_repository.dart';
import 'package:patient_app/src/features/wards/domain/ward.dart';
import 'package:patient_app/src/features/wards/presentation/ward_availability_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/shared/widgets/empty_state.dart';
import 'package:patient_app/src/shared/widgets/page_layout.dart';
import 'package:patient_app/src/shared/widgets/skeleton.dart';
import 'package:patient_app/src/theme/app_theme.dart';

/// Phone, tablet and Flutter web window sizes used for every screen.
const _sizes = <String, Size>{
  'phone': Size(360, 740),
  'tablet': Size(820, 1180),
  'web': Size(1280, 800),
};

const _brightnesses = [Brightness.light, Brightness.dark];

class _FakeAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
    status: AuthStatus.authenticated,
    user: AuthUser(
      id: '11111111-1111-1111-1111-111111111111',
      fullName: 'Saman Perera',
      email: 'saman@test.local',
      phoneNumber: '0711234567',
      role: UserRole.patient,
    ),
  );
}

class _SignedOutAuth extends AuthController {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);
}

Future<void> _pump(
  WidgetTester tester,
  Widget home, {
  Size size = const Size(360, 740),
  Brightness brightness = Brightness.light,
  List<Override> overrides = const [],
  bool settle = true,
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        theme: AppTheme.light,
        darkTheme: AppTheme.dark,
        themeMode: brightness == Brightness.dark
            ? ThemeMode.dark
            : ThemeMode.light,
        locale: const Locale('en'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: home,
      ),
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
}

Future<void> _openTab(WidgetTester tester, Key key) async {
  await tester.ensureVisible(find.byKey(key));
  await tester.pumpAndSettle();
  await tester.tap(find.byKey(key));
  await tester.pumpAndSettle();
}

/// Runs [body] for every size and brightness. Layout overflow fails the test.
void _matrix(
  String name,
  Future<void> Function(WidgetTester tester, Size size, Brightness brightness)
  body,
) {
  for (final entry in _sizes.entries) {
    for (final brightness in _brightnesses) {
      testWidgets('$name renders on ${entry.key} in ${brightness.name}', (
        tester,
      ) async {
        await body(tester, entry.value, brightness);
        expect(tester.takeException(), isNull);
      });
    }
  }
}

final _appointments = [
  Appointment(
    id: 'a1',
    treatmentName: 'Shirodhara',
    requestedDate: DateTime.now().add(const Duration(days: 2)),
    requestedTimeSlot: '10:00 - 11:00',
    status: AppointmentStatus.approved,
  ),
  Appointment(
    id: 'a2',
    treatmentName: 'Abhyanga',
    requestedDate: DateTime.now().add(const Duration(days: 5)),
    requestedTimeSlot: '09:00 - 10:00',
    status: AppointmentStatus.pending,
  ),
  Appointment(
    id: 'a3',
    treatmentName: 'Panchakarma Detox',
    requestedDate: DateTime.now().subtract(const Duration(days: 9)),
    requestedTimeSlot: '14:00 - 15:00',
    status: AppointmentStatus.completed,
  ),
];

const _wards = [
  Ward(id: 'w1', name: 'Panchakarma Ward', totalCapacity: 12, occupiedBeds: 7),
  Ward(id: 'w2', name: 'Women Ward', totalCapacity: 8, occupiedBeds: 8),
];

const _treatments = [
  Treatment(
    id: 't-1',
    nameEnglish: 'Panchakarma Detox',
    nameSinhala: 'පංචකර්ම',
    description: 'Classical detox',
    scheduleDays: ['Monday'],
  ),
];

final _complaints = [
  PatientComplaint(
    id: 'c1',
    subject: 'Long wait at the front desk',
    description: 'We waited forty minutes before the vaidya saw us.',
    priority: ComplaintPriority.normal,
    status: ComplaintStatus.open,
    createdAt: DateTime.utc(2026, 9, 20),
  ),
  PatientComplaint(
    id: 'c2',
    subject: 'Oil was too hot',
    description: 'The abhyanga oil was uncomfortably warm.',
    priority: ComplaintPriority.high,
    status: ComplaintStatus.escalated,
    createdAt: DateTime.utc(2026, 9, 21),
    escalatedAt: DateTime.utc(2026, 9, 22),
  ),
  PatientComplaint(
    id: 'c3',
    subject: 'Parking',
    description: 'No parking close to the entrance.',
    priority: ComplaintPriority.normal,
    status: ComplaintStatus.resolved,
    createdAt: DateTime.utc(2026, 9, 10),
  ),
  PatientComplaint(
    id: 'c4',
    subject: 'Billing question',
    description: 'Question about a charge.',
    priority: ComplaintPriority.normal,
    status: ComplaintStatus.inProgress,
    createdAt: DateTime.utc(2026, 9, 11),
  ),
];

final _notifications = [
  PatientNotification(
    id: 'n1',
    title: 'Reply to your feedback',
    message: 'The care team posted a reply.',
    kind: NotificationKind.reply,
    isRead: false,
    createdAt: DateTime.utc(2026, 9, 23, 8),
  ),
  PatientNotification(
    id: 'n2',
    title: 'Complaint escalated',
    message: 'Your concern was escalated.',
    kind: NotificationKind.escalation,
    isRead: true,
    createdAt: DateTime.utc(2026, 9, 23, 9),
  ),
];

final _publicFeed = [
  PublicFeedback(
    id: 'f1',
    patientName: 'Sunil Perera',
    isAnonymous: false,
    rating: 5,
    comment: 'Calm shirodhara and caring staff.',
    likeCount: 3,
    dislikeCount: 0,
    replies: const [],
    createdAt: DateTime.utc(2026, 9, 20),
  ),
];

final _myFeedback = [
  PatientFeedback(
    id: 'm1',
    rating: 4,
    comment: 'Good session, a short wait.',
    isAnonymous: false,
    status: 'Approved',
    createdAt: DateTime.utc(2026, 9, 20),
    canEdit: false,
  ),
];

List<Override> _communication({
  List<PublicFeedback>? feed,
  List<PatientFeedback>? mine,
  List<PatientComplaint>? complaints,
  List<PatientNotification>? notifications,
}) => [
  publicFeedProvider.overrideWith((ref) async => feed ?? _publicFeed),
  myFeedbackProvider.overrideWith((ref) async => mine ?? _myFeedback),
  myComplaintsProvider.overrideWith((ref) async => complaints ?? _complaints),
  notificationsProvider.overrideWith(
    (ref) async => notifications ?? _notifications,
  ),
];

List<Override> _homeOverrides({
  FutureOr<List<Appointment>> Function()? appointments,
  FutureOr<List<Treatment>> Function()? treatments,
}) => [
  authControllerProvider.overrideWith(_FakeAuth.new),
  myAppointmentsProvider.overrideWith(
    (ref) => appointments?.call() ?? _appointments,
  ),
  treatmentsProvider.overrideWith((ref) => treatments?.call() ?? _treatments),
  notificationsProvider.overrideWith((ref) async => _notifications),
];

void main() {
  group('shared widgets', () {
    testWidgets('skeleton bones pulse and stand still with animations off', (
      tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SkeletonScope(child: SkeletonBone(height: 20, width: 100)),
          ),
        ),
      );
      Color colorAt() {
        final box = tester.widget<Container>(find.byType(Container).first);
        return (box.decoration! as BoxDecoration).color!;
      }

      final first = colorAt();
      await tester.pump(const Duration(milliseconds: 500));
      expect(colorAt(), isNot(first));

      await tester.pumpWidget(
        const MediaQuery(
          data: MediaQueryData(disableAnimations: true),
          child: MaterialApp(
            home: Scaffold(
              body: SkeletonScope(child: SkeletonBone(height: 20, width: 100)),
            ),
          ),
        ),
      );
      final still = colorAt();
      await tester.pump(const Duration(milliseconds: 500));
      expect(colorAt(), still);
    });

    test('pageGutter centres content only when the window is wider', () {
      expect(pageGutter(360, 720, 20), 0);
      expect(pageGutter(720, 720, 20), 0);
      expect(pageGutter(1280, 720, 20), 260);
    });

    testWidgets('error and empty panels show an icon and a message', (
      tester,
    ) async {
      await _pump(
        tester,
        const Scaffold(
          body: Column(
            children: [
              Expanded(child: ErrorState(message: 'Could not load')),
              Expanded(child: EmptyState(message: 'Nothing here')),
            ],
          ),
        ),
        brightness: Brightness.dark,
      );
      expect(find.text('Could not load'), findsOneWidget);
      expect(find.text('Nothing here'), findsOneWidget);
      expect(find.byIcon(Icons.cloud_off_outlined), findsOneWidget);
    });
  });

  group('appointments', () {
    testWidgets('loading shows a skeleton, not a spinner', (tester) async {
      final pending = Completer<List<Appointment>>();
      await _pump(
        tester,
        const MyAppointmentsScreen(),
        overrides: [
          myAppointmentsProvider.overrideWith((ref) => pending.future),
        ],
        settle: false,
      );
      expect(find.byKey(AppointmentsScreenKeys.skeleton), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('error shows retry that reloads', (tester) async {
      var loads = 0;
      await _pump(
        tester,
        const MyAppointmentsScreen(),
        overrides: [
          myAppointmentsProvider.overrideWith((ref) async {
            loads += 1;
            throw Exception('offline');
          }),
        ],
      );
      expect(find.byKey(AppointmentsScreenKeys.retry), findsOneWidget);
      await tester.tap(find.byKey(AppointmentsScreenKeys.retry));
      await tester.pumpAndSettle();
      expect(loads, 2);
    });

    testWidgets('empty list offers to book', (tester) async {
      await _pump(
        tester,
        const MyAppointmentsScreen(),
        overrides: [
          myAppointmentsProvider.overrideWith((ref) async => const []),
        ],
      );
      expect(
        find.byKey(const ValueKey('empty-book-appointment')),
        findsOneWidget,
      );
    });

    testWidgets('cards use two columns on web and one on phones', (
      tester,
    ) async {
      final overrides = [
        myAppointmentsProvider.overrideWith((ref) async => _appointments),
      ];
      await _pump(
        tester,
        const MyAppointmentsScreen(),
        size: _sizes['web']!,
        overrides: overrides,
      );
      expect(
        find.byKey(const ValueKey('responsive-column-end')),
        findsOneWidget,
      );
      await _pump(
        tester,
        const MyAppointmentsScreen(),
        size: _sizes['phone']!,
        overrides: overrides,
      );
      expect(find.byKey(const ValueKey('responsive-column-end')), findsNothing);
    });

    _matrix('appointments', (tester, size, brightness) {
      return _pump(
        tester,
        const MyAppointmentsScreen(),
        size: size,
        brightness: brightness,
        overrides: [
          myAppointmentsProvider.overrideWith((ref) async => _appointments),
        ],
      );
    });
  });

  group('wards', () {
    testWidgets('loading shows a skeleton', (tester) async {
      final pending = Completer<List<Ward>>();
      await _pump(
        tester,
        const WardAvailabilityScreen(),
        overrides: [wardsProvider.overrideWith((ref) => pending.future)],
        settle: false,
      );
      expect(find.byKey(WardScreenKeys.skeleton), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('error shows retry that reloads', (tester) async {
      var loads = 0;
      await _pump(
        tester,
        const WardAvailabilityScreen(),
        overrides: [
          wardsProvider.overrideWith((ref) async {
            loads += 1;
            throw Exception('offline');
          }),
        ],
      );
      await tester.tap(find.byKey(WardScreenKeys.retry));
      await tester.pumpAndSettle();
      expect(loads, 2);
    });

    testWidgets('no wards shows an empty state above the request form', (
      tester,
    ) async {
      await _pump(
        tester,
        const WardAvailabilityScreen(),
        overrides: [wardsProvider.overrideWith((ref) async => const [])],
      );
      expect(find.byKey(WardScreenKeys.empty), findsOneWidget);
    });

    testWidgets('a full ward is marked with text and an icon', (tester) async {
      await _pump(
        tester,
        const WardAvailabilityScreen(),
        overrides: [wardsProvider.overrideWith((ref) async => _wards)],
        brightness: Brightness.dark,
      );
      expect(find.byIcon(Icons.block), findsOneWidget);
      expect(find.byIcon(Icons.check), findsOneWidget);
    });

    _matrix('wards', (tester, size, brightness) {
      return _pump(
        tester,
        const WardAvailabilityScreen(),
        size: size,
        brightness: brightness,
        overrides: [wardsProvider.overrideWith((ref) async => _wards)],
      );
    });
  });

  group('home', () {
    testWidgets('loading shows skeletons and no spinner bars', (tester) async {
      final pendingAppointments = Completer<List<Appointment>>();
      final pendingTreatments = Completer<List<Treatment>>();
      await _pump(
        tester,
        const HomeScreen(),
        overrides: _homeOverrides(
          appointments: () => pendingAppointments.future,
          treatments: () => pendingTreatments.future,
        ),
        settle: false,
      );
      expect(find.byType(LinearProgressIndicator), findsNothing);
      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(find.byType(SkeletonBone), findsWidgets);
    });

    testWidgets('appointments error shows a retry row, not the empty card', (
      tester,
    ) async {
      var loads = 0;
      await _pump(
        tester,
        const HomeScreen(),
        overrides: [
          ..._homeOverrides(),
          myAppointmentsProvider.overrideWith((ref) async {
            loads += 1;
            throw Exception('offline');
          }),
        ],
      );
      expect(find.text('No Upcoming Appointments'), findsNothing);
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();
      expect(loads, 2);
    });

    testWidgets('therapies error shows a retry card', (tester) async {
      var loads = 0;
      await _pump(
        tester,
        const HomeScreen(),
        overrides: [
          ..._homeOverrides(),
          treatmentsProvider.overrideWith((ref) async {
            loads += 1;
            throw Exception('offline');
          }),
        ],
      );
      await tester.ensureVisible(find.text('Try again'));
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();
      expect(loads, 2);
    });

    testWidgets('no therapies shows an empty card', (tester) async {
      await _pump(
        tester,
        const HomeScreen(),
        overrides: _homeOverrides(treatments: () => const <Treatment>[]),
      );
      expect(find.text('No therapies are listed yet.'), findsOneWidget);
    });

    testWidgets('quick actions use four columns on web and two on phones', (
      tester,
    ) async {
      await _pump(
        tester,
        const HomeScreen(),
        size: _sizes['web']!,
        overrides: _homeOverrides(),
      );
      final web = tester.getSize(find.byType(GridView));
      await _pump(
        tester,
        const HomeScreen(),
        size: _sizes['phone']!,
        overrides: _homeOverrides(),
      );
      final phone = tester.getSize(find.byType(GridView));
      // Four tiles in one row on web, two rows of two on phones.
      expect(web.height, lessThan(phone.height));
    });

    _matrix('home', (tester, size, brightness) {
      return _pump(
        tester,
        const HomeScreen(),
        size: size,
        brightness: brightness,
        overrides: _homeOverrides(),
      );
    });
  });

  group('feedback area', () {
    testWidgets('each section shows a skeleton while loading', (tester) async {
      final pendingFeed = Completer<List<PublicFeedback>>();
      final pendingMine = Completer<List<PatientFeedback>>();
      final pendingComplaints = Completer<List<PatientComplaint>>();
      final pendingNotices = Completer<List<PatientNotification>>();
      await _pump(
        tester,
        const FeedbackHubScreen(),
        overrides: [
          publicFeedProvider.overrideWith((ref) => pendingFeed.future),
          myFeedbackProvider.overrideWith((ref) => pendingMine.future),
          myComplaintsProvider.overrideWith((ref) => pendingComplaints.future),
          notificationsProvider.overrideWith((ref) => pendingNotices.future),
        ],
        settle: false,
      );
      expect(
        find.byKey(FeedbackKeys.feedSkeleton, skipOffstage: false),
        findsOneWidget,
      );
      expect(
        find.byKey(FeedbackKeys.mineSkeleton, skipOffstage: false),
        findsOneWidget,
      );
      expect(
        find.byKey(FeedbackKeys.complaintsSkeleton, skipOffstage: false),
        findsOneWidget,
      );
      expect(
        find.byKey(FeedbackKeys.notificationsSkeleton, skipOffstage: false),
        findsOneWidget,
      );
      expect(
        find.byType(CircularProgressIndicator, skipOffstage: false),
        findsNothing,
      );
    });

    testWidgets('each section shows retry on error', (tester) async {
      var feedLoads = 0;
      await _pump(
        tester,
        const FeedbackHubScreen(),
        overrides: [
          publicFeedProvider.overrideWith((ref) async {
            feedLoads += 1;
            throw Exception('offline');
          }),
          myFeedbackProvider.overrideWith((ref) async => throw Exception('x')),
          myComplaintsProvider.overrideWith(
            (ref) async => throw Exception('x'),
          ),
          notificationsProvider.overrideWith(
            (ref) async => throw Exception('x'),
          ),
        ],
      );
      expect(find.byKey(FeedbackKeys.feedRetry), findsOneWidget);
      await tester.tap(find.byKey(FeedbackKeys.feedRetry));
      await tester.pumpAndSettle();
      expect(feedLoads, 2);

      for (final key in [
        FeedbackKeys.hubMine,
        FeedbackKeys.hubComplaints,
        FeedbackKeys.hubNotifications,
      ]) {
        await _openTab(tester, key);
      }
      expect(find.byKey(FeedbackKeys.notificationsRetry), findsOneWidget);
    });

    testWidgets('hub tabs show the unread count and follow the section', (
      tester,
    ) async {
      await _pump(
        tester,
        const FeedbackHubScreen(section: 3),
        overrides: _communication(),
      );
      expect(find.text('Reply to your feedback'), findsOneWidget);
      // One unread notice among two.
      expect(find.text('1'), findsWidgets);
    });

    testWidgets('empty sections show friendly empty states', (tester) async {
      await _pump(
        tester,
        const FeedbackHubScreen(),
        overrides: _communication(
          feed: const [],
          mine: const [],
          complaints: const [],
          notifications: const [],
        ),
      );
      expect(
        find.text('No approved notes yet. They will appear here after review.'),
        findsOneWidget,
      );
      await _openTab(tester, FeedbackKeys.hubMine);
      expect(find.text('You have not shared feedback yet.'), findsOneWidget);
      await _openTab(tester, FeedbackKeys.hubComplaints);
      expect(find.text('You have not raised a concern yet.'), findsOneWidget);
      await _openTab(tester, FeedbackKeys.hubNotifications);
      expect(find.text('You are up to date.'), findsOneWidget);
    });

    _matrix('feedback hub', (tester, size, brightness) {
      return _pump(
        tester,
        const FeedbackHubScreen(),
        size: size,
        brightness: brightness,
        overrides: _communication(),
      );
    });

    for (final section in const {
      'public feed': PublicFeedbackFeedScreen(),
      'my feedback': MyFeedbackScreen(),
      'complaints': MyComplaintsScreen(),
      'notifications': NotificationsScreen(),
    }.entries) {
      _matrix('standalone ${section.key}', (tester, size, brightness) {
        return _pump(
          tester,
          section.value,
          size: size,
          brightness: brightness,
          overrides: _communication(),
        );
      });
    }

    testWidgets('complaint status pills carry an icon as well as color', (
      tester,
    ) async {
      await _pump(
        tester,
        const MyComplaintsScreen(),
        brightness: Brightness.dark,
        overrides: _communication(),
      );
      await tester.scrollUntilVisible(find.byIcon(Icons.priority_high), 200);
      expect(find.byIcon(Icons.priority_high), findsOneWidget);
      await tester.scrollUntilVisible(find.byIcon(Icons.check), 200);
      expect(find.byIcon(Icons.check), findsOneWidget);
      await tester.scrollUntilVisible(find.byIcon(Icons.schedule), 200);
      expect(find.byIcon(Icons.schedule), findsOneWidget);
    });
  });

  group('faq and contact', () {
    _matrix('faq', (tester, size, brightness) {
      return _pump(
        tester,
        const FaqScreen(),
        size: size,
        brightness: brightness,
      );
    });

    testWidgets('faq shows an empty state when nothing matches', (
      tester,
    ) async {
      await _pump(tester, const FaqScreen());
      await tester.enterText(find.byKey(FaqKeys.searchInput), 'zzzzzz');
      await tester.pumpAndSettle();
      expect(find.text('No matching questions found.'), findsOneWidget);
    });

    testWidgets('contact testimonials show a skeleton while loading', (
      tester,
    ) async {
      final pending = Completer<List<PublicFeedback>>();
      await _pump(
        tester,
        const ContactLocationScreen(),
        overrides: [publicFeedProvider.overrideWith((ref) => pending.future)],
        settle: false,
      );
      expect(find.byType(SkeletonCard), findsWidgets);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('contact testimonials error offers retry', (tester) async {
      var loads = 0;
      await _pump(
        tester,
        const ContactLocationScreen(),
        overrides: [
          publicFeedProvider.overrideWith((ref) async {
            loads += 1;
            throw Exception('offline');
          }),
        ],
      );
      await tester.ensureVisible(
        find.byKey(ContactLocationKeys.testimonialsRetry),
      );
      await tester.tap(find.byKey(ContactLocationKeys.testimonialsRetry));
      await tester.pumpAndSettle();
      expect(loads, 2);
    });

    _matrix('contact', (tester, size, brightness) {
      return _pump(
        tester,
        const ContactLocationScreen(),
        size: size,
        brightness: brightness,
        overrides: _communication(),
      );
    });
  });

  group('login and onboarding', () {
    _matrix('login', (tester, size, brightness) {
      return _pump(
        tester,
        const LoginScreen(),
        size: size,
        brightness: brightness,
        overrides: [authControllerProvider.overrideWith(_SignedOutAuth.new)],
      );
    });

    _matrix('onboarding', (tester, size, brightness) {
      return _pump(
        tester,
        const OnboardingScreen(),
        size: size,
        brightness: brightness,
        overrides: [authControllerProvider.overrideWith(_SignedOutAuth.new)],
      );
    });

    testWidgets('login register form fits a small phone in dark mode', (
      tester,
    ) async {
      await _pump(
        tester,
        const LoginScreen(),
        size: const Size(320, 568),
        brightness: Brightness.dark,
        overrides: [authControllerProvider.overrideWith(_SignedOutAuth.new)],
      );
      await tester.ensureVisible(find.byKey(LoginScreenKeys.modeToggle));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(LoginScreenKeys.modeToggle));
      await tester.pumpAndSettle();
      expect(find.byKey(LoginScreenKeys.fullName), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('onboarding fits a landscape phone with large text', (
      tester,
    ) async {
      tester.view.physicalSize = const Size(640, 360);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [authControllerProvider.overrideWith(_SignedOutAuth.new)],
          child: MaterialApp(
            theme: AppTheme.dark,
            locale: const Locale('en'),
            supportedLocales: AppLocalizations.supportedLocales,
            localizationsDelegates: AppLocalizations.localizationsDelegates,
            builder: (context, child) => MediaQuery(
              data: MediaQuery.of(
                context,
              ).copyWith(textScaler: const TextScaler.linear(1.4)),
              child: child!,
            ),
            home: const OnboardingScreen(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.byKey(OnboardingKeys.nextButton), findsOneWidget);
    });
  });
}
