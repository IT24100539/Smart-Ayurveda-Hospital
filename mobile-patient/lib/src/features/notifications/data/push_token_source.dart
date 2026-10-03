/// Reads the device push token that `POST /notifications/device-tokens` stores.
///
/// The token is a credential. Implementations must not log it.
abstract interface class PushTokenSource {
  /// FCM registration token, or null when push is not configured.
  Future<String?> readToken();
}

/// Used until Firebase project files are added. It never invents a token.
class DevPushTokenSource implements PushTokenSource {
  const DevPushTokenSource();

  @override
  Future<String?> readToken() async => null;
}
