import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/application/auth_controller.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';

bool _isSignedIn(Ref ref) =>
    ref.watch(authControllerProvider).isAuthenticated;

final publicFeedProvider = FutureProvider<List<PublicFeedback>>((ref) {
  return ref.watch(communicationRepositoryProvider).publicFeed();
});

final myComplaintsProvider = FutureProvider<List<PatientComplaint>>((ref) {
  if (!_isSignedIn(ref)) return [];
  return ref.watch(communicationRepositoryProvider).myComplaints();
});

final myFeedbackProvider = FutureProvider<List<PatientFeedback>>((ref) {
  if (!_isSignedIn(ref)) return [];
  return ref.watch(communicationRepositoryProvider).myFeedback();
});

final notificationsProvider = FutureProvider<List<PatientNotification>>((ref) {
  if (!_isSignedIn(ref)) return [];
  return ref.watch(communicationRepositoryProvider).notifications();
});
