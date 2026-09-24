import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/appointment_models.dart';

abstract interface class AppointmentRepository {
  Future<TreatmentAvailability> availability(String treatmentId, DateTime date);
  Future<Appointment> create({
    required String patientId,
    required TreatmentBooking treatment,
    required DateTime date,
    required TreatmentSlot slot,
  });
  Future<List<Appointment>> mine();
  Future<void> cancel(String appointmentId);
}

class ApiAppointmentRepository implements AppointmentRepository {
  const ApiAppointmentRepository(this._dio);
  final Dio _dio;

  @override
  Future<TreatmentAvailability> availability(
    String treatmentId,
    DateTime date,
  ) async {
    try {
      final response = await _dio.get<dynamic>(
        '/treatments/$treatmentId/availability',
        queryParameters: {'date': DateFormat('yyyy-MM-dd').format(date)},
      );
      return TreatmentAvailability.fromJson(response.data);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<Appointment> create({
    required String patientId,
    required TreatmentBooking treatment,
    required DateTime date,
    required TreatmentSlot slot,
  }) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/appointments',
        data: {
          'patientId': patientId,
          'treatmentId': treatment.id,
          'scheduleId': slot.scheduleId,
          'requestedDate': DateFormat('yyyy-MM-dd').format(date),
          'requestedTimeSlot': slot.time,
        },
      );
      return Appointment.fromJson(response.data!);
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<List<Appointment>> mine() async {
    try {
      final response = await _dio.get<dynamic>('/appointments/me');
      final data = response.data;
      final rawItems = data is List
          ? data
          : (data as Map)['items'] as List? ?? const [];
      return rawItems
          .map(
            (item) =>
                Appointment.fromJson(Map<String, dynamic>.from(item as Map)),
          )
          .toList();
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  @override
  Future<void> cancel(String appointmentId) async {
    try {
      await _dio.patch<void>('/appointments/$appointmentId/cancel');
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

final appointmentRepositoryProvider = Provider<AppointmentRepository>(
  (ref) => ApiAppointmentRepository(ref.watch(dioProvider)),
);
