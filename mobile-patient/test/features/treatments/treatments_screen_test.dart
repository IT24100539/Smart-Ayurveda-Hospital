import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/data/treatments_repository.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart';
import 'package:patient_app/src/features/treatments/presentation/treatments_screen.dart';
import 'package:patient_app/src/features/treatments/presentation/widgets/ask_treatment_card.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

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

class _UnauthenticatedAuth extends AuthController {
  @override
  AuthState build() =>
      const AuthState(status: AuthStatus.unauthenticated);
}

class _FakeTreatmentsRepository implements TreatmentsRepository {
  String? lastQuestion;

  @override
  Future<TreatmentAskResult> askTreatmentInfo(String question) async {
    lastQuestion = question;
    return const TreatmentAskResult(
      answer: 'Abhyanga is listed on Monday and Wednesday.',
      matchedTreatmentIds: ['1'],
      refused: false,
      workflowId: 'wf-1',
    );
  }

  @override
  Future<TreatmentAvailability> checkAvailability(String id, String date) {
    throw UnimplementedError();
  }

  @override
  Future<PaginatedResponse<Treatment>> getTreatments() {
    throw UnimplementedError();
  }
}

double _contrastRatio(Color foreground, Color background) {
  double luminance(Color color) {
    final argb = color.toARGB32();
    double channel(int shift) {
      final value = ((argb >> shift) & 0xFF) / 255;
      if (value <= 0.04045) return value / 12.92;
      return math.pow((value + 0.055) / 1.055, 2.4).toDouble();
    }

    return 0.2126 * channel(16) + 0.7152 * channel(8) + 0.0722 * channel(0);
  }

  final lighter = math.max(luminance(foreground), luminance(background));
  final darker = math.min(luminance(foreground), luminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

const _mockTreatments = [
  Treatment(
    id: '1',
    nameSinhala: 'පංචකර්ම',
    nameEnglish: 'Panchakarma',
    description: 'A five-fold detoxification treatment.',
    scheduleDays: ['Monday', 'Wednesday', 'Friday'],
    therapistName: 'Dr. Silva',
  ),
  Treatment(
    id: '2',
    nameSinhala: 'ශිරෝධාරා',
    nameEnglish: 'Shirodhara',
    description: 'Oil pouring on forehead.',
    scheduleDays: ['Tuesday', 'Thursday'],
    therapistName: 'Dr. Perera',
  ),
];

Widget _app(Widget home, {ThemeData? theme}) {
  return MaterialApp(
    theme: theme ?? AppTheme.light,
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: const [Locale('en', '')],
    home: home,
  );
}

void main() {
  testWidgets('TreatmentsScreen renders search bar and treatment cards', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(800, 2000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          treatmentsProvider.overrideWith((ref) => Future.value(_mockTreatments)),
          authControllerProvider.overrideWith(_UnauthenticatedAuth.new),
        ],
        child: _app(const TreatmentsScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Ask about treatments'), findsOneWidget);
    expect(
      find.text('Sign in to ask about therapies, days, and fees.'),
      findsOneWidget,
    );
    expect(find.text('Search treatments...'), findsOneWidget);

    expect(find.text('පංචකර්ම'), findsOneWidget);
    expect(find.text('Panchakarma'), findsOneWidget);
    expect(find.text('ශිරෝධාරා'), findsOneWidget);
    expect(find.text('Shirodhara'), findsOneWidget);

    expect(find.text('Mon'), findsOneWidget);
    expect(find.text('Tue'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'shiro');
    await tester.pumpAndSettle();

    expect(find.text('Panchakarma'), findsNothing);
    expect(find.text('Shirodhara'), findsOneWidget);
  });

  testWidgets('signed-in patient can ask and see the agent reply', (
    tester,
  ) async {
    final repository = _FakeTreatmentsRepository();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_SignedInAuth.new),
          treatmentsRepositoryProvider.overrideWithValue(repository),
        ],
        child: _app(
          const Scaffold(
            body: SingleChildScrollView(child: AskTreatmentCard()),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(
      find.byKey(const Key('ask-treatment-question')),
      'When is Abhyanga available?',
    );
    await tester.pump();
    await tester.tap(find.byKey(const Key('ask-treatment-submit')));
    await tester.pumpAndSettle();

    expect(repository.lastQuestion, 'When is Abhyanga available?');
    expect(
      find.text('Abhyanga is listed on Monday and Wednesday.'),
      findsOneWidget,
    );
  });

  testWidgets('agent reply stays readable in dark mode', (tester) async {
    final repository = _FakeTreatmentsRepository();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_SignedInAuth.new),
          treatmentsRepositoryProvider.overrideWithValue(repository),
        ],
        child: _app(
          const Scaffold(
            body: SingleChildScrollView(child: AskTreatmentCard()),
          ),
          theme: AppTheme.dark,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(
      find.byKey(const Key('ask-treatment-question')),
      'When is Abhyanga available?',
    );
    await tester.pump();
    await tester.tap(find.byKey(const Key('ask-treatment-submit')));
    await tester.pumpAndSettle();

    final scheme = AppTheme.dark.colorScheme;
    final reply = tester.widget<DecoratedBox>(
      find.byKey(const Key('ask-treatment-reply')),
    );
    final decoration = reply.decoration as BoxDecoration;
    expect(decoration.color, scheme.primaryContainer);
    expect(decoration.color, isNot(AyurvedaColors.sageMuted));

    final answer = tester.widget<Text>(
      find.text('Abhyanga is listed on Monday and Wednesday.'),
    );
    expect(answer.style?.color, scheme.onSurface);
    expect(
      _contrastRatio(scheme.onSurface, scheme.primaryContainer),
      greaterThanOrEqualTo(4.5),
    );
  });
}
