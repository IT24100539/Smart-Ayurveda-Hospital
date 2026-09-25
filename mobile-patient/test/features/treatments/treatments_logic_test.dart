import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';

void main() {
  group('formatDayTag', () {
    test('formats day names correctly', () {
      expect(formatDayTag('Monday'), 'Mon');
      expect(formatDayTag('Wednesday'), 'Wed');
      expect(formatDayTag('Friday'), 'Fri');
      expect(formatDayTag('Sunday'), 'Sun');
      expect(formatDayTag('NotADay'), 'NotADay');
    });
  });

  group('isAvailableToday', () {
    test('returns true when today is in the schedule days', () {
      final today = DateTime(2023, 10, 16); // Monday
      final scheduleDays = ['Monday', 'Wednesday', 'Friday'];
      expect(isAvailableToday(scheduleDays, today), isTrue);
    });

    test('returns false when today is not in the schedule days', () {
      final today = DateTime(2023, 10, 17); // Tuesday
      final scheduleDays = ['Monday', 'Wednesday', 'Friday'];
      expect(isAvailableToday(scheduleDays, today), isFalse);
    });

    test('returns false when schedule days are empty', () {
      final today = DateTime(2023, 10, 16); // Monday
      expect(isAvailableToday([], today), isFalse);
    });
  });
}