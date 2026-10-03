import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/api_exception.dart';
import 'package:patient_app/src/features/auth/data/auth_repository.dart';

void main() {
  late Dio dio;
  late AuthRepository repo;
  late String? lastPath;
  late Map<String, dynamic>? lastBody;
  late int statusCode;
  late Object? responseData;

  setUp(() {
    lastPath = null;
    lastBody = null;
    statusCode = 200;
    responseData = {'message': 'ok'};
    dio = Dio(BaseOptions(baseUrl: 'http://test/api'));
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          lastPath = options.path;
          lastBody = Map<String, dynamic>.from(options.data as Map);
          if (statusCode >= 400) {
            handler.reject(
              DioException(
                requestOptions: options,
                response: Response(
                  requestOptions: options,
                  statusCode: statusCode,
                  data: responseData,
                ),
                type: DioExceptionType.badResponse,
              ),
            );
            return;
          }
          handler.resolve(
            Response(
              requestOptions: options,
              statusCode: statusCode,
              data: responseData,
            ),
          );
        },
      ),
    );
    repo = AuthRepository(dio);
  });

  test('requestReset posts to /auth/request-reset', () async {
    await repo.requestReset(email: 'patient@hospital.lk');
    expect(lastPath, '/auth/request-reset');
    expect(lastBody, {'email': 'patient@hospital.lk'});
  });

  test('completeReset posts to /auth/complete-reset', () async {
    await repo.completeReset(
      email: 'patient@hospital.lk',
      token: 'tok',
      newPassword: 'Abcdefg1!',
      confirmPassword: 'Abcdefg1!',
    );
    expect(lastPath, '/auth/complete-reset');
    expect(lastBody, {
      'email': 'patient@hospital.lk',
      'token': 'tok',
      'newPassword': 'Abcdefg1!',
      'confirmPassword': 'Abcdefg1!',
    });
  });

  test('completeReset maps API failures to ApiException', () async {
    statusCode = 400;
    responseData = {
      'detail': 'Invalid or expired password reset token.',
    };
    expect(
      () => repo.completeReset(
        email: 'patient@hospital.lk',
        token: 'old',
        newPassword: 'Abcdefg1!',
        confirmPassword: 'Abcdefg1!',
      ),
      throwsA(isA<ApiException>()),
    );
  });
}
