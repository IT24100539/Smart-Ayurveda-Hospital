import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/router/app_routes.dart';

void main() {
  test('drops return paths that would redirect back to splash or off-app', () {
    expect(AppRoutes.sanitizeReturnPath('/home'), '/home');
    expect(
      AppRoutes.sanitizeReturnPath('/treatments/abc/book?name=Abhyanga'),
      '/treatments/abc/book?name=Abhyanga',
    );
    expect(AppRoutes.sanitizeReturnPath('/'), isNull);
    expect(AppRoutes.sanitizeReturnPath('/?from=/home'), isNull);
    expect(AppRoutes.sanitizeReturnPath('/login?returnPath=/home'), isNull);
    expect(AppRoutes.sanitizeReturnPath('https://evil.example/phish'), isNull);
    expect(AppRoutes.sanitizeReturnPath('//evil.example'), isNull);
  });
}
