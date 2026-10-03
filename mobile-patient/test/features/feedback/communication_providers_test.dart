import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/data/communication_repository.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';

void main() {
  test('signed-out patients do not call inbox endpoints', () async {
    final spy = _SpyRepository();
    final container = ProviderContainer(
      overrides: [
        authControllerProvider.overrideWith(_SignedOutAuth.new),
        communicationRepositoryProvider.overrideWithValue(spy),
      ],
    );
    addTearDown(container.dispose);

    expect(await container.read(notificationsProvider.future), isEmpty);
    expect(await container.read(myFeedbackProvider.future), isEmpty);
    expect(await container.read(myComplaintsProvider.future), isEmpty);
    expect(spy.calls, isEmpty);
  });

  test('signed-in patients load the inbox', () async {
    final spy = _SpyRepository();
    final container = ProviderContainer(
      overrides: [
        authControllerProvider.overrideWith(_SignedInAuth.new),
        communicationRepositoryProvider.overrideWithValue(spy),
      ],
    );
    addTearDown(container.dispose);

    expect(await container.read(notificationsProvider.future), isEmpty);
    expect(await container.read(myFeedbackProvider.future), isEmpty);
    expect(await container.read(myComplaintsProvider.future), isEmpty);
    expect(spy.calls, ['notifications', 'mine', 'complaints']);
  });
}

class _SignedOutAuth extends AuthController {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);
}

class _SignedInAuth extends AuthController {
  @override
  AuthState build() {
    return const AuthState(
      status: AuthStatus.authenticated,
      user: AuthUser(
        id: 'user-1',
        fullName: 'Nimal Perera',
        email: 'nimal@example.com',
        phoneNumber: '0770000000',
        role: UserRole.patient,
      ),
    );
  }
}

class _SpyRepository extends CommunicationRepository {
  _SpyRepository() : super(Dio());

  final calls = <String>[];

  @override
  Future<List<PatientNotification>> notifications() {
    calls.add('notifications');
    return Future.value(const []);
  }

  @override
  Future<List<PatientFeedback>> myFeedback() {
    calls.add('mine');
    return Future.value(const []);
  }

  @override
  Future<List<PatientComplaint>> myComplaints() {
    calls.add('complaints');
    return Future.value(const []);
  }
}
