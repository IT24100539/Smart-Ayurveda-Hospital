import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/health_hub_models.dart';

abstract interface class HealthHubRepository {
  Future<List<TreatmentPlan>> getTreatmentPlans();
  Future<RegistrationSummary> getRegistrationSummary();
}

class ApiHealthHubRepository implements HealthHubRepository {
  const ApiHealthHubRepository(this._dio);
  final Dio _dio;

  @override
  Future<List<TreatmentPlan>> getTreatmentPlans() async {
    try {
      final response = await _dio.get<dynamic>('/patients/me/treatment-plans');
      final data = response.data;
      if (data is List) {
        return data
            .map((item) => TreatmentPlan.fromJson(Map<String, dynamic>.from(item as Map)))
            .toList();
      }
      return const [];
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<RegistrationSummary> getRegistrationSummary() async {
    try {
      final response = await _dio.get<Map<String, dynamic>>('/patients/me/registration-summary');
      return RegistrationSummary.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final healthHubRepositoryProvider = Provider<HealthHubRepository>((ref) {
  return ApiHealthHubRepository(ref.watch(dioProvider));
});

final treatmentPlansProvider = FutureProvider.autoDispose<List<TreatmentPlan>>((ref) {
  return ref.watch(healthHubRepositoryProvider).getTreatmentPlans();
});

final registrationSummaryProvider = FutureProvider.autoDispose<RegistrationSummary>((ref) {
  return ref.watch(healthHubRepositoryProvider).getRegistrationSummary();
});
