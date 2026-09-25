import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart';
import 'package:patient_app/src/features/treatments/presentation/treatments_screen.dart';

void main() {
  testWidgets('TreatmentsScreen renders search bar and treatment cards', (tester) async {
    final mockTreatments = [
      const Treatment(
        id: '1',
        nameSinhala: 'පංචකර්ම',
        nameEnglish: 'Panchakarma',
        description: 'A five-fold detoxification treatment.',
        scheduleDays: ['Monday', 'Wednesday', 'Friday'],
        therapistName: 'Dr. Silva',
      ),
      const Treatment(
        id: '2',
        nameSinhala: 'ශිරෝධාරා',
        nameEnglish: 'Shirodhara',
        description: 'Oil pouring on forehead.',
        scheduleDays: ['Tuesday', 'Thursday'],
        therapistName: 'Dr. Perera',
      ),
    ];

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          treatmentsProvider.overrideWith((ref) => Future.value(mockTreatments)),
        ],
        child: const MaterialApp(
          localizationsDelegates: [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: [
            Locale('en', ''),
          ],
          home: TreatmentsScreen(),
        ),
      ),
    );

    // Wait for future to resolve
    await tester.pumpAndSettle();

    // Search bar should be present
    expect(find.byType(TextField), findsOneWidget);
    expect(find.text('Search treatments...'), findsOneWidget);

    // Cards should be present
    expect(find.text('පංචකර්ම'), findsOneWidget);
    expect(find.text('Panchakarma'), findsOneWidget);
    expect(find.text('ශිරෝධාරා'), findsOneWidget);
    expect(find.text('Shirodhara'), findsOneWidget);

    // Day tags should be present
    expect(find.text('Mon'), findsOneWidget);
    expect(find.text('Tue'), findsOneWidget);

    // Test filtering
    await tester.enterText(find.byType(TextField), 'shiro');
    await tester.pumpAndSettle();

    expect(find.text('Panchakarma'), findsNothing);
    expect(find.text('Shirodhara'), findsOneWidget);
  });
}
