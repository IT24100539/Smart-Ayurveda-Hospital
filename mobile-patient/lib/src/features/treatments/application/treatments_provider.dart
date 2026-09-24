import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/treatments_repository.dart';
import '../domain/treatment_models.dart';

/// Pure function to format day tags from day names
String formatDayTag(String dayName) {
  switch (dayName) {
    case 'Monday':
      return 'Mon';
    case 'Tuesday':
      return 'Tue';
    case 'Wednesday':
      return 'Wed';
    case 'Thursday':
      return 'Thu';
    case 'Friday':
      return 'Fri';
    case 'Saturday':
      return 'Sat';
    case 'Sunday':
      return 'Sun';
    default:
      return dayName;
  }
}

/// Pure function to determine if a treatment is available today
bool isAvailableToday(List<String> scheduleDays, DateTime today) {
  const weekdayNames = [
    'Monday', 'Tuesday', 'Wednesday', 'Thursday',
    'Friday', 'Saturday', 'Sunday'
  ];
  final todayName = weekdayNames[today.weekday - 1];
  return scheduleDays.contains(todayName);
}

final treatmentsSearchQueryProvider = StateProvider<String>((ref) => '');

final treatmentsProvider = FutureProvider<List<Treatment>>((ref) async {
  final repository = ref.watch(treatmentsRepositoryProvider);
  final response = await repository.getTreatments();
  return response.items;
});

final filteredTreatmentsProvider = Provider<AsyncValue<List<Treatment>>>((ref) {
  final treatmentsState = ref.watch(treatmentsProvider);
  final searchQuery = ref.watch(treatmentsSearchQueryProvider).toLowerCase();

  return treatmentsState.whenData((treatments) {
    if (searchQuery.isEmpty) return treatments;
    return treatments.where((t) {
      return t.nameEnglish.toLowerCase().contains(searchQuery) ||
          t.nameSinhala.toLowerCase().contains(searchQuery);
    }).toList();
  });
});
