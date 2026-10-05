import 'package:flutter/foundation.dart';

/// Compile-time configuration, supplied with `--dart-define`.
abstract final class AppConfig {
  /// Base URL of `Hospital.Api`, including the `/api` prefix.
  ///
  /// A physical device has no loopback alias to this machine. Pass the LAN
  /// address explicitly:
  /// `flutter run --dart-define=API_BASE_URL=http://192.168.1.100:5080/api`
  ///
  /// With no override the local HTTP port is used, which matches the API
  /// launch profile and does not depend on the ASP.NET development certificate:
  /// - Android emulator: `http://10.0.2.2:5080/api`
  /// - Web, desktop, and the iOS simulator: `http://localhost:5080/api`
  static const configuredApiBaseUrl = String.fromEnvironment('API_BASE_URL');

  static String get apiBaseUrl => resolveApiBaseUrl(
        configured: configuredApiBaseUrl,
        isWeb: kIsWeb,
        platform: defaultTargetPlatform,
      );

  /// [configured] is the `--dart-define=API_BASE_URL` value. It wins on every
  /// platform, including a physical phone.
  static String resolveApiBaseUrl({
    required String configured,
    required bool isWeb,
    required TargetPlatform platform,
  }) {
    final override = configured.trim();
    if (override.isNotEmpty) return override;
    if (!isWeb && platform == TargetPlatform.android) {
      return 'http://10.0.2.2:5080/api';
    }
    return 'http://localhost:5080/api';
  }

  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 20);

  /// Mirrors `PasswordPolicy` in Hospital.Api `appsettings.json`.
  /// Override with `--dart-define=PASSWORD_MIN_LENGTH=10` and the matching
  /// `PASSWORD_REQUIRE_*` flags when the API policy changes.
  static const minPasswordLength = int.fromEnvironment(
    'PASSWORD_MIN_LENGTH',
    defaultValue: 8,
  );
  static const maxPasswordLength = int.fromEnvironment(
    'PASSWORD_MAX_LENGTH',
    defaultValue: 128,
  );
  static const passwordRequireUppercase = bool.fromEnvironment(
    'PASSWORD_REQUIRE_UPPERCASE',
    defaultValue: true,
  );
  static const passwordRequireLowercase = bool.fromEnvironment(
    'PASSWORD_REQUIRE_LOWERCASE',
    defaultValue: true,
  );
  static const passwordRequireDigit = bool.fromEnvironment(
    'PASSWORD_REQUIRE_DIGIT',
    defaultValue: true,
  );
  static const passwordRequireSpecial = bool.fromEnvironment(
    'PASSWORD_REQUIRE_SPECIAL',
    defaultValue: true,
  );
}
