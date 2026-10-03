import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/dev/presentation/component_gallery_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

Widget _gallery(ThemeData theme) {
  return MaterialApp(
    theme: theme,
    locale: const Locale('en'),
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: AppLocalizations.supportedLocales,
    home: const ComponentGalleryScreen(),
  );
}

void main() {
  for (final entry in {
    'light': AppTheme.light,
    'dark': AppTheme.dark,
  }.entries) {
    testWidgets('component gallery builds in ${entry.key}', (tester) async {
      await tester.pumpWidget(_gallery(entry.value));
      await tester.pumpAndSettle();

      expect(find.text('Component gallery'), findsOneWidget);
      expect(find.text('Approved'), findsWidgets);
      expect(find.text('Pending'), findsWidgets);
      expect(find.text('THERAPY'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }
}
