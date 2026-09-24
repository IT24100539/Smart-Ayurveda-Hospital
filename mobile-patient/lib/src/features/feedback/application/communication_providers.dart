import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/communication_repository.dart';
import '../domain/communication_models.dart';

final publicFeedProvider = FutureProvider<List<PublicFeedback>>((ref) {
  return ref.watch(communicationRepositoryProvider).publicFeed();
});

final myComplaintsProvider = FutureProvider<List<PatientComplaint>>((ref) {
  return ref.watch(communicationRepositoryProvider).myComplaints();
});

final myFeedbackProvider = FutureProvider<List<PatientFeedback>>((ref) {
  return ref.watch(communicationRepositoryProvider).myFeedback();
});

final notificationsProvider = FutureProvider<List<PatientNotification>>((ref) {
  return ref.watch(communicationRepositoryProvider).notifications();
});
