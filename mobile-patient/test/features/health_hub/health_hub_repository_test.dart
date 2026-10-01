import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/health_hub/domain/health_hub_models.dart';

void main() {
  group('TreatmentPlan domain model parsing', () {
    test('parses complete treatment plan correctly', () {
      final json = {
        'treatmentId': '550e8400-e29b-41d4-a716-446655440000',
        'treatmentName': 'Panchakarma Cleanse',
        'sessionCount': 3,
        'lastStatus': 'Approved',
        'nextDate': '2026-10-15',
        'sessions': [
          {
            'appointmentId': '11111111-1111-1111-1111-111111111111',
            'date': '2026-10-01',
            'timeSlot': '09:00 - 10:00',
            'status': 'Completed',
          },
          {
            'appointmentId': '22222222-2222-2222-2222-222222222222',
            'date': '2026-10-15',
            'timeSlot': '10:00 - 11:00',
            'status': 'Approved',
          },
        ],
      };

      final plan = TreatmentPlan.fromJson(json);

      expect(plan.treatmentId, '550e8400-e29b-41d4-a716-446655440000');
      expect(plan.treatmentName, 'Panchakarma Cleanse');
      expect(plan.sessionCount, 3);
      expect(plan.lastStatus, AppointmentStatus.approved);
      expect(plan.nextDate, DateTime(2026, 10, 15));
      expect(plan.sessions.length, 2);
      expect(plan.sessions[0].appointmentId, '11111111-1111-1111-1111-111111111111');
      expect(plan.sessions[0].status, AppointmentStatus.completed);
      expect(plan.sessions[0].timeSlot, '09:00 - 10:00');
    });

    test('handles null nextDate and empty sessions gracefully', () {
      final json = {
        'treatmentId': '550e8400-e29b-41d4-a716-446655440000',
        'treatmentName': 'Shirodhara Relax',
        'sessionCount': 0,
        'lastStatus': 'Cancelled',
        'nextDate': null,
        'sessions': <dynamic>[],
      };

      final plan = TreatmentPlan.fromJson(json);

      expect(plan.nextDate, isNull);
      expect(plan.sessions, isEmpty);
      expect(plan.lastStatus, AppointmentStatus.cancelled);
    });
  });

  group('RegistrationSummary domain model parsing', () {
    test('parses full registration summary correctly', () {
      final json = {
        'uhid': 'SAH-2026-0042',
        'fullName': 'Kasun Bandara',
        'phone': '+94771234567',
        'email': 'kasun@ayurveda.test',
        'dateOfBirth': '1988-06-15',
        'gender': 'Male',
        'prakriti': 'Pitta-Kapha',
        'vikriti': 'Vata',
        'allergies': 'Penicillin, Mustard Seeds',
        'bloodGroup': 'B+',
      };

      final summary = RegistrationSummary.fromJson(json);

      expect(summary.uhid, 'SAH-2026-0042');
      expect(summary.fullName, 'Kasun Bandara');
      expect(summary.phone, '+94771234567');
      expect(summary.email, 'kasun@ayurveda.test');
      expect(summary.dateOfBirth, DateTime(1988, 6, 15));
      expect(summary.gender, 'Male');
      expect(summary.prakriti, 'Pitta-Kapha');
      expect(summary.vikriti, 'Vata');
      expect(summary.allergies, 'Penicillin, Mustard Seeds');
      expect(summary.bloodGroup, 'B+');
    });

    test('handles null email, allergies, and blood group', () {
      final json = {
        'uhid': 'SAH-2026-0099',
        'fullName': 'Nimali Senanayake',
        'phone': '0719876543',
        'email': null,
        'dateOfBirth': '1995-12-01',
        'gender': 'Female',
        'prakriti': 'Vata',
        'vikriti': 'Pitta',
        'allergies': null,
        'bloodGroup': null,
      };

      final summary = RegistrationSummary.fromJson(json);

      expect(summary.email, isNull);
      expect(summary.allergies, isNull);
      expect(summary.bloodGroup, isNull);
      expect(summary.uhid, 'SAH-2026-0099');
    });
  });
}
