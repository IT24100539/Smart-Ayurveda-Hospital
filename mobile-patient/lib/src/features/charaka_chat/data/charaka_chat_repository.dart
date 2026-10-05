import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';

class CharakaAnswer {
  const CharakaAnswer({
    required this.answer,
    required this.refused,
    required this.workflowId,
    this.matchedTreatmentIds = const [],
  });

  final String answer;
  final bool refused;
  final String workflowId;
  final List<String> matchedTreatmentIds;
}

abstract class CharakaChatRepository {
  Future<CharakaAnswer> askTreatment(String question);
  Future<CharakaAnswer> askPatient(String question);
}

class DioCharakaChatRepository implements CharakaChatRepository {
  DioCharakaChatRepository(this._dio);

  final Dio _dio;

  @override
  Future<CharakaAnswer> askTreatment(String question) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/agent-workflows/ask-treatment',
        data: {'question': question},
        options: Options(receiveTimeout: const Duration(seconds: 45)),
      );
      final data = response.data ?? {};
      final matched = data['matchedTreatmentIds'] as List<dynamic>? ?? const [];
      return CharakaAnswer(
        answer: data['answer'] as String? ?? '',
        refused: data['refused'] as bool? ?? false,
        workflowId: data['workflowId']?.toString() ?? '',
        matchedTreatmentIds: matched.map((e) => e.toString()).toList(),
      );
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<CharakaAnswer> askPatient(String question) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/agent-workflows/ask-patient',
        data: {'question': question},
        options: Options(receiveTimeout: const Duration(seconds: 45)),
      );
      final data = response.data ?? {};
      return CharakaAnswer(
        answer: data['answer'] as String? ?? '',
        refused: data['refused'] as bool? ?? false,
        workflowId: data['workflowId']?.toString() ?? '',
      );
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final charakaChatRepositoryProvider = Provider<CharakaChatRepository>((ref) {
  return DioCharakaChatRepository(ref.watch(dioProvider));
});
