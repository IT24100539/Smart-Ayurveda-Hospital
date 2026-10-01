import 'package:flutter/foundation.dart';

/// Compile-time configuration, supplied with `--dart-define`.
abstract final class AppConfig {
  /// Base URL of `Hospital.Api`, including the `/api` prefix.
  ///
  /// Override per environment with:
  /// `flutter run --dart-define=API_BASE_URL=https://192.168.1.100:7443/api`
  /// or for HTTP:
  /// `flutter run --dart-define=API_BASE_URL=http://192.168.1.100:5080/api`
  ///
  /// Per-platform defaults:
  /// - Android emulator: `https://10.0.2.2:7443/api`
  /// - Web / desktop / iOS simulator: `https://localhost:7443/api`
  static const configuredApiBaseUrl = String.fromEnvironment('API_BASE_URL');

  static String get apiBaseUrl {
    if (configuredApiBaseUrl.isNotEmpty) return configuredApiBaseUrl;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'https://10.0.2.2:7443/api';
    }
    return 'https://localhost:7443/api';
  }

  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 20);

  /// Mirrors `RegisterRequestValidator` / `LoginRequestValidator` on the API.
  static const minPasswordLength = 8;
  static const maxPasswordLength = 128;
}
