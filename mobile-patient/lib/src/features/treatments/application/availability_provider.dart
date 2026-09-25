import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/treatments_repository.dart';
import '../domain/treatment_models.dart';

final availabilityProvider = FutureProvider.family<TreatmentAvailability, ({String id, String date})>((ref, args) async {
  final repository = ref.watch(treatmentsRepositoryProvider);
  return repository.checkAvailability(args.id, args.date);
});
