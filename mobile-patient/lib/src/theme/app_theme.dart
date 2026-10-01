import 'package:flutter/material.dart';

/// Palette for the patient app.
///
/// Anchored on the deep herbal forest green and warm cream of the design system,
/// with gold and terracotta accents for highlights and alerts.
abstract final class AyurvedaColors {
  static const forest = Color(0xFF1B4332);
  static const forestDark = Color(0xFF102B1F);
  static const forestLight = Color(0xFF26675F);
  static const sage = Color(0xFF52796F);
  static const sageLight = Color(0xFF84A98C);
  static const sageMuted = Color(0xFFD9E2D7);

  static const gold = Color(0xFFC9A227);
  static const goldMuted = Color(0xFFF2E4BD);

  static const terracotta = Color(0xFFC86A4B);
  static const terracottaMuted = Color(0xFFF7D9CF);

  static const cream = Color(0xFFF7F3EA);
  static const creamRaised = Color(0xFFFFFDF8);
  static const border = Color(0xFFE4DDD0);

  static const ink = Color(0xFF1D2A22);
  static const inkMuted = Color(0xFF5D6F6E);

  static const danger = Color(0xFFA84832);
  static const dangerMuted = Color(0xFFF4D6D2);

  static const info = Color(0xFF205477);
  static const infoMuted = Color(0xFFD8E7F3);
  static const neutralChip = Color(0xFFE3E3E3);
  static const neutralChipInk = Color(0xFF555555);

  // Dark mode surface tokens
  static const darkScaffold = Color(0xFF0D1B18);
  static const darkSurface = Color(0xFF142724);
  static const darkSurfaceRaised = Color(0xFF1B3430);
  static const darkBorder = Color(0xFF264943);
  static const darkInk = Color(0xFFEDF6F3);
  static const darkInkMuted = Color(0xFF8EAFA7);
}

/// Custom theme extension for hospital brand accents not natively in ColorScheme.
class AyurvedaThemeExtension extends ThemeExtension<AyurvedaThemeExtension> {
  const AyurvedaThemeExtension({
    required this.goldAccent,
    required this.terracottaAccent,
    required this.cardBorderColor,
    required this.heroGradientStart,
    required this.heroGradientEnd,
  });

  final Color goldAccent;
  final Color terracottaAccent;
  final Color cardBorderColor;
  final Color heroGradientStart;
  final Color heroGradientEnd;

  @override
  ThemeExtension<AyurvedaThemeExtension> copyWith({
    Color? goldAccent,
    Color? terracottaAccent,
    Color? cardBorderColor,
    Color? heroGradientStart,
    Color? heroGradientEnd,
  }) {
    return AyurvedaThemeExtension(
      goldAccent: goldAccent ?? this.goldAccent,
      terracottaAccent: terracottaAccent ?? this.terracottaAccent,
      cardBorderColor: cardBorderColor ?? this.cardBorderColor,
      heroGradientStart: heroGradientStart ?? this.heroGradientStart,
      heroGradientEnd: heroGradientEnd ?? this.heroGradientEnd,
    );
  }

  @override
  ThemeExtension<AyurvedaThemeExtension> lerp(
    covariant ThemeExtension<AyurvedaThemeExtension>? other,
    double t,
  ) {
    if (other is! AyurvedaThemeExtension) return this;
    return AyurvedaThemeExtension(
      goldAccent: Color.lerp(goldAccent, other.goldAccent, t)!,
      terracottaAccent:
          Color.lerp(terracottaAccent, other.terracottaAccent, t)!,
      cardBorderColor:
          Color.lerp(cardBorderColor, other.cardBorderColor, t)!,
      heroGradientStart:
          Color.lerp(heroGradientStart, other.heroGradientStart, t)!,
      heroGradientEnd:
          Color.lerp(heroGradientEnd, other.heroGradientEnd, t)!,
    );
  }
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
      extensions: const [
        AyurvedaThemeExtension(
          goldAccent: AyurvedaColors.gold,
          terracottaAccent: AyurvedaColors.terracotta,
          cardBorderColor: AyurvedaColors.border,
          heroGradientStart: Color(0xFF1B4332),
          heroGradientEnd: Color(0xFF0F2D22),
        ),
      ],
      appBarTheme: const AppBarTheme(
        backgroundColor: AyurvedaColors.cream,
        foregroundColor: AyurvedaColors.ink,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AyurvedaColors.ink,
          fontSize: 20,
          fontWeight: FontWeight.w600,
          fontFamily: 'serif',
        ),
      ),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AyurvedaColors.forest,
        foregroundColor: Colors.white,
        elevation: 2,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AyurvedaColors.forest,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          elevation: 1,
        ),
      ),
      textTheme: base.textTheme
          .copyWith(
            displayLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            displayMedium: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            headlineLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            headlineMedium: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.w700,
            ),
            titleLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.w700,
              fontSize: 20,
            ),
          )
          .apply(
            bodyColor: AyurvedaColors.ink,
            displayColor: AyurvedaColors.ink,
          ),
      cardTheme: CardThemeData(
        color: AyurvedaColors.creamRaised,
        surfaceTintColor: Colors.transparent,
        elevation: 1,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: AyurvedaColors.border),
        ),
        shadowColor: const Color(0x141A2E2D),
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
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AyurvedaColors.forest,
          minimumSize: const Size.fromHeight(52),
          side: const BorderSide(color: AyurvedaColors.forest, width: 1.5),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
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
        elevation: 3,
        height: 72,
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

  static ThemeData get dark {
    const colorScheme = ColorScheme(
      brightness: Brightness.dark,
      primary: AyurvedaColors.forestLight,
      onPrimary: Colors.white,
      primaryContainer: Color(0xFF1B3D34),
      onPrimaryContainer: AyurvedaColors.sageMuted,
      secondary: AyurvedaColors.gold,
      onSecondary: AyurvedaColors.forestDark,
      secondaryContainer: Color(0xFF423315),
      onSecondaryContainer: AyurvedaColors.goldMuted,
      tertiary: AyurvedaColors.sageLight,
      onTertiary: AyurvedaColors.forestDark,
      tertiaryContainer: Color(0xFF28483F),
      onTertiaryContainer: Colors.white,
      error: AyurvedaColors.danger,
      onError: Colors.white,
      surface: AyurvedaColors.darkScaffold,
      onSurface: AyurvedaColors.darkInk,
      surfaceContainerLowest: AyurvedaColors.darkScaffold,
      surfaceContainerLow: AyurvedaColors.darkSurface,
      surfaceContainer: AyurvedaColors.darkSurfaceRaised,
      onSurfaceVariant: AyurvedaColors.darkInkMuted,
      outline: AyurvedaColors.darkBorder,
      outlineVariant: Color(0xFF335850),
    );

    final base = ThemeData(colorScheme: colorScheme, useMaterial3: true);

    return base.copyWith(
      scaffoldBackgroundColor: AyurvedaColors.darkScaffold,
      extensions: const [
        AyurvedaThemeExtension(
          goldAccent: AyurvedaColors.gold,
          terracottaAccent: AyurvedaColors.terracotta,
          cardBorderColor: AyurvedaColors.darkBorder,
          heroGradientStart: Color(0xFF122822),
          heroGradientEnd: Color(0xFF091613),
        ),
      ],
      appBarTheme: const AppBarTheme(
        backgroundColor: AyurvedaColors.darkScaffold,
        foregroundColor: AyurvedaColors.darkInk,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AyurvedaColors.darkInk,
          fontSize: 20,
          fontWeight: FontWeight.w600,
          fontFamily: 'serif',
        ),
      ),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AyurvedaColors.forestLight,
        foregroundColor: Colors.white,
        elevation: 2,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AyurvedaColors.forestLight,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          elevation: 1,
        ),
      ),
      textTheme: base.textTheme
          .copyWith(
            displayLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            displayMedium: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            headlineLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.bold,
            ),
            headlineMedium: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.w700,
            ),
            titleLarge: const TextStyle(
              fontFamily: 'serif',
              fontWeight: FontWeight.w700,
              fontSize: 20,
            ),
          )
          .apply(
            bodyColor: AyurvedaColors.darkInk,
            displayColor: AyurvedaColors.darkInk,
          ),
      cardTheme: CardThemeData(
        color: AyurvedaColors.darkSurface,
        surfaceTintColor: Colors.transparent,
        elevation: 1,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: AyurvedaColors.darkBorder),
        ),
        shadowColor: const Color(0x30000000),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AyurvedaColors.darkSurfaceRaised,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: _inputBorder(AyurvedaColors.darkBorder),
        enabledBorder: _inputBorder(AyurvedaColors.darkBorder),
        focusedBorder: _inputBorder(AyurvedaColors.sageLight, width: 2),
        errorBorder: _inputBorder(AyurvedaColors.danger),
        focusedErrorBorder: _inputBorder(AyurvedaColors.danger, width: 2),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AyurvedaColors.forestLight,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AyurvedaColors.sageLight,
          minimumSize: const Size.fromHeight(52),
          side: const BorderSide(color: AyurvedaColors.sageLight, width: 1.5),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AyurvedaColors.sageLight),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AyurvedaColors.darkSurface,
        indicatorColor: const Color(0xFF1B3D34),
        surfaceTintColor: Colors.transparent,
        elevation: 3,
        height: 72,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            size: 24,
            color: states.contains(WidgetState.selected)
                ? AyurvedaColors.sageLight
                : AyurvedaColors.darkInkMuted,
          ),
        ),
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => TextStyle(
            fontSize: 11,
            fontWeight: states.contains(WidgetState.selected)
                ? FontWeight.w600
                : FontWeight.w500,
            color: states.contains(WidgetState.selected)
                ? AyurvedaColors.sageLight
                : AyurvedaColors.darkInkMuted,
          ),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AyurvedaColors.darkBorder,
        thickness: 1,
      ),
      snackBarTheme: const SnackBarThemeData(
        backgroundColor: AyurvedaColors.darkSurfaceRaised,
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
