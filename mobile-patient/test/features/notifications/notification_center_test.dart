import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/data/communication_repository.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_keys.dart';
import 'package:patient_app/src/features/feedback/presentation/notification_destination.dart';
import 'package:patient_app/src/features/feedback/presentation/notifications_screen.dart';
import 'package:patient_app/src/features/health_hub/presentation/health_hub_screen.dart';
import 'package:patient_app/src/features/home/presentation/home_screen.dart';
import 'package:patient_app/src/features/notifications/application/push_registration.dart';
import 'package:patient_app/src/features/notifications/data/push_token_source.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/router/app_routes.dart';
import 'package:patient_app/src/theme/app_theme.dart';

void main() {
  test('notification types map onto the related screens', () {
    expect(
      notificationDestination(NotificationKind.reply),
      AppRoutes.myFeedback,
    );
    expect(
      notificationDestination(NotificationKind.escalation),
      AppRoutes.complaints,
    );
    expect(
      notificationDestination(NotificationKind.appointmentRescheduled),
      AppRoutes.appointments,
    );
    expect(
      notificationDestination(NotificationKind.prescriptionIssued),
      '/profile/health-hub?tab=prescriptions',
    );
    expect(
      notificationDestination(NotificationKind.invoiceIssued),
      '/profile/health-hub?tab=invoices',
    );
    expect(notificationDestination(NotificationKind.general), isNull);
    expect(healthHubTabIndex('invoices'), 4);
    expect(healthHubTabIndex('unknown'), 0);

    final issued = PatientNotification.fromJson({
      'id': 'n-rx',
      'title': 'Prescription ready',
      'message': 'A vaidya issued your prescription.',
      'type': 'PrescriptionIssued',
      'isRead': false,
      'createdAt': '2026-10-02T08:00:00Z',
    });
    final numbered = PatientNotification.fromJson({
      'id': 'n-inv',
      'title': 'Invoice ready',
      'message': 'An invoice was issued.',
      'type': 11,
      'isRead': false,
      'createdAt': '2026-10-02T08:00:00Z',
    });
    expect(issued.kind, NotificationKind.prescriptionIssued);
    expect(numbered.kind, NotificationKind.invoiceIssued);
    expect(NotificationKind.fromWire('FeedbackAlert'), NotificationKind.general);
  });

  test('dev push source does not register a token', () async {
    var calls = 0;
    final service = PushRegistrationService(
      source: const DevPushTokenSource(),
      platform: () => 'android',
      register: ({required token, required platform}) async {
        calls++;
      },
    );

    expect(await service.registerIfAvailable(), isFalse);
    expect(calls, 0);
  });

  test('blank tokens and desktop platforms are not sent', () async {
    var calls = 0;
    Future<void> record({required String token, required String platform}) async {
      calls++;
    }

    final blank = PushRegistrationService(
      source: const _FixedToken('  '),
      platform: () => 'ios',
      register: record,
    );
    final desktop = PushRegistrationService(
      source: const _FixedToken('phone-token-1'),
      platform: () => null,
      register: record,
    );

    expect(await blank.registerIfAvailable(), isFalse);
    expect(await desktop.registerIfAvailable(), isFalse);
    expect(calls, 0);
  });

  test('a token is posted to the device-tokens endpoint', () async {
    final adapter = _ScriptedAdapter();
    final repository = CommunicationRepository(_dio(adapter));

    await repository.registerDeviceToken(
      token: 'phone-token-1',
      platform: 'android',
    );

    expect(adapter.last?.path, '/notifications/device-tokens');
    expect(adapter.last?.data, {
      'token': 'phone-token-1',
      'platform': 'android',
    });
  });

  testWidgets('signing in registers the token from the push source', (
    tester,
  ) async {
    final auth = _GateAuth();
    final posted = <String>[];
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(() => auth),
          pushRegistrationProvider.overrideWithValue(
            PushRegistrationService(
              source: const _FixedToken('phone-token-1'),
              platform: () => 'android',
              register: ({required token, required platform}) async {
                posted.add('$platform:$token');
              },
            ),
          ),
        ],
        child: const PushRegistrationHost(child: SizedBox.shrink()),
      ),
    );
    await tester.pump();
    expect(posted, isEmpty);

    auth.enter();
    await tester.pump();
    await tester.pump();

    expect(posted, ['android:phone-token-1']);
  });

  testWidgets('home shows an unread badge', (tester) async {
    await tester.pumpWidget(
      _app(
        const HomeScreen(),
        overrides: [
          authControllerProvider.overrideWith(_SignedInAuth.new),
          treatmentsProvider.overrideWith((ref) async => const []),
          myAppointmentsProvider.overrideWith(
            (ref) async => const <Appointment>[],
          ),
          notificationsProvider.overrideWith((ref) async => _sample()),
        ],
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.homeUnreadBadge), findsOneWidget);
    expect(find.text('2'), findsWidgets);
  });

  testWidgets('mark all read clears the unread badge', (tester) async {
    final repository = _RecordingRepository();
    await tester.pumpWidget(
      _app(
        const NotificationsScreen(),
        overrides: [
          communicationRepositoryProvider.overrideWithValue(repository),
          notificationsProvider.overrideWith((ref) async => _sample()),
        ],
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.unreadBadge), findsOneWidget);
    await tester.tap(find.byKey(FeedbackKeys.markAllRead));
    await tester.pumpAndSettle();

    expect(repository.markedAll, 1);
    expect(find.byKey(FeedbackKeys.unreadBadge), findsNothing);
    expect(find.byIcon(Icons.circle), findsNothing);
  });

  testWidgets('tapping a prescription notice marks it read and opens the tab', (
    tester,
  ) async {
    final repository = _RecordingRepository();
    final router = GoRouter(
      initialLocation: AppRoutes.notifications,
      routes: [
        GoRoute(
          path: AppRoutes.notifications,
          builder: (context, state) => const NotificationsScreen(),
        ),
        GoRoute(
          path: AppRoutes.healthHub,
          builder: (context, state) =>
              Text('hub-${state.uri.queryParameters['tab']}'),
        ),
        GoRoute(
          path: AppRoutes.appointments,
          builder: (context, state) => const Text('appointments-screen'),
        ),
      ],
    );
    addTearDown(router.dispose);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          communicationRepositoryProvider.overrideWithValue(repository),
          notificationsProvider.overrideWith(
            (ref) async => [
              _notice(
                id: 'rx-notice',
                title: 'Prescription ready',
                kind: NotificationKind.prescriptionIssued,
              ),
            ],
          ),
        ],
        child: MaterialApp.router(
          theme: AppTheme.light,
          locale: const Locale('en'),
          routerConfig: router,
          localizationsDelegates: _delegates,
          supportedLocales: AppLocalizations.supportedLocales,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(FeedbackKeys.notificationTile('rx-notice')));
    await tester.pumpAndSettle();

    expect(repository.readIds, ['rx-notice']);
    expect(find.text('hub-prescriptions'), findsOneWidget);
  });

  testWidgets('a failed mark-read stays on the notice', (tester) async {
    final repository = _RecordingRepository()..failRead = true;
    await tester.pumpWidget(
      _app(
        const NotificationsScreen(),
        overrides: [
          communicationRepositoryProvider.overrideWithValue(repository),
          notificationsProvider.overrideWith(
            (ref) async => [
              _notice(
                id: 'stay',
                title: 'Hospital notice',
                kind: NotificationKind.general,
              ),
            ],
          ),
        ],
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(FeedbackKeys.notificationTile('stay')));
    await tester.pumpAndSettle();

    expect(find.text('Hospital notice'), findsOneWidget);
    expect(find.byIcon(Icons.circle), findsOneWidget);
    expect(find.text('Something went wrong. Please try again.'), findsOneWidget);
  });
}

PatientNotification _notice({
  required String id,
  required String title,
  required NotificationKind kind,
  bool isRead = false,
}) {
  return PatientNotification(
    id: id,
    title: title,
    message: 'From the hospital.',
    kind: kind,
    isRead: isRead,
    createdAt: DateTime.utc(2026, 10, 2),
  );
}

List<PatientNotification> _sample() => [
  _notice(id: 'n1', title: 'Reply', kind: NotificationKind.reply),
  _notice(
    id: 'n2',
    title: 'Invoice ready',
    kind: NotificationKind.invoiceIssued,
  ),
  _notice(
    id: 'n3',
    title: 'Read already',
    kind: NotificationKind.general,
    isRead: true,
  ),
];

Widget _app(Widget home, {required List<Override> overrides}) {
  return ProviderScope(
    overrides: overrides,
    child: MaterialApp(
      theme: AppTheme.light,
      locale: const Locale('en'),
      localizationsDelegates: _delegates,
      supportedLocales: AppLocalizations.supportedLocales,
      home: home,
    ),
  );
}

const _delegates = [
  AppLocalizations.delegate,
  GlobalMaterialLocalizations.delegate,
  GlobalWidgetsLocalizations.delegate,
  GlobalCupertinoLocalizations.delegate,
];

class _FixedToken implements PushTokenSource {
  const _FixedToken(this.value);
  final String value;

  @override
  Future<String?> readToken() async => value;
}

class _RecordingRepository extends CommunicationRepository {
  _RecordingRepository() : super(Dio());

  final readIds = <String>[];
  var markedAll = 0;
  var failRead = false;

  @override
  Future<void> markNotificationRead(String id) async {
    if (failRead) throw StateError('down');
    readIds.add(id);
  }

  @override
  Future<void> markAllNotificationsRead() async {
    markedAll++;
  }
}

class _GateAuth extends AuthController {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);

  void enter() {
    state = const AuthState(
      status: AuthStatus.authenticated,
      user: AuthUser(
        id: 'user-1',
        fullName: 'Meera Nair',
        email: 'meera@example.com',
        phoneNumber: '0770000000',
        role: UserRole.patient,
      ),
    );
  }
}

class _SignedInAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
    status: AuthStatus.authenticated,
    user: AuthUser(
      id: 'user-1',
      fullName: 'Meera Nair',
      email: 'meera@example.com',
      phoneNumber: '0770000000',
      role: UserRole.patient,
    ),
  );
}

Dio _dio(HttpClientAdapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'https://localhost:7443/api'));
  dio.httpClientAdapter = adapter;
  return dio;
}

class _ScriptedAdapter implements HttpClientAdapter {
  RequestOptions? last;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    last = options;
    return ResponseBody.fromString(
      '{}',
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
