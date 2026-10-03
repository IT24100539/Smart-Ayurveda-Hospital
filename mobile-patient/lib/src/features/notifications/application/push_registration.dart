import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/application/auth_controller.dart';
import '../../feedback/data/communication_repository.dart';
import '../data/push_token_source.dart';

/// Posts the current device token after the patient signs in.
///
/// A missing token or a desktop platform skips the request. Failures stay
/// inside the inbox flow and are not written with the token.
class PushRegistrationService {
  const PushRegistrationService({
    required this.source,
    required this.platform,
    required this.register,
  });

  final PushTokenSource source;
  final String? Function() platform;
  final Future<void> Function({
    required String token,
    required String platform,
  }) register;

  /// True when a token was sent to the API.
  Future<bool> registerIfAvailable() async {
    final token = (await source.readToken())?.trim();
    final platformName = platform();
    if (token == null || token.isEmpty || platformName == null) return false;
    await register(token: token, platform: platformName);
    return true;
  }
}

/// `android`, `ios`, or `web`. Desktop builds are not accepted by the API.
String? currentDevicePlatform() {
  if (kIsWeb) return 'web';
  return switch (defaultTargetPlatform) {
    TargetPlatform.android => 'android',
    TargetPlatform.iOS => 'ios',
    _ => null,
  };
}

final pushTokenSourceProvider = Provider<PushTokenSource>((ref) {
  return const DevPushTokenSource();
});

final pushRegistrationProvider = Provider<PushRegistrationService>((ref) {
  return PushRegistrationService(
    source: ref.watch(pushTokenSourceProvider),
    platform: currentDevicePlatform,
    register: ({required String token, required String platform}) {
      return ref.read(communicationRepositoryProvider).registerDeviceToken(
        token: token,
        platform: platform,
      );
    },
  );
});

/// Registers the device token when a session becomes authenticated.
class PushRegistrationHost extends ConsumerWidget {
  const PushRegistrationHost({required this.child, super.key});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen(authControllerProvider, (previous, next) {
      if (!next.isAuthenticated || previous?.isAuthenticated == true) return;
      final registration = ref.read(pushRegistrationProvider);
      registration.registerIfAvailable().then(
        (_) {},
        onError: (Object error, StackTrace stackTrace) {
          debugPrint('Push token registration failed.');
        },
      );
    });
    return child;
  }
}
