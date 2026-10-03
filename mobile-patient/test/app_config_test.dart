import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/config/app_config.dart';

void main() {
  test('android emulator reaches the host through 10.0.2.2', () {
    expect(
      AppConfig.resolveApiBaseUrl(
        configured: '',
        isWeb: false,
        platform: TargetPlatform.android,
      ),
      'http://10.0.2.2:5080/api',
    );
  });

  test('web and desktop use localhost', () {
    expect(
      AppConfig.resolveApiBaseUrl(
        configured: '',
        isWeb: true,
        platform: TargetPlatform.android,
      ),
      'http://localhost:5080/api',
    );
    for (final platform in [
      TargetPlatform.windows,
      TargetPlatform.linux,
      TargetPlatform.macOS,
    ]) {
      expect(
        AppConfig.resolveApiBaseUrl(
          configured: '',
          isWeb: false,
          platform: platform,
        ),
        'http://localhost:5080/api',
      );
    }
  });

  test('dart-define wins on a physical device', () {
    const lan = 'http://192.168.1.20:5080/api';
    expect(
      AppConfig.resolveApiBaseUrl(
        configured: lan,
        isWeb: false,
        platform: TargetPlatform.android,
      ),
      lan,
    );
  });
}
