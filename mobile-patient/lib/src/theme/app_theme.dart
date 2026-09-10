import 'package:flutter/material.dart';

/// Palette for the patient app.
///
/// Anchored on the forest green and warm cream of `docs/wireframes`, with a
/// muted gold accent for highlights. Deliberately not `ColorScheme.fromSeed`,
/// which would tint these away from the wireframe.
abstract final class AyurvedaColors {
  static const forest = Color(0xFF1B4332);
  static const forestDark = Color(0xFF102B1F);
  static const sage = Color(0xFF52796F);
  static const sageLight = Color(0xFF84A98C);
  static const sageMuted = Color(0xFFD9E2D7);

  static const gold = Color(0xFFC9A227);
  static const goldMuted = Color(0xFFF2E4BD);

  static const cream = Color(0xFFF7F3EA);
  static const creamRaised = Color(0xFFFFFDF8);
  static const border = Color(0xFFE4DDD0);

  static const ink = Color(0xFF1D2A22);
  static const inkMuted = Color(0xFF5D6F6E);

  static const danger = Color(0xFFA84832);
}

abstract final class AppTheme {
  static ThemeData get light {
    const colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: AyurvedaColors.forest,
      onPrimary: Colors.white,
      primaryContainer: AyurvedaColors.sageMuted,
      onPrimaryContainer: AyurvedaColors.forestDark,
      secondary: AyurvedaColors.gold,
      onSecondary: AyurvedaColors.forestDark,
      secondaryContainer: AyurvedaColors.goldMuted,
      onSecondaryContainer: AyurvedaColors.forestDark,
      tertiary: AyurvedaColors.sage,
      onTertiary: Colors.white,
      tertiaryContainer: AyurvedaColors.sageLight,
      onTertiaryContainer: AyurvedaColors.forestDark,
      error: AyurvedaColors.danger,
      onError: Colors.white,
      surface: AyurvedaColors.cream,
      onSurface: AyurvedaColors.ink,
      surfaceContainerLowest: AyurvedaColors.creamRaised,
      surfaceContainerLow: AyurvedaColors.creamRaised,
      surfaceContainer: AyurvedaColors.cream,
      onSurfaceVariant: AyurvedaColors.inkMuted,
      outline: AyurvedaColors.border,
      outlineVariant: AyurvedaColors.sageMuted,
    );

    final base = ThemeData(colorScheme: colorScheme, useMaterial3: true);

    return base.copyWith(
      scaffoldBackgroundColor: AyurvedaColors.cream,
      appBarTheme: const AppBarTheme(
        backgroundColor: AyurvedaColors.cream,
        foregroundColor: AyurvedaColors.ink,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
      ),
      textTheme: base.textTheme.apply(
        bodyColor: AyurvedaColors.ink,
        displayColor: AyurvedaColors.ink,
      ),
      cardTheme: CardThemeData(
        color: AyurvedaColors.creamRaised,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: AyurvedaColors.border),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AyurvedaColors.creamRaised,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: _inputBorder(AyurvedaColors.border),
        enabledBorder: _inputBorder(AyurvedaColors.border),
        focusedBorder: _inputBorder(AyurvedaColors.forest, width: 2),
        errorBorder: _inputBorder(AyurvedaColors.danger),
        focusedErrorBorder: _inputBorder(AyurvedaColors.danger, width: 2),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AyurvedaColors.forest,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AyurvedaColors.forest,
          minimumSize: const Size.fromHeight(52),
          side: const BorderSide(color: AyurvedaColors.forest),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AyurvedaColors.sage),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AyurvedaColors.creamRaised,
        indicatorColor: AyurvedaColors.sageMuted,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        height: 68,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            size: 24,
            color: states.contains(WidgetState.selected)
                ? AyurvedaColors.forest
                : AyurvedaColors.inkMuted,
          ),
        ),
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => TextStyle(
            fontSize: 11,
            fontWeight: states.contains(WidgetState.selected)
                ? FontWeight.w600
                : FontWeight.w500,
            color: states.contains(WidgetState.selected)
                ? AyurvedaColors.forest
                : AyurvedaColors.inkMuted,
          ),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AyurvedaColors.border,
        thickness: 1,
      ),
      snackBarTheme: const SnackBarThemeData(
        backgroundColor: AyurvedaColors.forestDark,
        contentTextStyle: TextStyle(color: Colors.white),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  static OutlineInputBorder _inputBorder(Color color, {double width = 1}) {
    return OutlineInputBorder(
      borderRadius: BorderRadius.circular(14),
      borderSide: BorderSide(color: color, width: width),
    );
  }
}
