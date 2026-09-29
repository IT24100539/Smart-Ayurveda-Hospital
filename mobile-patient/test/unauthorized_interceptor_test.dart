import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/api_client.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';

class _CapturingErrorHandler extends ErrorInterceptorHandler {
  DioException? passed;

  @override
  void next(DioException err) {
    passed = err;
  }
}

DioException _error({
  required int status,
  required String path,
  String? token,
  DioExceptionType type = DioExceptionType.badResponse,
}) {
  final options = RequestOptions(
    path: path,
    method: 'GET',
    headers: {
      if (token != null) 'Authorization': 'Bearer $token',
    },
  );
  return DioException(
    requestOptions: options,
    type: type,
    response: Response<void>(
      requestOptions: options,
      statusCode: status,
    ),
  );
}

void main() {
  test('403 does not clear the session', () async {
    final storage = InMemoryTokenStorage('session-token');
    var unauthorizedCalls = 0;
    final interceptor = UnauthorizedInterceptor(
      tokenStorage: storage,
      onUnauthorized: () async => unauthorizedCalls++,
    );
    final handler = _CapturingErrorHandler();

    await interceptor.onError(
      _error(status: 403, path: '/feedback/staff', token: 'session-token'),
      handler,
    );

    expect(await storage.readToken(), 'session-token');
    expect(unauthorizedCalls, 0);
    expect(handler.passed?.response?.statusCode, 403);
  });

  test('401 without a bearer token does not clear the session', () async {
    final storage = InMemoryTokenStorage('session-token');
    var unauthorizedCalls = 0;
    final interceptor = UnauthorizedInterceptor(
      tokenStorage: storage,
      onUnauthorized: () async => unauthorizedCalls++,
    );

    await interceptor.onError(
      _error(status: 401, path: '/treatments/1/slots'),
      _CapturingErrorHandler(),
    );

    expect(await storage.readToken(), 'session-token');
    expect(unauthorizedCalls, 0);
  });

  test('401 on an auth endpoint does not clear the session', () async {
    final storage = InMemoryTokenStorage('session-token');
    var unauthorizedCalls = 0;
    final interceptor = UnauthorizedInterceptor(
      tokenStorage: storage,
      onUnauthorized: () async => unauthorizedCalls++,
    );

    await interceptor.onError(
      _error(status: 401, path: '/auth/login', token: 'session-token'),
      _CapturingErrorHandler(),
    );

    expect(await storage.readToken(), 'session-token');
    expect(unauthorizedCalls, 0);
  });

  test('401 with a bearer token on a patient route clears the session', () async {
    final storage = InMemoryTokenStorage('session-token');
    var unauthorizedCalls = 0;
    final interceptor = UnauthorizedInterceptor(
      tokenStorage: storage,
      onUnauthorized: () async => unauthorizedCalls++,
    );

    await interceptor.onError(
      _error(status: 401, path: '/appointments/me', token: 'session-token'),
      _CapturingErrorHandler(),
    );

    expect(await storage.readToken(), isNull);
    expect(unauthorizedCalls, 1);
  });
}
