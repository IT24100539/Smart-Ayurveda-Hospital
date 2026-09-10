import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import '../storage/token_storage.dart';

/// Attaches the stored JWT to every outgoing request.
class AuthHeaderInterceptor extends Interceptor {
  AuthHeaderInterceptor(this._tokenStorage);

  final TokenStorage _tokenStorage;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (!options.extra.containsKey(_skipAuthHeaderKey)) {
      final token = await _tokenStorage.readToken();
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }
}

/// Clears the stored token when the API rejects it, then notifies the app so
/// the router can send the patient back to the login screen.
///
/// Auth endpoints are exempt: `POST /auth/login` answers 401 for bad
/// credentials, which is a form validation failure rather than an expired
/// session.
class UnauthorizedInterceptor extends Interceptor {
  UnauthorizedInterceptor({
    required TokenStorage tokenStorage,
    required Future<void> Function() onUnauthorized,
  }) : _tokenStorage = tokenStorage,
       _onUnauthorized = onUnauthorized;

  final TokenStorage _tokenStorage;
  final Future<void> Function() _onUnauthorized;

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final isAuthEndpoint = err.requestOptions.path.contains('/auth/');
    if (err.response?.statusCode == 401 && !isAuthEndpoint) {
      await _tokenStorage.clear();
      await _onUnauthorized();
    }
    handler.next(err);
  }
}

const _skipAuthHeaderKey = 'skipAuthHeader';

/// Marks a request as anonymous so [AuthHeaderInterceptor] leaves it alone.
final anonymousRequest = Options(extra: const {_skipAuthHeaderKey: true});

/// Called by [UnauthorizedInterceptor] on an expired or rejected token.
///
/// Overridden in `app.dart` once the auth controller exists; the indirection
/// keeps the dio provider from depending on the auth feature.
final onUnauthorizedProvider = Provider<Future<void> Function()>(
  (ref) => () async {},
);

final dioProvider = Provider<Dio>((ref) {
  final tokenStorage = ref.watch(tokenStorageProvider);

  final dio = Dio(
    BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: AppConfig.connectTimeout,
      receiveTimeout: AppConfig.receiveTimeout,
      contentType: Headers.jsonContentType,
      // The API answers with `application/problem+json` on failure, which dio
      // must be allowed to hand to the error interceptors as decoded JSON.
      responseType: ResponseType.json,
    ),
  );

  dio.interceptors.addAll([
    AuthHeaderInterceptor(tokenStorage),
    UnauthorizedInterceptor(
      tokenStorage: tokenStorage,
      // Resolved lazily so the auth controller is only touched on a real 401.
      onUnauthorized: () => ref.read(onUnauthorizedProvider)(),
    ),
  ]);

  ref.onDispose(dio.close);
  return dio;
});
