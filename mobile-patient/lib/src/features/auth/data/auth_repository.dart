import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/auth_models.dart';

class AuthRepository {
  const AuthRepository(this._dio);

  final Dio _dio;

  Future<AuthResult> login({required String email, required String password}) {
    return _post('/auth/login', {'email': email, 'password': password});
  }

  /// `role` is deliberately omitted: the API forces [UserRole.patient] for
  /// anonymous callers and only lets an authenticated admin choose otherwise.
  Future<AuthResult> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
    required String dateOfBirth,
    required String gender,
  }) {
    return _post('/auth/register', {
      'fullName': fullName,
      'email': email,
      'phoneNumber': phoneNumber,
      'password': password,
      'dateOfBirth': dateOfBirth,
      'gender': gender,
    });
  }

  /// `POST /auth/request-reset`. Always succeeds from the caller's point of
  /// view when the API is reachable — the same generic body is returned
  /// whether or not the email is registered.
  Future<void> requestReset({required String email}) {
    return _postAnonymous('/auth/request-reset', {'email': email});
  }

  /// `POST /auth/complete-reset`. Revokes the token and invalidates sessions
  /// on the API when the token is valid.
  Future<void> completeReset({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  }) {
    return _postAnonymous('/auth/complete-reset', {
      'email': email,
      'token': token,
      'newPassword': newPassword,
      'confirmPassword': confirmPassword,
    });
  }

  Future<AuthResult> _post(String path, Map<String, dynamic> body) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        path,
        data: body,
        options: anonymousRequest,
      );
      return AuthResult.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  Future<void> _postAnonymous(String path, Map<String, dynamic> body) async {
    try {
      await _dio.post<Map<String, dynamic>>(
        path,
        data: body,
        options: anonymousRequest,
      );
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(dioProvider));
});
