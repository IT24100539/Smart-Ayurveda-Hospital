import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/feedback/presentation/feedback_keys.dart';
import 'package:patient_app/src/features/feedback/presentation/notifications_screen.dart';
import 'package:patient_app/src/features/feedback/presentation/star_rating.dart';
import 'package:patient_app/src/features/feedback/presentation/submit_feedback_screen.dart';
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
        overrides: [authControllerProvider.overrideWith(_NamedAuth.new)],
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
}

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
