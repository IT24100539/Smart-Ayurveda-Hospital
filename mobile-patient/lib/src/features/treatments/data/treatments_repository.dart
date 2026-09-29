import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/treatment_models.dart';

abstract interface class TreatmentsRepository {
  Future<PaginatedResponse<Treatment>> getTreatments();
  Future<TreatmentAvailability> checkAvailability(String id, String date);
  Future<TreatmentAskResult> askTreatmentInfo(String question);
}

class DioTreatmentsRepository implements TreatmentsRepository {
  const DioTreatmentsRepository(this._dio);

  final Dio _dio;

  @override
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

  @override
  Future<TreatmentAvailability> checkAvailability(String id, String date) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/treatments/$id/slots',
        queryParameters: {'date': date},
        options: anonymousRequest,
      );
      return TreatmentAvailability.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  /// Asks the treatment-info agent about listed therapies, days, and fees.
  /// Requires a patient JWT. Agent replies can take longer than catalogue reads.
  @override
  Future<TreatmentAskResult> askTreatmentInfo(String question) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/agent-workflows/ask-treatment',
        data: {'question': question},
        options: Options(receiveTimeout: const Duration(seconds: 90)),
      );
      return TreatmentAskResult.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final treatmentsRepositoryProvider = Provider<TreatmentsRepository>((ref) {
  return DioTreatmentsRepository(ref.watch(dioProvider));
});
