import 'package:dio/dio.dart';

/// A failure from `Hospital.Api`, normalised out of its `application/problem+json`
/// responses (see `Hospital.Api/Middleware/ExceptionHandlingMiddleware.cs`).
class ApiException implements Exception {
  const ApiException({
    required this.statusCode,
    this.detail,
    this.fieldErrors = const {},
    this.isNetworkError = false,
  });

  final int? statusCode;

  /// `detail` from ProblemDetails, e.g. "Invalid email or password.".
  final String? detail;

  /// `errors` from a 400 validation response. FluentValidation emits PascalCase
  /// property names, so keys are lower-cased here to make lookups predictable.
  final Map<String, List<String>> fieldErrors;

  final bool isNetworkError;

  bool get isUnauthorized => statusCode == 401;
  bool get isConflict => statusCode == 409;

  /// First validation message for [field], matched case-insensitively.
  String? fieldError(String field) {
    final messages = fieldErrors[field.toLowerCase()];
    return (messages == null || messages.isEmpty) ? null : messages.first;
  }

  /// Best available human-readable message, or `null` to let the UI fall back
  /// to a localized default.
  String? get message {
    if (detail != null) return detail;
    for (final messages in fieldErrors.values) {
      if (messages.isNotEmpty) return messages.first;
    }
    return null;
  }

  factory ApiException.fromDioException(DioException error) {
    const networkTypes = {
      DioExceptionType.connectionTimeout,
      DioExceptionType.sendTimeout,
      DioExceptionType.receiveTimeout,
      DioExceptionType.connectionError,
      DioExceptionType.unknown,
    };

    if (networkTypes.contains(error.type)) {
      return ApiException(
        statusCode: error.response?.statusCode,
        isNetworkError: true,
      );
    }

    final body = error.response?.data;
    if (body is! Map) {
      return ApiException(statusCode: error.response?.statusCode);
    }

    final rawErrors = body['errors'];
    final fieldErrors = <String, List<String>>{};
    if (rawErrors is Map) {
      rawErrors.forEach((key, value) {
        final messages = value is List
            ? value.map((m) => m.toString()).toList()
            : [value.toString()];
        fieldErrors[key.toString().toLowerCase()] = messages;
      });
    }

    final detail = body['detail'] ?? body['title'];

    return ApiException(
      statusCode: error.response?.statusCode,
      detail: detail is String ? detail : null,
      fieldErrors: fieldErrors,
    );
  }

  @override
  String toString() => 'ApiException($statusCode): ${message ?? 'no detail'}';
}
