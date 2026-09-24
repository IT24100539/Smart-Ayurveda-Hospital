import 'package:flutter/foundation.dart';

/// Compile-time configuration, supplied with `--dart-define`.
abstract final class AppConfig {
  /// Base URL of `Hospital.Api`, including the `/api` prefix.
  ///
  /// Override per environment, e.g.
  /// `flutter run --dart-define=API_BASE_URL=http://192.168.1.5:5080/api`.
  ///
  /// With no override, Chrome uses the API on this machine. The Android
  /// emulator reaches that same port through `10.0.2.2`
  /// (see `backend/src/Hospital.Api/Properties/launchSettings.json`).
  static const configuredApiBaseUrl = String.fromEnvironment('API_BASE_URL');

  static String get apiBaseUrl {
    if (configuredApiBaseUrl.isNotEmpty) return configuredApiBaseUrl;
    if (kIsWeb) return 'http://localhost:5080/api';
    return 'http://10.0.2.2:5080/api';
  }

  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 20);

  /// Mirrors `RegisterRequestValidator` / `LoginRequestValidator` on the API.
  static const minPasswordLength = 8;
  static const maxPasswordLength = 128;
}
