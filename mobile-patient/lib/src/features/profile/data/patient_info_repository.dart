import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';

class PatientInfoAskResult {
  const PatientInfoAskResult({
    required this.answer,
    required this.refused,
    required this.workflowId,
  });

  final String answer;
  final bool refused;
  final String workflowId;

  factory PatientInfoAskResult.fromJson(Map<String, dynamic> json) {
    return PatientInfoAskResult(
      answer: json['answer'] as String? ?? '',
      refused: json['refused'] as bool? ?? false,
      workflowId: json['workflowId'] as String? ?? '',
    );
  }
}

abstract class PatientInfoRepository {
  Future<PatientInfoAskResult> askPatientInfo(String question);
}

class DioPatientInfoRepository implements PatientInfoRepository {
  DioPatientInfoRepository(this._dio);

  final Dio _dio;

  @override
  Future<PatientInfoAskResult> askPatientInfo(String question) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/agent-workflows/ask-patient',
        data: {'question': question},
        options: Options(receiveTimeout: const Duration(seconds: 90)),
      );
      return PatientInfoAskResult.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final patientInfoRepositoryProvider = Provider<PatientInfoRepository>((ref) {
  return DioPatientInfoRepository(ref.watch(dioProvider));
});
