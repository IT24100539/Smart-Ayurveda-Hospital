import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/faq/presentation/faq_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

Widget _buildHarness({Locale locale = const Locale('en')}) {
  return MaterialApp(
    theme: AppTheme.light,
    locale: locale,
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: AppLocalizations.supportedLocales,
    home: const FaqScreen(),
  );
}

void main() {
  testWidgets('renders verified FAQ categories and questions', (tester) async {
    await tester.pumpWidget(_buildHarness());
    await tester.pumpAndSettle();

    expect(find.byKey(FaqKeys.bookingTile), findsOneWidget);
    expect(find.byKey(FaqKeys.rescheduleTile), findsOneWidget);
    expect(find.byKey(FaqKeys.cancelTile), findsOneWidget);
    expect(find.byKey(FaqKeys.charakaCapabilitiesTile), findsOneWidget);

    expect(find.text('How do I request an appointment?'), findsOneWidget);
    expect(find.text('Can I reschedule my appointment?'), findsOneWidget);
    expect(find.text('How do I cancel an appointment?'), findsOneWidget);
    expect(
      find.text('What information can Charaka AI provide?'),
      findsOneWidget,
    );
  });

  testWidgets('tapping an FAQ question expands verified answer', (
    tester,
  ) async {
    await tester.pumpWidget(_buildHarness());
    await tester.pumpAndSettle();

    // Tap on Charaka AI limitations
    await tester.tap(find.byKey(FaqKeys.charakaLimitationsTile));
    await tester.pumpAndSettle();

    // Verify answer contains safety policy details
    expect(
      find.textContaining('Charaka is an informational assistant'),
      findsOneWidget,
    );
    expect(
      find.textContaining('cannot prescribe treatments or diagnose illnesses'),
      findsOneWidget,
    );
  });

  testWidgets('search bar filters questions accurately', (tester) async {
    await tester.pumpWidget(_buildHarness());
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(FaqKeys.searchInput), 'reschedule');
    await tester.pumpAndSettle();

    expect(find.byKey(FaqKeys.rescheduleTile), findsOneWidget);
    expect(find.byKey(FaqKeys.wardsTile), findsNothing);
  });
}
