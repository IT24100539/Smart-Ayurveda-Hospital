import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/treatment_models.dart';

class TreatmentsRepository {
  const TreatmentsRepository(this._dio);

  final Dio _dio;

  Future<PaginatedResponse<Treatment>> getTreatments() async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/treatments',
        options: anonymousRequest,
      );
      return PaginatedResponse.fromJson(response.data!, Treatment.fromJson);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  Future<TreatmentAvailability> checkAvailability(String id, String date) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/treatments/$id/availability',
        queryParameters: {'date': date},
        options: anonymousRequest,
      );
      return TreatmentAvailability.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final treatmentsRepositoryProvider = Provider<TreatmentsRepository>((ref) {
  return TreatmentsRepository(ref.watch(dioProvider));
});
