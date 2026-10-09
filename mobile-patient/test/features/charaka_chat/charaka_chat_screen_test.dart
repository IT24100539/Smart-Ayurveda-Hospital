import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/api_exception.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/charaka_chat/data/charaka_chat_repository.dart';
import 'package:patient_app/src/features/charaka_chat/presentation/charaka_chat_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

class _FakeAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
        status: AuthStatus.authenticated,
        user: AuthUser(
          id: 'patient-123',
          fullName: 'Meera Nair',
          email: 'meera.nair@example.local',
          phoneNumber: '9876500001',
          role: UserRole.patient,
        ),
      );
}

class _FakeCharakaChatRepository implements CharakaChatRepository {
  _FakeCharakaChatRepository({
    this.treatmentAnswer,
    this.patientAnswer,
    this.error,
  });

  CharakaAnswer? treatmentAnswer;
  CharakaAnswer? patientAnswer;
  Object? error;
  Completer<CharakaAnswer>? treatmentCompleter;
  Completer<CharakaAnswer>? patientCompleter;

  final List<String> treatmentCalls = [];
  final List<String> patientCalls = [];

  @override
  Future<CharakaAnswer> askTreatment(
    String question, {
    List<CharakaChatTurn> history = const [],
  }) async {
    treatmentCalls.add(question);
    if (treatmentCompleter != null) return treatmentCompleter!.future;
    if (error != null) throw error!;
    return treatmentAnswer ??
        CharakaAnswer(
          answer: 'Panchakarma is offered Mon-Fri. The complete therapy cycle fee is LKR 12,000.',
          refused: false,
          workflowId: 'wf-treat-1',
        );
  }

  @override
  Future<CharakaAnswer> askPatient(String question) async {
    patientCalls.add(question);
    if (patientCompleter != null) return patientCompleter!.future;
    if (error != null) throw error!;
    return patientAnswer ??
        CharakaAnswer(
          answer: 'Your registered UHID is SAH-2026-00007 under Western Province.',
          refused: false,
          workflowId: 'wf-pat-1',
        );
  }
}

Widget _buildHarness(
  _FakeCharakaChatRepository repository, {
  Locale locale = const Locale('en'),
}) {
  return ProviderScope(
    overrides: [
      charakaChatRepositoryProvider.overrideWithValue(repository),
      authControllerProvider.overrideWith(_FakeAuth.new),
    ],
    child: MaterialApp(
      theme: AppTheme.light,
      locale: locale,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: const CharakaChatScreen(),
    ),
  );
}

void main() {
  testWidgets('renders message bubbles, disclaimer, and suggested prompts', (
    tester,
  ) async {
    final repo = _FakeCharakaChatRepository();
    await tester.pumpWidget(_buildHarness(repo));
    await tester.pumpAndSettle();

    // Verify title and disclaimer
    expect(find.text('Charaka AI Assistant'), findsOneWidget);
    expect(
      find.textContaining('Charaka provides general information'),
      findsOneWidget,
    );

    // Verify initial welcome bubble
    expect(
      find.textContaining('Ayubowan! I am Charaka, your hospital assistant.'),
      findsOneWidget,
    );

    // Verify suggested prompts
    expect(
      find.text('What therapies do you offer for stress and relaxation?'),
      findsOneWidget,
    );

    // Verify input field and send button
    expect(find.byKey(CharakaChatKeys.input), findsOneWidget);
    expect(find.byKey(CharakaChatKeys.sendButton), findsOneWidget);
  });

  testWidgets('submitting a treatment question receives assistant answer', (
    tester,
  ) async {
    final completer = Completer<CharakaAnswer>();
    final repo = _FakeCharakaChatRepository()..treatmentCompleter = completer;

    await tester.pumpWidget(_buildHarness(repo));
    await tester.pumpAndSettle();

    // Type question and send
    await tester.enterText(
      find.byKey(CharakaChatKeys.input),
      'What is the price of Panchakarma?',
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pump(); // Render loading state

    expect(find.byKey(CharakaChatKeys.typingIndicator), findsOneWidget);

    completer.complete(
      const CharakaAnswer(
        answer: 'Panchakarma is offered Mon-Fri. The complete therapy cycle fee is LKR 12,000.',
        refused: false,
        workflowId: 'wf-1',
      ),
    );
    await tester.pumpAndSettle(); // Complete request

    expect(repo.treatmentCalls, contains('What is the price of Panchakarma?'));
    expect(
      find.text('What is the price of Panchakarma?'),
      findsOneWidget,
    );
    expect(
      find.text(
        'Panchakarma is offered Mon-Fri. The complete therapy cycle fee is LKR 12,000.',
      ),
      findsOneWidget,
    );
  });

  testWidgets('an outside-Ayurveda reply stays in the conversation without a medical badge', (
    tester,
  ) async {
    final repo = _FakeCharakaChatRepository(
      treatmentAnswer: const CharakaAnswer(
        answer:
            'That sits outside Ayurveda. Ask me about doshas, food, herbs, or panchakarma.',
        refused: true,
        workflowId: 'wf-refused',
      ),
    );

    await tester.pumpWidget(_buildHarness(repo));
    await tester.pumpAndSettle();

    await tester.enterText(
      find.byKey(CharakaChatKeys.input),
      'Who won the world cup?',
    );
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    expect(find.byKey(CharakaChatKeys.refusalBadge), findsNothing);
    expect(
      find.textContaining('That sits outside Ayurveda.'),
      findsOneWidget,
    );
  });

  testWidgets('network failure displays unreachable/offline banner and inline retry button', (
    tester,
  ) async {
    final repo = _FakeCharakaChatRepository(
      error: const ApiException(
        statusCode: null,
        detail: 'Connection refused',
        isNetworkError: true,
      ),
    );

    await tester.pumpWidget(_buildHarness(repo));
    await tester.pumpAndSettle();

    await tester.enterText(
      find.byKey(CharakaChatKeys.input),
      'Tell me about Abhyanga',
    );
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    // Offline banner should appear
    expect(find.byKey(CharakaChatKeys.offlineBanner), findsOneWidget);
    expect(
      find.textContaining('AI assistant is currently unreachable'),
      findsOneWidget,
    );

    // Inline retry button should be visible on the failed message
    expect(find.byKey(CharakaChatKeys.retryButton), findsOneWidget);

    // Now make repository succeed and tap retry
    repo.error = null;
    repo.treatmentAnswer = const CharakaAnswer(
      answer: 'Abhyanga is a traditional warm herbal oil massage.',
      refused: false,
      workflowId: 'wf-success',
    );

    await tester.tap(find.byKey(CharakaChatKeys.retryButton));
    await tester.pumpAndSettle();

    expect(repo.treatmentCalls.length, 2);
    expect(
      find.text('Abhyanga is a traditional warm herbal oil massage.'),
      findsOneWidget,
    );
    expect(find.byKey(CharakaChatKeys.retryButton), findsNothing);
  });

  testWidgets(
    'switching topic to My Patient Info submits to patient info workflow',
    (tester) async {
      final repo = _FakeCharakaChatRepository(
        patientAnswer: const CharakaAnswer(
          answer:
              'Your registered UHID is SAH-2026-00007 under Western Province.',
          refused: false,
          workflowId: 'wf-patient-info',
        ),
      );

      await tester.pumpWidget(_buildHarness(repo));
      await tester.pumpAndSettle();

      // Switch topic to My Patient Info
      await tester.tap(find.text('My Patient Info'));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.byKey(CharakaChatKeys.input),
        'What is my UHID?',
      );
      await tester.tap(find.byKey(CharakaChatKeys.sendButton));
      await tester.pumpAndSettle();

      expect(repo.patientCalls, contains('What is my UHID?'));
      expect(
        find.text(
          'Your registered UHID is SAH-2026-00007 under Western Province.',
        ),
        findsOneWidget,
      );
    },
  );

  testWidgets('patient-record medical refusals still show the safety badge', (
    tester,
  ) async {
    final repo = _FakeCharakaChatRepository(
      patientAnswer: const CharakaAnswer(
        answer: 'I cannot give medical advice about your record.',
        refused: true,
        workflowId: 'wf-patient-refused',
      ),
    );

    await tester.pumpWidget(_buildHarness(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.text('My Patient Info'));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(CharakaChatKeys.input),
      'Should I take medicine for this pain?',
    );
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    expect(find.byKey(CharakaChatKeys.refusalBadge), findsOneWidget);
    expect(find.text('Medical advice refused'), findsOneWidget);
  });

  testWidgets(
    '500 server error displays error bubble with retry and no offline banner',
    (tester) async {
      final repo = _FakeCharakaChatRepository(
        error: const ApiException(
          statusCode: 500,
          detail: 'Internal server error',
          isNetworkError: false,
        ),
      );

      await tester.pumpWidget(_buildHarness(repo));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.byKey(CharakaChatKeys.input),
        'Tell me about therapies',
      );
      await tester.tap(find.byKey(CharakaChatKeys.sendButton));
      await tester.pumpAndSettle();

      // Offline banner must NOT be displayed
      expect(find.byKey(CharakaChatKeys.offlineBanner), findsNothing);

      // Normal error bubble with retry button should be visible
      expect(find.byKey(CharakaChatKeys.retryButton), findsOneWidget);
      expect(find.text('Internal server error'), findsOneWidget);
    },
  );

  testWidgets(
    '400 bad request error displays error bubble with retry and no offline banner',
    (tester) async {
      final repo = _FakeCharakaChatRepository(
        error: const ApiException(
          statusCode: 400,
          detail: 'Question cannot be empty',
          isNetworkError: false,
        ),
      );

      await tester.pumpWidget(_buildHarness(repo));
      await tester.pumpAndSettle();

      await tester.enterText(
        find.byKey(CharakaChatKeys.input),
        'Invalid inquiry',
      );
      await tester.tap(find.byKey(CharakaChatKeys.sendButton));
      await tester.pumpAndSettle();

      // Offline banner must NOT be displayed
      expect(find.byKey(CharakaChatKeys.offlineBanner), findsNothing);

      // Normal error bubble with retry button should be visible
      expect(find.byKey(CharakaChatKeys.retryButton), findsOneWidget);
      expect(find.text('Question cannot be empty'), findsOneWidget);
    },
  );
}

