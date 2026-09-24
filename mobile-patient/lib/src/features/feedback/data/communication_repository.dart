import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/communication_models.dart';

class CommunicationRepository {
  const CommunicationRepository(this._dio);

  final Dio _dio;

  Future<List<PublicFeedback>> publicFeed() {
    return _getList('/feedback', PublicFeedback.fromJson);
  }

  Future<void> submitFeedback({
    required int rating,
    required String comment,
    required bool isAnonymous,
    String? appointmentId,
    String? treatmentId,
  }) {
    return _send(
      () => _dio.post<dynamic>(
        '/feedback',
        data: {
          'rating': rating,
          'comment': comment,
          'isAnonymous': isAnonymous,
          'appointmentId': ?appointmentId,
          'treatmentId': ?treatmentId,
        },
      ),
    );
  }

  Future<void> react(String feedbackId, ReactionType reaction) {
    return _send(
      () => _dio.post<dynamic>(
        '/feedback/$feedbackId/reactions',
        data: {'reactionType': reaction.wireName},
      ),
    );
  }

  Future<void> removeReaction(String feedbackId) {
    return _send(() => _dio.delete<dynamic>('/feedback/$feedbackId/reactions'));
  }

  Future<void> replyToFeedback(String feedbackId, String reply) {
    return _send(
      () => _dio.post<dynamic>(
        '/feedback/$feedbackId/replies/patient',
        data: {'reply': reply},
      ),
    );
  }

  Future<List<PatientFeedback>> myFeedback() {
    return _getList('/feedback/mine', PatientFeedback.fromJson);
  }

  Future<void> updateFeedback({
    required String id,
    int? rating,
    String? comment,
    bool? isAnonymous,
    bool withdraw = false,
  }) {
    return _send(
      () => _dio.patch<dynamic>(
        '/feedback/$id',
        data: {
          'rating': ?rating,
          'comment': ?comment,
          'isAnonymous': ?isAnonymous,
          'withdraw': withdraw,
        },
      ),
    );
  }

  Future<List<PatientComplaint>> myComplaints() {
    return _getList('/complaints/me', PatientComplaint.fromJson);
  }

  Future<void> submitComplaint({
    required String subject,
    required String description,
    required ComplaintPriority priority,
  }) {
    return _send(
      () => _dio.post<dynamic>(
        '/complaints',
        data: {
          'subject': subject,
          'description': description,
          'priority': priority.wireName,
        },
      ),
    );
  }

  Future<List<PatientNotification>> notifications() {
    return _getList('/notifications/me', PatientNotification.fromJson);
  }

  Future<void> markNotificationRead(String id) {
    return _send(() => _dio.patch<dynamic>('/notifications/$id/read'));
  }

  Future<void> markAllNotificationsRead() {
    return _send(() => _dio.patch<dynamic>('/notifications/read-all'));
  }

  Future<List<T>> _getList<T>(
    String path,
    T Function(Map<String, dynamic>) parse,
  ) async {
    try {
      final response = await _dio.get<dynamic>(path);
      final raw = response.data;
      if (raw is! List) return <T>[];
      return raw
          .map((item) => parse(Map<String, dynamic>.from(item as Map)))
          .toList();
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  Future<void> _send(Future<Response<dynamic>> Function() request) async {
    try {
      await request();
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final communicationRepositoryProvider = Provider<CommunicationRepository>((
  ref,
) {
  return CommunicationRepository(ref.watch(dioProvider));
});
