import 'package:flutter/painting.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'features/doctors/application/doctors_provider.dart';
import 'features/feedback/application/communication_providers.dart';
import 'features/treatments/application/treatments_provider.dart';

/// Drops in-memory patient data after the session ends.
///
/// The auth controller clears the stored token. This removes cached lists and
/// the image cache so the next sign-in does not show the previous patient's
/// records. Theme and language stay on the device.
class SessionCache {
  SessionCache(this._ref);

  final Ref _ref;

  void clear() {
    _ref.invalidate(notificationsProvider);
    _ref.invalidate(myFeedbackProvider);
    _ref.invalidate(myComplaintsProvider);
    _ref.invalidate(publicFeedProvider);
    _ref.invalidate(treatmentsProvider);
    _ref.invalidate(treatmentsSearchQueryProvider);
    _ref.invalidate(doctorsSearchQueryProvider);
    final images = PaintingBinding.instance.imageCache;
    images.clear();
    images.clearLiveImages();
  }
}

final sessionCacheProvider = Provider<SessionCache>((ref) => SessionCache(ref));
