enum AppointmentStatus {
  pending,
  approved,
  rejected,
  completed,
  cancelled;

  static AppointmentStatus fromWire(Object? value) {
    if (value is num && value >= 0 && value < values.length) {
      return values[value.toInt()];
    }
    final normalized = value?.toString().toLowerCase();
    return values.firstWhere(
      (status) => status.name == normalized,
      orElse: () => AppointmentStatus.pending,
    );
  }
}

class TreatmentBooking {
  const TreatmentBooking({
    required this.id,
    required this.name,
    this.durationMinutes,
    this.doctorName,
  });

  final String id;
  final String name;
  final int? durationMinutes;
  final String? doctorName;

  factory TreatmentBooking.fromRouteMap(Map<Object?, Object?> map) =>
      TreatmentBooking(
        id: (map['id'] ?? map['treatmentId']).toString(),
        name: (map['name'] ?? map['treatmentName'] ?? 'Treatment').toString(),
        durationMinutes: (map['durationMinutes'] as num?)?.toInt(),
        doctorName: (map['doctorName'] ?? map['allocatedDoctor'])?.toString(),
      );
}

class TreatmentSlot {
  const TreatmentSlot({
    required this.time,
    this.scheduleId,
    this.available = true,
    this.doctorName,
  });

  final String time;
  final String? scheduleId;
  final bool available;
  final String? doctorName;

  factory TreatmentSlot.fromJson(Map<String, dynamic> json) => TreatmentSlot(
    time: (json['timeSlot'] ?? json['time'] ?? json['label'] ?? '').toString(),
    scheduleId: (json['scheduleId'] ?? json['id'])?.toString(),
    available:
        json['available'] as bool? ?? json['isAvailable'] as bool? ?? true,
    doctorName:
        (json['doctorName'] ??
                json['allocatedDoctor'] ??
                json['practitionerName'] ??
                (json['doctor'] is Map
                    ? (json['doctor'] as Map)['name']
                    : null))
            ?.toString(),
  );
}

class TreatmentAvailability {
  const TreatmentAvailability({required this.available, required this.slots});

  final bool available;
  final List<TreatmentSlot> slots;

  factory TreatmentAvailability.fromJson(dynamic data) {
    if (data is List) {
      final slots = data
          .whereType<Map>()
          .map(
            (item) => TreatmentSlot.fromJson(Map<String, dynamic>.from(item)),
          )
          .where((slot) => slot.time.isNotEmpty)
          .toList();
      return TreatmentAvailability(
        available: slots.any((slot) => slot.available),
        slots: slots,
      );
    }
    final json = Map<String, dynamic>.from(data as Map);
    final rawSlots = json['slots'] ?? json['availableSlots'] ?? const [];
    final slots = rawSlots is List
        ? rawSlots
              .map((item) {
                if (item is String) return TreatmentSlot(time: item);
                return TreatmentSlot.fromJson(
                  Map<String, dynamic>.from(item as Map),
                );
              })
              .where((slot) => slot.time.isNotEmpty)
              .toList()
        : <TreatmentSlot>[];
    return TreatmentAvailability(
      available:
          json['available'] as bool? ??
          json['isAvailable'] as bool? ??
          slots.any((slot) => slot.available),
      slots: slots,
    );
  }
}

class Appointment {
  const Appointment({
    required this.id,
    required this.treatmentName,
    required this.requestedDate,
    required this.requestedTimeSlot,
    required this.status,
  });

  final String id;
  final String treatmentName;
  final DateTime requestedDate;
  final String requestedTimeSlot;
  final AppointmentStatus status;

  factory Appointment.fromJson(Map<String, dynamic> json) => Appointment(
    id: json['id'].toString(),
    treatmentName: json['treatmentName']?.toString() ?? 'Treatment',
    requestedDate: DateTime.parse(json['requestedDate'].toString()),
    requestedTimeSlot: json['requestedTimeSlot']?.toString() ?? '',
    status: AppointmentStatus.fromWire(json['status']),
  );
}
