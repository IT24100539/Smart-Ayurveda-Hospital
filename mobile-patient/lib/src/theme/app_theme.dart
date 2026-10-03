import 'dart:ui' show lerpDouble;

import 'package:flutter/material.dart';

/// Palette for the patient app.
///
/// Light mode is cream and white with herbal green and saffron gold.
/// Dark mode is a near-black green with dark-teal cards.
/// Role colors that change with brightness live on [AyurvedaThemeExtension].
abstract final class AyurvedaColors {
  static const forest = Color(0xFF1B2B27);
  static const forestDark = Color(0xFF102B1F);
  static const forestLight = Color(0xFF1F6F5F);

  static const sage = Color(0xFF566862);
  static const sageLight = Color(0xFF4DB6AC);
  static const sageMuted = Color(0xFFE5F4F2);
  static const primaryHover = Color(0xFF6CC9C0);

  static const gold = Color(0xFF6B4410);
  static const goldBright = Color(0xFFE7C27D);
  static const goldMuted = Color(0xFFF6E7C8);
  static const saffron = Color(0xFFC98A1B);

  static const terracotta = Color(0xFF9A4530);
  static const terracottaMuted = Color(0xFFF8E6DF);
  static const terracottaOnDark = Color(0xFFE08A6B);

  static const cream = Color(0xFFF7F3E8);
  static const creamRaised = Color(0xFFFFFFFF);
  static const creamSunken = Color(0xFFF1EBDA);
  static const border = Color(0xFF52635D);
  static const borderSubtle = Color(0xFFE3DCC8);

  static const ink = Color(0xFF1B2B27);
  static const inkMuted = Color(0xFF566862);
  static const fieldBorder = Color(0xFF5D6F69);
  static const warning = Color(0xFF8F5C00);
  static const warningOnDark = Color(0xFFF0B44C);

  static const success = Color(0xFF166E48);
  static const successOnDark = Color(0xFF5FD3A0);
  static const danger = Color(0xFFB3261E);
  static const dangerMuted = Color(0xFFFDECEA);
  static const dangerOnDark = Color(0xFFFF8A80);
  static const dangerMutedDark = Color(0xFF3A2220);

  static const info = Color(0xFF1D4E89);
  static const infoMuted = Color(0xFFE4EEF8);
  static const neutralChip = Color(0xFFF1EBDA);
  static const neutralChipInk = Color(0xFF566862);
  static const approvedBackground = Color(0xFFE3F4EC);
  static const approvedForeground = Color(0xFF17613F);
  static const pendingBackground = Color(0xFFFBE9C6);
  static const pendingForeground = Color(0xFF7A4E0F);

  static const scrim = Color(0xFF0B1512);
  static const onScrim = Color(0xFFFFFFFF);

  static const darkScaffold = Color(0xFF0B1512);
  static const darkSurface = Color(0xFF13241F);
  static const darkSurfaceRaised = Color(0xFF1A302A);
  static const darkBorder = Color(0xFF4E6E66);
  static const darkBorderSubtle = Color(0xFF24403A);
  static const darkInk = Color(0xFFEAF2EF);
  static const darkInkMuted = Color(0xFF9DB5AE);
  static const labelMutedDark = Color(0xFF7F9A93);
  static const onTealDark = Color(0xFF06201C);
  static const fieldBorderDark = Color(0xFF7F9A93);

  static const headerLightStart = Color(0xFF1F6F5F);
  static const headerLightEnd = Color(0xFF277D6D);
  static const headerDarkStart = Color(0xFF0F5953);
  static const headerDarkEnd = Color(0xFF187370);
  static const onHeader = Color(0xFFEAF2EF);
  static const avatarFill = Color(0xFFF1EBDA);
  static const onPrimaryLight = Color(0xFFFFFFFF);
}

/// Brand roles that switch with light and dark.
class AyurvedaThemeExtension extends ThemeExtension<AyurvedaThemeExtension> {
  const AyurvedaThemeExtension({
    required this.goldAccent,
    required this.terracottaAccent,
    required this.terracottaBackground,
    required this.cardBorderColor,
    required this.heroGradientStart,
    required this.heroGradientEnd,
    required this.teal,
    required this.onTeal,
    required this.pillBackground,
    required this.pillForeground,
    required this.approvedBackground,
    required this.approvedForeground,
    required this.pendingBackground,
    required this.pendingForeground,
    required this.completedBackground,
    required this.completedForeground,
    required this.neutralBackground,
    required this.neutralForeground,
    required this.headerGradientStart,
    required this.headerGradientEnd,
    required this.onHeader,
    required this.avatarBackground,
    required this.avatarForeground,
    required this.cardRadius,
    required this.headerRadius,
  });

  final Color goldAccent;
  final Color terracottaAccent;
  final Color terracottaBackground;
  final Color cardBorderColor;
  final Color heroGradientStart;
  final Color heroGradientEnd;
  final Color teal;
  final Color onTeal;
  final Color pillBackground;
  final Color pillForeground;
  final Color approvedBackground;
  final Color approvedForeground;
  final Color pendingBackground;
  final Color pendingForeground;
  final Color completedBackground;
  final Color completedForeground;
  final Color neutralBackground;
  final Color neutralForeground;
  final Color headerGradientStart;
  final Color headerGradientEnd;
  final Color onHeader;
  final Color avatarBackground;
  final Color avatarForeground;
  final double cardRadius;
  final double headerRadius;

  static const light = AyurvedaThemeExtension(
    goldAccent: AyurvedaColors.gold,
    terracottaAccent: AyurvedaColors.terracotta,
    terracottaBackground: AyurvedaColors.terracottaMuted,
    cardBorderColor: AyurvedaColors.borderSubtle,
    heroGradientStart: AyurvedaColors.headerLightStart,
    heroGradientEnd: AyurvedaColors.headerLightEnd,
    teal: AyurvedaColors.forestLight,
    onTeal: AyurvedaColors.onPrimaryLight,
    pillBackground: AyurvedaColors.goldMuted,
    pillForeground: AyurvedaColors.gold,
    approvedBackground: AyurvedaColors.approvedBackground,
    approvedForeground: AyurvedaColors.approvedForeground,
    pendingBackground: AyurvedaColors.pendingBackground,
    pendingForeground: AyurvedaColors.pendingForeground,
    completedBackground: AyurvedaColors.sageMuted,
    completedForeground: AyurvedaColors.forestLight,
    neutralBackground: AyurvedaColors.neutralChip,
    neutralForeground: AyurvedaColors.neutralChipInk,
    headerGradientStart: AyurvedaColors.headerLightStart,
    headerGradientEnd: AyurvedaColors.headerLightEnd,
    onHeader: AyurvedaColors.onPrimaryLight,
    avatarBackground: AyurvedaColors.onPrimaryLight,
    avatarForeground: AyurvedaColors.forestLight,
    cardRadius: 24,
    headerRadius: 28,
  );

  static const dark = AyurvedaThemeExtension(
    goldAccent: AyurvedaColors.goldBright,
    terracottaAccent: AyurvedaColors.terracottaOnDark,
    terracottaBackground: Color(0xFF3A2420),
    cardBorderColor: AyurvedaColors.darkBorderSubtle,
    heroGradientStart: AyurvedaColors.headerDarkStart,
    heroGradientEnd: AyurvedaColors.headerDarkEnd,
    teal: AyurvedaColors.sageLight,
    onTeal: AyurvedaColors.onTealDark,
    pillBackground: AyurvedaColors.goldMuted,
    pillForeground: AyurvedaColors.gold,
    approvedBackground: AyurvedaColors.approvedBackground,
    approvedForeground: AyurvedaColors.approvedForeground,
    pendingBackground: AyurvedaColors.pendingBackground,
    pendingForeground: AyurvedaColors.pendingForeground,
    completedBackground: AyurvedaColors.sageMuted,
    completedForeground: AyurvedaColors.forestLight,
    neutralBackground: AyurvedaColors.neutralChip,
    neutralForeground: AyurvedaColors.neutralChipInk,
    headerGradientStart: AyurvedaColors.headerDarkStart,
    headerGradientEnd: AyurvedaColors.headerDarkEnd,
    onHeader: AyurvedaColors.onHeader,
    avatarBackground: AyurvedaColors.onPrimaryLight,
    avatarForeground: AyurvedaColors.forestLight,
    cardRadius: 24,
    headerRadius: 28,
  );

  static AyurvedaThemeExtension of(BuildContext context) {
    return Theme.of(context).extension<AyurvedaThemeExtension>() ?? light;
  }

  @override
  AyurvedaThemeExtension copyWith({
    Color? goldAccent,
    Color? terracottaAccent,
    Color? terracottaBackground,
    Color? cardBorderColor,
    Color? heroGradientStart,
    Color? heroGradientEnd,
    Color? teal,
    Color? onTeal,
    Color? pillBackground,
    Color? pillForeground,
    Color? approvedBackground,
    Color? approvedForeground,
    Color? pendingBackground,
    Color? pendingForeground,
    Color? completedBackground,
    Color? completedForeground,
    Color? neutralBackground,
    Color? neutralForeground,
    Color? headerGradientStart,
    Color? headerGradientEnd,
    Color? onHeader,
    Color? avatarBackground,
    Color? avatarForeground,
    double? cardRadius,
    double? headerRadius,
  }) {
    return AyurvedaThemeExtension(
      goldAccent: goldAccent ?? this.goldAccent,
      terracottaAccent: terracottaAccent ?? this.terracottaAccent,
      terracottaBackground: terracottaBackground ?? this.terracottaBackground,
      cardBorderColor: cardBorderColor ?? this.cardBorderColor,
      heroGradientStart: heroGradientStart ?? this.heroGradientStart,
      heroGradientEnd: heroGradientEnd ?? this.heroGradientEnd,
      teal: teal ?? this.teal,
      onTeal: onTeal ?? this.onTeal,
      pillBackground: pillBackground ?? this.pillBackground,
      pillForeground: pillForeground ?? this.pillForeground,
      approvedBackground: approvedBackground ?? this.approvedBackground,
      approvedForeground: approvedForeground ?? this.approvedForeground,
      pendingBackground: pendingBackground ?? this.pendingBackground,
      pendingForeground: pendingForeground ?? this.pendingForeground,
      completedBackground: completedBackground ?? this.completedBackground,
      completedForeground: completedForeground ?? this.completedForeground,
      neutralBackground: neutralBackground ?? this.neutralBackground,
      neutralForeground: neutralForeground ?? this.neutralForeground,
      headerGradientStart: headerGradientStart ?? this.headerGradientStart,
      headerGradientEnd: headerGradientEnd ?? this.headerGradientEnd,
      onHeader: onHeader ?? this.onHeader,
      avatarBackground: avatarBackground ?? this.avatarBackground,
      avatarForeground: avatarForeground ?? this.avatarForeground,
      cardRadius: cardRadius ?? this.cardRadius,
      headerRadius: headerRadius ?? this.headerRadius,
    );
  }

  @override
  AyurvedaThemeExtension lerp(
    covariant ThemeExtension<AyurvedaThemeExtension>? other,
    double t,
  ) {
    if (other is! AyurvedaThemeExtension) return this;
    return AyurvedaThemeExtension(
      goldAccent: Color.lerp(goldAccent, other.goldAccent, t)!,
      terracottaAccent: Color.lerp(terracottaAccent, other.terracottaAccent, t)!,
      terracottaBackground:
          Color.lerp(terracottaBackground, other.terracottaBackground, t)!,
      cardBorderColor: Color.lerp(cardBorderColor, other.cardBorderColor, t)!,
      heroGradientStart: Color.lerp(heroGradientStart, other.heroGradientStart, t)!,
      heroGradientEnd: Color.lerp(heroGradientEnd, other.heroGradientEnd, t)!,
      teal: Color.lerp(teal, other.teal, t)!,
      onTeal: Color.lerp(onTeal, other.onTeal, t)!,
      pillBackground: Color.lerp(pillBackground, other.pillBackground, t)!,
      pillForeground: Color.lerp(pillForeground, other.pillForeground, t)!,
      approvedBackground:
          Color.lerp(approvedBackground, other.approvedBackground, t)!,
      approvedForeground:
          Color.lerp(approvedForeground, other.approvedForeground, t)!,
      pendingBackground: Color.lerp(pendingBackground, other.pendingBackground, t)!,
      pendingForeground: Color.lerp(pendingForeground, other.pendingForeground, t)!,
      completedBackground:
          Color.lerp(completedBackground, other.completedBackground, t)!,
      completedForeground:
          Color.lerp(completedForeground, other.completedForeground, t)!,
      neutralBackground: Color.lerp(neutralBackground, other.neutralBackground, t)!,
      neutralForeground: Color.lerp(neutralForeground, other.neutralForeground, t)!,
      headerGradientStart:
          Color.lerp(headerGradientStart, other.headerGradientStart, t)!,
      headerGradientEnd: Color.lerp(headerGradientEnd, other.headerGradientEnd, t)!,
      onHeader: Color.lerp(onHeader, other.onHeader, t)!,
      avatarBackground: Color.lerp(avatarBackground, other.avatarBackground, t)!,
      avatarForeground: Color.lerp(avatarForeground, other.avatarForeground, t)!,
      cardRadius: lerpDouble(cardRadius, other.cardRadius, t)!,
      headerRadius: lerpDouble(headerRadius, other.headerRadius, t)!,
    );
  }
}

abstract final class AyurvedaFonts {
  static const serif = 'Source Serif 4';
  static const sans = 'Source Sans 3';
  static const sinhala = 'Noto Sans Sinhala';
  static const fallback = <String>[sinhala];
}

/// Serif titles and prices, sans body, small tracked labels.
abstract final class AyurvedaType {
  static const avatarSize = 72.0;
  static const avatarRadius = 20.0;
  static const stepSize = 48.0;
  static TextStyle price(BuildContext context) {
    return TextStyle(
      fontFamily: AyurvedaFonts.serif,
      fontFamilyFallback: AyurvedaFonts.fallback,
      fontWeight: FontWeight.w700,
      fontSize: 18,
      height: 1.2,
      color: AyurvedaThemeExtension.of(context).teal,
    );
  }

  static TextStyle eyebrow(BuildContext context, {Color? color}) {
    return TextStyle(
      fontSize: 11,
      fontWeight: FontWeight.w600,
      letterSpacing: 1.32,
      color: color ?? Theme.of(context).colorScheme.onSurfaceVariant,
    );
  }
}

abstract final class AppTheme {
  static ThemeData get light => _build(
    brightness: Brightness.light,
    scheme: const ColorScheme(
      brightness: Brightness.light,
      primary: AyurvedaColors.forestLight,
      onPrimary: AyurvedaColors.onPrimaryLight,
      primaryContainer: AyurvedaColors.sageMuted,
      onPrimaryContainer: AyurvedaColors.forestLight,
      secondary: AyurvedaColors.gold,
      onSecondary: AyurvedaColors.onPrimaryLight,
      secondaryContainer: AyurvedaColors.goldMuted,
      onSecondaryContainer: AyurvedaColors.gold,
      tertiary: AyurvedaColors.terracotta,
      onTertiary: AyurvedaColors.onPrimaryLight,
      tertiaryContainer: AyurvedaColors.terracottaMuted,
      onTertiaryContainer: AyurvedaColors.terracotta,
      error: AyurvedaColors.danger,
      onError: AyurvedaColors.onPrimaryLight,
      errorContainer: AyurvedaColors.dangerMuted,
      onErrorContainer: AyurvedaColors.danger,
      surface: AyurvedaColors.cream,
      onSurface: AyurvedaColors.ink,
      surfaceContainerLowest: AyurvedaColors.creamRaised,
      surfaceContainerLow: AyurvedaColors.creamRaised,
      surfaceContainer: AyurvedaColors.creamSunken,
      surfaceContainerHigh: AyurvedaColors.creamSunken,
      onSurfaceVariant: AyurvedaColors.inkMuted,
      outline: AyurvedaColors.fieldBorder,
      outlineVariant: AyurvedaColors.borderSubtle,
    ),
    extension: AyurvedaThemeExtension.light,
    scaffold: AyurvedaColors.cream,
    card: AyurvedaColors.creamRaised,
    appBar: AyurvedaColors.cream,
    appBarForeground: AyurvedaColors.ink,
    navBar: AyurvedaColors.creamRaised,
    navIndicator: AyurvedaColors.sageMuted,
    navSelected: AyurvedaColors.forestLight,
    navIdle: AyurvedaColors.inkMuted,
    inputFill: AyurvedaColors.creamRaised,
    focus: AyurvedaColors.forestLight,
  );

  static ThemeData get dark => _build(
    brightness: Brightness.dark,
    scheme: const ColorScheme(
      brightness: Brightness.dark,
      primary: AyurvedaColors.sageLight,
      onPrimary: AyurvedaColors.onTealDark,
      primaryContainer: Color(0xFF163832),
      onPrimaryContainer: AyurvedaColors.sageLight,
      secondary: AyurvedaColors.goldBright,
      onSecondary: Color(0xFF3D3420),
      secondaryContainer: Color(0xFF3D3420),
      onSecondaryContainer: AyurvedaColors.goldBright,
      tertiary: AyurvedaColors.terracottaOnDark,
      onTertiary: Color(0xFF3A2420),
      tertiaryContainer: Color(0xFF3A2420),
      onTertiaryContainer: AyurvedaColors.terracottaOnDark,
      error: AyurvedaColors.dangerOnDark,
      onError: Color(0xFF3A1210),
      errorContainer: AyurvedaColors.dangerMutedDark,
      onErrorContainer: AyurvedaColors.dangerOnDark,
      surface: AyurvedaColors.darkScaffold,
      onSurface: AyurvedaColors.darkInk,
      surfaceContainerLowest: AyurvedaColors.darkScaffold,
      surfaceContainerLow: AyurvedaColors.darkSurface,
      surfaceContainer: AyurvedaColors.darkSurface,
      surfaceContainerHigh: AyurvedaColors.darkSurfaceRaised,
      onSurfaceVariant: AyurvedaColors.darkInkMuted,
      outline: AyurvedaColors.fieldBorderDark,
      outlineVariant: AyurvedaColors.darkBorderSubtle,
    ),
    extension: AyurvedaThemeExtension.dark,
    scaffold: AyurvedaColors.darkScaffold,
    card: AyurvedaColors.darkSurface,
    appBar: AyurvedaColors.darkScaffold,
    appBarForeground: AyurvedaColors.darkInk,
    navBar: AyurvedaColors.darkSurface,
    navIndicator: Color(0xFF163832),
    navSelected: AyurvedaColors.sageLight,
    navIdle: AyurvedaColors.darkInkMuted,
    inputFill: AyurvedaColors.darkSurfaceRaised,
    focus: AyurvedaColors.sageLight,
  );

  static ThemeData _build({
    required Brightness brightness,
    required ColorScheme scheme,
    required AyurvedaThemeExtension extension,
    required Color scaffold,
    required Color card,
    required Color appBar,
    required Color appBarForeground,
    required Color navBar,
    required Color navIndicator,
    required Color navSelected,
    required Color navIdle,
    required Color inputFill,
    required Color focus,
  }) {
    final base = ThemeData(
      colorScheme: scheme,
      useMaterial3: true,
      fontFamily: AyurvedaFonts.sans,
      fontFamilyFallback: AyurvedaFonts.fallback,
    );
    final ink = scheme.onSurface;
    final serif = TextStyle(
      fontFamily: AyurvedaFonts.serif,
      fontFamilyFallback: AyurvedaFonts.fallback,
      fontWeight: FontWeight.w700,
      color: ink,
    );

    return base.copyWith(
      scaffoldBackgroundColor: scaffold,
      extensions: [extension],
      appBarTheme: AppBarTheme(
        backgroundColor: appBar,
        foregroundColor: appBarForeground,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: serif.copyWith(fontSize: 20),
      ),
      cardTheme: CardThemeData(
        color: card,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: EdgeInsets.zero,
        shadowColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(extension.cardRadius),
          side: BorderSide(color: extension.cardBorderColor),
        ),
      ),
      textTheme: base.textTheme
          .copyWith(
            displayLarge: serif.copyWith(fontSize: 40, height: 1.1),
            displayMedium: serif.copyWith(fontSize: 32, height: 1.25),
            headlineLarge: serif.copyWith(fontSize: 32, height: 1.25),
            headlineMedium: serif.copyWith(fontSize: 22, height: 1.27),
            headlineSmall: serif.copyWith(fontSize: 22, height: 1.27),
            titleLarge: serif.copyWith(fontSize: 22, height: 1.27),
            titleMedium: serif.copyWith(fontSize: 20, height: 1.3),
            titleSmall: serif.copyWith(fontSize: 20, height: 1.3),
            bodyLarge: TextStyle(fontSize: 15, height: 1.5, color: ink),
            bodyMedium: TextStyle(fontSize: 15, height: 1.5, color: ink),
            bodySmall: TextStyle(fontSize: 13, height: 1.4, color: scheme.onSurfaceVariant),
            labelSmall: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              letterSpacing: 1.32,
              color: brightness == Brightness.dark
                  ? AyurvedaColors.labelMutedDark
                  : AyurvedaColors.inkMuted,
            ),
          )
          .apply(bodyColor: ink, displayColor: ink),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: scheme.primary,
          foregroundColor: scheme.onPrimary,
          disabledBackgroundColor: scheme.primary.withValues(alpha: 0.38),
          disabledForegroundColor: scheme.onPrimary.withValues(alpha: 0.7),
          minimumSize: const Size.fromHeight(52),
          elevation: 0,
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: const StadiumBorder(),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: scheme.primary,
          foregroundColor: scheme.onPrimary,
          elevation: 0,
          shadowColor: Colors.transparent,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: const StadiumBorder(),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: scheme.primary,
          minimumSize: const Size.fromHeight(52),
          side: BorderSide(color: scheme.primary, width: 1.5),
          shape: const StadiumBorder(),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: scheme.primary),
      ),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        backgroundColor: scheme.primary,
        foregroundColor: scheme.onPrimary,
        elevation: 0,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: inputFill,
        contentPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
        border: _inputBorder(scheme.outline),
        enabledBorder: _inputBorder(scheme.outline),
        focusedBorder: _inputBorder(focus, width: 2),
        errorBorder: _inputBorder(scheme.error),
        focusedErrorBorder: _inputBorder(scheme.error, width: 2),
        errorStyle: TextStyle(color: scheme.error, fontWeight: FontWeight.w600),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: navBar,
        indicatorColor: navIndicator,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        height: 72,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            size: 24,
            color: states.contains(WidgetState.selected) ? navSelected : navIdle,
          ),
        ),
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => TextStyle(
            fontSize: 11,
            letterSpacing: 0.4,
            fontWeight: states.contains(WidgetState.selected)
                ? FontWeight.w700
                : FontWeight.w500,
            color: states.contains(WidgetState.selected) ? navSelected : navIdle,
          ),
        ),
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: ButtonStyle(
          foregroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.selected)
                ? scheme.onPrimary
                : scheme.onSurface,
          ),
          backgroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.selected) ? scheme.primary : null,
          ),
          side: WidgetStateProperty.all(BorderSide(color: scheme.outline)),
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: extension.pillBackground,
        labelStyle: TextStyle(
          color: extension.pillForeground,
          fontWeight: FontWeight.w700,
          fontSize: 12,
        ),
        side: BorderSide.none,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
      ),
      dividerTheme: DividerThemeData(color: extension.cardBorderColor, thickness: 1),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: brightness == Brightness.dark
            ? AyurvedaColors.darkSurfaceRaised
            : AyurvedaColors.forestDark,
        contentTextStyle: TextStyle(
          color: brightness == Brightness.dark
              ? AyurvedaColors.darkInk
              : AyurvedaColors.onHeader,
        ),
        behavior: SnackBarBehavior.floating,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(extension.cardRadius),
        ),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: scheme.primary),
      dialogTheme: DialogThemeData(
        backgroundColor: card,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(extension.headerRadius),
          side: BorderSide(color: extension.cardBorderColor),
        ),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: card,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(extension.headerRadius),
          ),
        ),
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? scheme.onPrimary
              : scheme.onSurfaceVariant,
        ),
        trackColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected)
              ? scheme.primary
              : scheme.surfaceContainerHigh,
        ),
      ),
      tabBarTheme: TabBarThemeData(
        labelColor: scheme.primary,
        unselectedLabelColor: scheme.onSurfaceVariant,
        indicatorColor: scheme.primary,
        dividerColor: extension.cardBorderColor,
      ),
      navigationRailTheme: NavigationRailThemeData(
        backgroundColor: navBar,
        indicatorColor: navIndicator,
        selectedIconTheme: IconThemeData(color: navSelected),
        unselectedIconTheme: IconThemeData(color: navIdle),
        selectedLabelTextStyle: TextStyle(
          color: navSelected,
          fontWeight: FontWeight.w700,
          fontSize: 12,
        ),
        unselectedLabelTextStyle: TextStyle(color: navIdle, fontSize: 12),
      ),
    );
  }

  static OutlineInputBorder _inputBorder(Color color, {double width = 1}) {
    return OutlineInputBorder(
      borderRadius: BorderRadius.circular(28),
      borderSide: BorderSide(color: color, width: width),
    );
  }
}
