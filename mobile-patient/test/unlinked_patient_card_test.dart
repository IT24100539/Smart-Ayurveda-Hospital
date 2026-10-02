import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/shared/widgets/unlinked_patient_card.dart';

void main() {
  group('UnlinkedPatientCard', () {
    testWidgets('renders in light mode with contrast-safe styling and actions', (
      tester,
    ) async {
      var retryClicked = false;

      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.light(useMaterial3: true),
          locale: const Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: Scaffold(
            body: UnlinkedPatientCard(
              onRetry: () => retryClicked = true,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.no_accounts_rounded), findsOneWidget);
      expect(find.text('No patient record is linked to this login.'), findsOneWidget);
      expect(
        find.text(
          'The clinical record must use the same email address. Please contact reception or update your profile to link your clinical record.',
        ),
        findsOneWidget,
      );
      expect(find.text('Check again'), findsOneWidget);

      await tester.tap(find.text('Check again'));
      expect(retryClicked, isTrue);
    });

    testWidgets('renders in dark mode without crashing', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.dark(useMaterial3: true),
          locale: const Locale('en'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: const Scaffold(
            body: UnlinkedPatientCard(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.no_accounts_rounded), findsOneWidget);
      expect(find.text('No patient record is linked to this login.'), findsOneWidget);
    });

    testWidgets('renders localized content in Sinhala (SI)', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('si'),
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          home: const Scaffold(
            body: UnlinkedPatientCard(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('මෙම ගිණුමට සම්බන්ධ රෝගී වාර්තාවක් හමු නොවීය.'), findsOneWidget);
      expect(
        find.text(
          'සායනික වාර්තාව එකම විද්‍යුත් තැපැල් ලිපිනය භාවිතා කළ යුතුය. කරුණාකර වාර්තාව සම්බන්ධ කිරීමට රෝහල් පිළිගැනීමේ කවුන්ටරය අමතන්න.',
        ),
        findsOneWidget,
      );
    });
  });
}
