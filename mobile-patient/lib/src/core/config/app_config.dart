/// Compile-time configuration, supplied with `--dart-define`.
abstract final class AppConfig {
  /// Base URL of `Hospital.Api`, including the `/api` prefix.
  ///
  /// Override per environment, e.g.
  /// `flutter run --dart-define=API_BASE_URL=http://192.168.1.5:5080/api`.
  ///
  /// The default targets `10.0.2.2`, which is how the Android emulator reaches
  /// the host machine's `localhost`, on the port `Hospital.Api` binds in
  /// development (see `backend/src/Hospital.Api/Properties/launchSettings.json`).
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080/api',
  );

  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 20);

  /// Mirrors `RegisterRequestValidator` / `LoginRequestValidator` on the API.
  static const minPasswordLength = 8;
  static const maxPasswordLength = 128;
}
