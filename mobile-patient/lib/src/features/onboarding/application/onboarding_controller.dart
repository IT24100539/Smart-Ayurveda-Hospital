import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/onboarding_storage.dart';

class OnboardingState {
  const OnboardingState({
    required this.isResolved,
    required this.hasSeenOnboarding,
  });

  const OnboardingState.initial()
      : isResolved = false,
        hasSeenOnboarding = false;

  final bool isResolved;
  final bool hasSeenOnboarding;

  OnboardingState copyWith({
    bool? isResolved,
    bool? hasSeenOnboarding,
  }) {
    return OnboardingState(
      isResolved: isResolved ?? this.isResolved,
      hasSeenOnboarding: hasSeenOnboarding ?? this.hasSeenOnboarding,
    );
  }
}

class OnboardingController extends Notifier<OnboardingState> {
  @override
  OnboardingState build() {
    Future.microtask(load);
    return const OnboardingState.initial();
  }

  Future<void> load() async {
    final storage = ref.read(onboardingStorageProvider);
    final seen = await storage.hasSeenOnboarding();
    state = OnboardingState(isResolved: true, hasSeenOnboarding: seen);
  }

  Future<void> completeOnboarding() async {
    final storage = ref.read(onboardingStorageProvider);
    await storage.markOnboardingSeen();
    state = const OnboardingState(isResolved: true, hasSeenOnboarding: true);
  }

  Future<void> resetOnboarding() async {
    final storage = ref.read(onboardingStorageProvider);
    await storage.reset();
    state = const OnboardingState(isResolved: true, hasSeenOnboarding: false);
  }
}

final onboardingControllerProvider =
    NotifierProvider<OnboardingController, OnboardingState>(
  OnboardingController.new,
);
