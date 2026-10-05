import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/doctor.dart';

abstract interface class DoctorsRepository {
  Future<List<Doctor>> listDoctors({String? query});
  Future<Doctor> getDoctor(String id);
  Future<Uint8List?> loadPhoto(String id);
}

class DioDoctorsRepository implements DoctorsRepository {
  const DioDoctorsRepository(this._dio);

  final Dio _dio;

  static const _pageSize = 100;

  @override
  Future<List<Doctor>> listDoctors({String? query}) async {
    final trimmed = query?.trim();
    final items = <Doctor>[];
    var received = 0;
    var page = 1;

    try {
      while (page <= 20) {
        final response = await _dio.get<Map<String, dynamic>>(
          '/doctors',
          queryParameters: {
            if (trimmed != null && trimmed.isNotEmpty) 'query': trimmed,
            'activeOnly': true,
            'page': page,
            'pageSize': _pageSize,
          },
        );
        final parsed = DoctorPage.fromJson(response.data!);
        received += parsed.items.length;
        items.addAll(parsed.items.where((doctor) => doctor.isActive));
        if (parsed.items.isEmpty || received >= parsed.totalCount) {
          break;
        }
        page++;
      }
      return items;
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<Doctor> getDoctor(String id) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>('/doctors/$id');
      return Doctor.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  /// Portrait bytes from the authorized photo route. Callers fall back to
  /// initials when this returns null.
  @override
  Future<Uint8List?> loadPhoto(String id) async {
    try {
      final response = await _dio.get<List<int>>(
        '/doctors/$id/photo',
        options: Options(responseType: ResponseType.bytes),
      );
      final data = response.data;
      if (data == null || data.isEmpty) return null;
      return Uint8List.fromList(data);
    } on DioException {
      return null;
    }
  }
}

final doctorsRepositoryProvider = Provider<DoctorsRepository>((ref) {
  return DioDoctorsRepository(ref.watch(dioProvider));
});
