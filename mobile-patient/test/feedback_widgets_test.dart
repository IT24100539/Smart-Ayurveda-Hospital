import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_hub_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_keys.dart';
import 'package:patient_app/src/features/feedback/presentation/my_feedback_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/notifications_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/star_rating.dart';
import 'package:patient_app/src/features/feedback/presentation/submit_feedback_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';

void main() {
  testWidgets('star rating widget records the tapped value', (tester) async {
    var recorded = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StarRating(
            value: recorded,
            onChanged: (value) => recorded = value,
          ),
        ),
      ),
    );

    await tester.tap(find.byKey(FeedbackKeys.star(4)));
    expect(recorded, 4);
  });

  testWidgets('anonymous toggle hides the name field preview', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_NamedAuth.new),
          ..._emptyLinkOverrides(),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: SubmitFeedbackScreen(
            appointmentId: 'appt-1',
            treatmentId: 'treat-1',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.namePreview), findsOneWidget);
    expect(find.text('Nimal Perera'), findsOneWidget);

    await tester.ensureVisible(find.byKey(FeedbackKeys.anonymousToggle));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(FeedbackKeys.anonymousToggle));
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.namePreview), findsNothing);
    expect(find.text('Nimal Perera'), findsNothing);
  });

  testWidgets('notification list shows the unread count badge', (tester) async {
    final now = DateTime.utc(2026, 9, 23, 8);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationsProvider.overrideWith(
            (ref) async => [
              PatientNotification(
                id: 'n1',
                title: 'Reply to your feedback',
                message: 'The care team posted a reply.',
                kind: NotificationKind.reply,
                isRead: false,
                createdAt: now,
              ),
              PatientNotification(
                id: 'n2',
                title: 'Complaint update',
                message: 'Your complaint is now in progress.',
                kind: NotificationKind.statusChange,
                isRead: false,
                createdAt: now,
              ),
              PatientNotification(
                id: 'n3',
                title: 'Complaint escalated',
                message: 'Your concern was escalated for hospital review.',
                kind: NotificationKind.escalation,
                isRead: true,
                createdAt: now,
              ),
            ],
          ),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: NotificationsScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.unreadBadge), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.byIcon(Icons.reply_outlined), findsOneWidget);
    expect(find.byIcon(Icons.update), findsOneWidget);
    expect(find.byIcon(Icons.priority_high), findsOneWidget);
  });

  testWidgets('submit form requires a star rating', (tester) async {
    await _pumpSubmit(
      tester,
      const SubmitFeedbackScreen(treatmentId: 'treat-1'),
    );

    await tester.tap(find.byKey(FeedbackKeys.linkTreatment));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(FeedbackKeys.comment), 'The steam was hot.');
    await tester.ensureVisible(find.byKey(FeedbackKeys.submit));
    await tester.tap(find.byKey(FeedbackKeys.submit));
    await tester.pumpAndSettle();

    expect(find.text('Choose a star rating'), findsOneWidget);
  });

  testWidgets('submit form requires a visit or a treatment', (tester) async {
    await _pumpSubmit(tester, const SubmitFeedbackScreen());

    await tester.tap(find.byKey(FeedbackKeys.star(5)));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(FeedbackKeys.comment), 'Calm shirodhara.');
    await tester.ensureVisible(find.byKey(FeedbackKeys.submit));
    await tester.tap(find.byKey(FeedbackKeys.submit));
    await tester.pumpAndSettle();

    expect(find.text('Choose a completed visit or a treatment.'), findsOneWidget);
    expect(find.text('No completed visit yet. You can still write about a treatment.'), findsOneWidget);
  });

  testWidgets('feedback hub switches between the four sections', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          publicFeedProvider.overrideWith((ref) async => const <PublicFeedback>[]),
          myFeedbackProvider.overrideWith((ref) async => const <PatientFeedback>[]),
          myComplaintsProvider.overrideWith((ref) async => const <PatientComplaint>[]),
          notificationsProvider.overrideWith(
            (ref) async => [
              PatientNotification(
                id: 'n1',
                title: 'Reply',
                message: 'A reply is waiting.',
                kind: NotificationKind.reply,
                isRead: false,
                createdAt: DateTime.utc(2026, 9, 20),
              ),
            ],
          ),
          ..._emptyLinkOverrides(),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: FeedbackHubScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(FeedbackKeys.writeFeedback), findsWidgets);
    expect(find.text('Community'), findsOneWidget);

    await tester.tap(find.byKey(FeedbackKeys.hubMine));
    await tester.pumpAndSettle();
    expect(find.text('You have not shared feedback yet.'), findsOneWidget);

    await tester.tap(find.byKey(FeedbackKeys.hubComplaints));
    await tester.pumpAndSettle();
    expect(find.text('You have not raised a concern yet.'), findsOneWidget);

    await tester.tap(find.byKey(FeedbackKeys.hubNotifications));
    await tester.pumpAndSettle();
    expect(find.text('Reply'), findsOneWidget);
    expect(find.text('1'), findsWidgets);
  });

  testWidgets('my feedback shows care team replies and the withdraw button', (
    tester,
  ) async {
    final now = DateTime.now().toUtc();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          myFeedbackProvider.overrideWith(
            (ref) async => [
              PatientFeedback(
                id: 'fb-1',
                rating: 2,
                comment: 'The abhyanga wait ran long.',
                isAnonymous: true,
                status: 'PendingModeration',
                createdAt: now,
                canEdit: true,
                replies: [
                  PublicReply(
                    id: 'reply-1',
                    role: ReplyRole.staff,
                    reply: 'Namaste. A vaidya will review the wait.',
                    createdAt: now,
                  ),
                ],
              ),
            ],
          ),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: MyFeedbackScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Replies'), findsOneWidget);
    expect(find.text('Care team'), findsOneWidget);
    expect(find.text('Namaste. A vaidya will review the wait.'), findsOneWidget);
    expect(find.text('Withdraw'), findsOneWidget);
    expect(find.text('No replies yet.'), findsNothing);
  });

  testWidgets('withdrawn feedback shows Withdrawn and hides the withdraw button', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          myFeedbackProvider.overrideWith(
            (ref) async => [
              PatientFeedback(
                id: 'fb-2',
                rating: 3,
                comment: 'Please withdraw this note about the waiting area.',
                isAnonymous: false,
                status: 'Withdrawn',
                createdAt: DateTime.utc(2026, 9, 20),
                canEdit: false,
              ),
            ],
          ),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: MyFeedbackScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Withdrawn'), findsOneWidget);
    expect(find.text('Withdraw'), findsNothing);
    expect(find.text('Replies'), findsOneWidget);
    expect(find.text('No replies yet.'), findsOneWidget);
  });

  test('notification JSON keeps the care-team reply', () {
    final named = PatientNotification.fromJson({
      'id': 'n1',
      'title': 'Reply to your feedback',
      'message': 'Namaste. A vaidya will review the wait.',
      'type': 'FeedbackReply',
      'isRead': false,
      'createdAt': '2026-09-28T09:00:00Z',
    });
    final numbered = PatientNotification.fromJson({
      'id': 'n2',
      'title': 'Reply to your feedback',
      'message': 'Posted.',
      'type': 1,
      'isRead': false,
      'createdAt': '2026-09-28T09:00:00Z',
    });

    expect(named.kind, NotificationKind.reply);
    expect(numbered.kind, NotificationKind.reply);
    expect(named.title, 'Reply to your feedback');
  });

  testWidgets('notifications show an empty state only when the list is empty', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          notificationsProvider.overrideWith((ref) async => const <PatientNotification>[]),
        ],
        child: const MaterialApp(
          locale: Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: NotificationsScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('You are up to date.'), findsOneWidget);
    expect(find.text('Try again'), findsNothing);
  });
}

Future<void> _pumpSubmit(WidgetTester tester, Widget home) {
  return tester.pumpWidget(
    ProviderScope(
      overrides: [
        authControllerProvider.overrideWith(_NamedAuth.new),
        ..._emptyLinkOverrides(),
      ],
      child: MaterialApp(
        locale: const Locale('en'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: home,
      ),
    ),
  ).then((_) => tester.pumpAndSettle());
}

List<Override> _emptyLinkOverrides() => [
  myAppointmentsProvider.overrideWith((ref) async => const <Appointment>[]),
  treatmentsProvider.overrideWith(
    (ref) async => const [
      Treatment(
        id: 'treat-1',
        nameSinhala: 'ශිරෝධාරා',
        nameEnglish: 'Shirodhara',
        description: 'Oil poured on the forehead.',
        scheduleDays: ['Monday'],
      ),
    ],
  ),
];

class _NamedAuth extends AuthController {
  @override
  AuthState build() {
    return const AuthState(
      status: AuthStatus.authenticated,
      user: AuthUser(
        id: 'user-1',
        fullName: 'Nimal Perera',
        email: 'nimal@example.com',
        phoneNumber: '0770000000',
        role: UserRole.patient,
      ),
    );
  }
}
