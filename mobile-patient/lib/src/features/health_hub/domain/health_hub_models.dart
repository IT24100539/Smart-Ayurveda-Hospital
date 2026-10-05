import '../../appointments/domain/appointment_models.dart';

class TreatmentSession {
  const TreatmentSession({
    required this.appointmentId,
    required this.date,
    required this.timeSlot,
    required this.status,
  });

  final String appointmentId;
  final DateTime date;
  final String timeSlot;
  final AppointmentStatus status;

  factory TreatmentSession.fromJson(Map<String, dynamic> json) {
    return TreatmentSession(
      appointmentId: (json['appointmentId'] ?? json['id'] ?? '').toString(),
      date: DateTime.tryParse(json['date']?.toString() ?? '') ?? DateTime.now(),
      timeSlot: (json['timeSlot'] ?? '').toString(),
      status: AppointmentStatus.fromWire(json['status']),
    );
  }
}

class TreatmentPlan {
  const TreatmentPlan({
    required this.treatmentId,
    required this.treatmentName,
    required this.sessionCount,
    required this.lastStatus,
    required this.nextDate,
    required this.sessions,
  });

  final String treatmentId;
  final String treatmentName;
  final int sessionCount;
  final AppointmentStatus lastStatus;
  final DateTime? nextDate;
  final List<TreatmentSession> sessions;

  factory TreatmentPlan.fromJson(Map<String, dynamic> json) {
    final rawSessions = json['sessions'] as List<dynamic>? ?? const [];
    final nextDateRaw = json['nextDate'];
    return TreatmentPlan(
      treatmentId: (json['treatmentId'] ?? '').toString(),
      treatmentName: (json['treatmentName'] ?? '').toString(),
      sessionCount: (json['sessionCount'] as num?)?.toInt() ?? rawSessions.length,
      lastStatus: AppointmentStatus.fromWire(json['lastStatus']),
      nextDate: nextDateRaw != null && nextDateRaw.toString().isNotEmpty
          ? DateTime.tryParse(nextDateRaw.toString())
          : null,
      sessions: rawSessions
          .map((s) => TreatmentSession.fromJson(Map<String, dynamic>.from(s as Map)))
          .toList(),
    );
  }
}

class RegistrationSummary {
  const RegistrationSummary({
    required this.uhid,
    required this.fullName,
    required this.phone,
    this.email,
    required this.dateOfBirth,
    required this.gender,
    required this.prakriti,
    required this.vikriti,
    this.allergies,
    this.bloodGroup,
  });

  final String uhid;
  final String fullName;
  final String phone;
  final String? email;
  final DateTime dateOfBirth;
  final String gender;
  final String prakriti;
  final String vikriti;
  final String? allergies;
  final String? bloodGroup;

  factory RegistrationSummary.fromJson(Map<String, dynamic> json) {
    return RegistrationSummary(
      uhid: (json['uhid'] ?? '').toString(),
      fullName: (json['fullName'] ?? '').toString(),
      phone: (json['phone'] ?? '').toString(),
      email: json['email']?.toString(),
      dateOfBirth: DateTime.tryParse(json['dateOfBirth']?.toString() ?? '') ?? DateTime(1970),
      gender: (json['gender'] ?? '').toString(),
      prakriti: (json['prakriti'] ?? '').toString(),
      vikriti: (json['vikriti'] ?? '').toString(),
      allergies: json['allergies']?.toString(),
      bloodGroup: json['bloodGroup']?.toString(),
    );
  }
}
