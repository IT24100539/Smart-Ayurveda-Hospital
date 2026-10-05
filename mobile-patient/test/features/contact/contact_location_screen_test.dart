import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/contact/presentation/contact_location_screen.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

Widget _buildHarness({
  List<PublicFeedback> feedbackList = const [],
  Locale locale = const Locale('en'),
}) {
  return ProviderScope(
    overrides: [
      publicFeedProvider.overrideWith((ref) async => feedbackList),
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
      home: const ContactLocationScreen(),
    ),
  );
}

void main() {
  testWidgets('renders verified hospital details from l10n strings', (
    tester,
  ) async {
    await tester.pumpWidget(_buildHarness());
    await tester.pumpAndSettle();

    // Verify hospital name
    expect(find.byKey(ContactLocationKeys.hospitalName), findsOneWidget);
    expect(find.text('Smart Ayurveda Hospital'), findsOneWidget);

    // Verify hospital address
    expect(find.byKey(ContactLocationKeys.hospitalAddress), findsOneWidget);
    expect(find.text('Pallekele, Kundasale 20168'), findsOneWidget);

    // Verify hospital phone
    expect(find.byKey(ContactLocationKeys.hospitalPhone), findsOneWidget);
    expect(find.text('+94 81 242 0541'), findsOneWidget);

    // Verify hospital hours
    expect(find.byKey(ContactLocationKeys.hospitalHours), findsOneWidget);
    expect(find.text('Mon-Fri 8:00 AM - 5:30 PM'), findsOneWidget);
  });

  testWidgets(
    'renders verified empty state when no approved feedback is returned',
    (tester) async {
      await tester.pumpWidget(_buildHarness(feedbackList: const []));
      await tester.pumpAndSettle();

      expect(find.byKey(ContactLocationKeys.testimonialsSection), findsOneWidget);
      expect(
        find.text('No approved notes yet. They will appear here after review.'),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'renders real feedback items when approved testimonials exist',
    (tester) async {
      final realFeedback = [
        PublicFeedback(
          id: 'fb-1',
          patientName: 'Sunil Perera',
          isAnonymous: false,
          rating: 5,
          comment: 'Excellent Panchakarma therapy session and caring staff.',
          likeCount: 3,
          dislikeCount: 0,
          replies: const [],
          createdAt: DateTime.now().subtract(const Duration(days: 2)),
        ),
      ];

      await tester.pumpWidget(_buildHarness(feedbackList: realFeedback));
      await tester.pumpAndSettle();

      expect(find.byKey(ContactLocationKeys.testimonialsSection), findsOneWidget);
      expect(
        find.text('Excellent Panchakarma therapy session and caring staff.'),
        findsOneWidget,
      );
      expect(find.text('Sunil Perera'), findsOneWidget);
    },
  );
}
