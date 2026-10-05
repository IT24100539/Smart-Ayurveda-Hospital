import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/shared/widgets/clinic_widgets.dart';
import 'package:patient_app/src/theme/app_theme.dart';

double contrastRatio(Color foreground, Color background) {
  final lighter = math.max(_luminance(foreground), _luminance(background));
  final darker = math.min(_luminance(foreground), _luminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

double _luminance(Color color) {
  final argb = color.toARGB32();
  double channel(int shift) {
    final value = ((argb >> shift) & 0xFF) / 255;
    if (value <= 0.04045) return value / 12.92;
    return math.pow((value + 0.055) / 1.055, 2.4).toDouble();
  }

  return 0.2126 * channel(16) + 0.7152 * channel(8) + 0.0722 * channel(0);
}

void main() {
  test('light and dark tokens meet WCAG AA', () {
    void check(Color foreground, Color background, [double minimum = 4.5]) {
      expect(
        contrastRatio(foreground, background),
        greaterThanOrEqualTo(minimum),
        reason: '$foreground on $background',
      );
    }

    const light = AyurvedaThemeExtension.light;
    const dark = AyurvedaThemeExtension.dark;

    check(AyurvedaColors.ink, AyurvedaColors.cream);
    check(AyurvedaColors.ink, AyurvedaColors.creamRaised);
    check(AyurvedaColors.inkMuted, AyurvedaColors.cream);
    check(AyurvedaColors.inkMuted, AyurvedaColors.creamRaised);
    check(light.teal, AyurvedaColors.creamRaised);
    check(light.onTeal, light.teal);
    check(light.pillForeground, light.pillBackground);
    check(light.approvedForeground, light.approvedBackground);
    check(light.pendingForeground, light.pendingBackground);
    check(light.completedForeground, light.completedBackground);
    check(light.neutralForeground, light.neutralBackground);
    check(light.terracottaAccent, light.terracottaBackground);
    check(light.onHeader, light.headerGradientStart);
    check(light.onHeader, light.headerGradientEnd);
    check(light.avatarForeground, light.avatarBackground);
    check(light.avatarBackground, light.headerGradientEnd);
    check(AyurvedaColors.danger, AyurvedaColors.cream);
    check(AyurvedaColors.danger, AyurvedaColors.dangerMuted);
    check(AppTheme.light.colorScheme.outline, AyurvedaColors.creamRaised, 3);
    check(AppTheme.light.colorScheme.outline, AyurvedaColors.creamSunken, 3);

    check(AyurvedaColors.darkInk, AyurvedaColors.darkScaffold);
    check(AyurvedaColors.darkInk, AyurvedaColors.darkSurface);
    check(AyurvedaColors.darkInkMuted, AyurvedaColors.darkScaffold);
    check(AyurvedaColors.darkInkMuted, AyurvedaColors.darkSurface);
    check(dark.teal, AyurvedaColors.darkScaffold);
    check(dark.teal, AyurvedaColors.darkSurface);
    check(dark.onTeal, dark.teal);
    check(dark.pillForeground, dark.pillBackground);
    check(dark.approvedForeground, dark.approvedBackground);
    check(dark.pendingForeground, dark.pendingBackground);
    check(dark.completedForeground, dark.completedBackground);
    check(dark.neutralForeground, dark.neutralBackground);
    check(dark.terracottaAccent, dark.terracottaBackground);
    check(dark.onHeader, dark.headerGradientStart);
    check(dark.onHeader, dark.headerGradientEnd);
    check(AyurvedaColors.dangerOnDark, AyurvedaColors.darkScaffold);
    check(AyurvedaColors.dangerOnDark, AyurvedaColors.darkSurface);
    check(AyurvedaColors.dangerOnDark, AyurvedaColors.dangerMutedDark);
    check(AppTheme.dark.colorScheme.outline, AyurvedaColors.darkSurface, 3);
    check(AppTheme.dark.colorScheme.outline, AyurvedaColors.darkSurfaceRaised, 3);
    check(AyurvedaColors.labelMutedDark, AyurvedaColors.darkSurface);
    check(AyurvedaColors.labelMutedDark, AyurvedaColors.darkSurfaceRaised);
    check(AyurvedaColors.warning, AyurvedaColors.cream);
    check(AyurvedaColors.warning, AyurvedaColors.creamSunken);
    check(AyurvedaColors.warningOnDark, AyurvedaColors.darkScaffold);

    expect(AppTheme.light.colorScheme.error, AyurvedaColors.danger);
    expect(AppTheme.dark.colorScheme.error, AyurvedaColors.dangerOnDark);
    expect(light.cardRadius, 24);
    expect(dark.cardRadius, 24);
    expect(light.headerRadius, 28);

    final lightStart = contrastRatio(light.onHeader, light.headerGradientStart);
    final lightEnd = contrastRatio(light.onHeader, light.headerGradientEnd);
    final darkStart = contrastRatio(dark.onHeader, dark.headerGradientStart);
    final darkEnd = contrastRatio(dark.onHeader, dark.headerGradientEnd);
    expect(lightStart, closeTo(6.00, 0.05), reason: 'light header start ${lightStart.toStringAsFixed(2)}:1');
    expect(lightEnd, closeTo(4.95, 0.05), reason: 'light header end ${lightEnd.toStringAsFixed(2)}:1');
    expect(darkStart, closeTo(7.17, 0.05), reason: 'dark header start ${darkStart.toStringAsFixed(2)}:1');
    expect(darkEnd, closeTo(4.95, 0.05), reason: 'dark header end ${darkEnd.toStringAsFixed(2)}:1');
  });

  testWidgets('header card, stepper, chips, and dark error text', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: Scaffold(
          body: ListView(
            children: const [
              PatientHeaderCard(name: 'Sunil Shantha', uhid: 'SAH-9', subtitle: 'Care record'),
              NumberedStepper(labels: ['Date', 'Time', 'Confirm'], current: 1),
              ErrorLine(message: 'Could not save'),
            ],
          ),
        ),
      ),
    );

    expect(find.text('SS'), findsOneWidget);
    expect(find.text('SAH-9'), findsOneWidget);
    expect(find.text('DATE'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.byIcon(Icons.check), findsOneWidget);
    expect(find.byIcon(Icons.error_outline), findsOneWidget);

    final error = tester.widget<Text>(find.text('Could not save'));
    expect(error.style?.color, AyurvedaColors.dangerOnDark);
  });

  testWidgets('suggestion chips scroll without a visible scrollbar', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: SuggestionChips(labels: const ['Abhyanga', 'Shirodhara'], onSelected: (_) {}),
        ),
      ),
    );

    expect(find.text('Abhyanga'), findsOneWidget);
    expect(find.byType(Scrollbar), findsNothing);
    expect(find.byType(RawScrollbar), findsNothing);
  });
}
