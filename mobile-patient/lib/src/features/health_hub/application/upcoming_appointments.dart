import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../appointments/domain/appointment_models.dart';
import '../../appointments/presentation/appointments_screen.dart';

/// Pending or approved appointments dated today or later, soonest first.
List<Appointment> upcomingAppointments(
  List<Appointment> appointments,
  DateTime now,
) {
  final today = DateTime(now.year, now.month, now.day);
  final upcoming = appointments.where((appointment) {
    final open =
        appointment.status == AppointmentStatus.pending ||
        appointment.status == AppointmentStatus.approved;
    final date = appointment.requestedDate;
    final day = DateTime(date.year, date.month, date.day);
    return open && !day.isBefore(today);
  }).toList()
    ..sort((a, b) => a.requestedDate.compareTo(b.requestedDate));
  return upcoming;
}

final upcomingAppointmentsProvider =
    Provider.autoDispose<AsyncValue<List<Appointment>>>((ref) {
      return ref
          .watch(myAppointmentsProvider)
          .whenData((items) => upcomingAppointments(items, DateTime.now()));
    });
