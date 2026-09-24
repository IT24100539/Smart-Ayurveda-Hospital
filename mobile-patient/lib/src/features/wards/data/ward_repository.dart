import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/ward.dart';

abstract interface class WardRepository {
  Future<List<Ward>> list();
  Future<void> requestAdmission({
    required String patientId,
    required String wardId,
    required String reason,
    required DateTime preferredDate,
  });
}

class ApiWardRepository implements WardRepository {
  const ApiWardRepository(this._dio);
  final Dio _dio;

  @override
  Future<List<Ward>> list() async {
    try {
      final response = await _dio.get<List<dynamic>>('/wards');
      // Deliberately parse aggregate fields only. The patient UI never retains
      // or renders the backend's staff-only bed collection.
      return (response.data ?? const [])
          .map((item) => Ward.fromJson(Map<String, dynamic>.from(item as Map)))
          .toList();
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<void> requestAdmission({
    required String patientId,
    required String wardId,
    required String reason,
    required DateTime preferredDate,
  }) async {
    try {
      await _dio.post<dynamic>(
        '/admissions',
        data: {
          'patientId': patientId,
          'wardId': wardId,
          'reason': reason,
          'preferredDate': DateFormat('yyyy-MM-dd').format(preferredDate),
          'requestedByAgent': false,
        },
      );
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final wardRepositoryProvider = Provider<WardRepository>(
  (ref) => ApiWardRepository(ref.watch(dioProvider)),
);
final wardsProvider = FutureProvider.autoDispose<List<Ward>>(
  (ref) => ref.watch(wardRepositoryProvider).list(),
);
