import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../application/communication_providers.dart';

/// Unread count drawn from the patient inbox. Hidden when nothing is unread.
class UnreadCountBadge extends ConsumerWidget {
  const UnreadCountBadge({required this.child, this.badgeKey, super.key});

  final Widget child;
  final Key? badgeKey;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unread = ref.watch(notificationsProvider).maybeWhen(
      data: (items) => items.where((item) => !item.isRead).length,
      orElse: () => 0,
    );
    return Badge(
      isLabelVisible: unread > 0,
      label: Text('$unread', key: badgeKey),
      child: child,
    );
  }
}
