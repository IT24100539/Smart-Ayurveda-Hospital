import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/skeleton.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_keys.dart';
import 'feedback_messages.dart';
import 'notification_destination.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({this.embedded = false, super.key});

  final bool embedded;

  @override
  ConsumerState<NotificationsScreen> createState() =>
      _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  List<PatientNotification>? _items;

  Future<void> _markAllRead() async {
    final current = _items ?? ref.read(notificationsProvider).valueOrNull;
    if (current == null) return;
    setState(() {
      _items = [for (final notice in current) notice.copyWith(isRead: true)];
    });
    try {
      await ref
          .read(communicationRepositoryProvider)
          .markAllNotificationsRead();
      ref.invalidate(notificationsProvider);
    } catch (error) {
      if (!mounted) return;
      setState(() => _items = current);
      final l10n = AppLocalizations.of(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    }
  }

  Future<bool> _markRead(PatientNotification item) async {
    if (item.isRead) return true;
    final current = _items ?? ref.read(notificationsProvider).valueOrNull;
    if (current == null) return false;

    setState(() {
      _items = [
        for (final notice in current)
          if (notice.id == item.id) notice.copyWith(isRead: true) else notice,
      ];
    });

    try {
      await ref
          .read(communicationRepositoryProvider)
          .markNotificationRead(item.id);
      ref.invalidate(notificationsProvider);
      return true;
    } catch (error) {
      if (!mounted) return false;
      setState(() => _items = current);
      final l10n = AppLocalizations.of(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
      return false;
    }
  }

  Future<void> _open(PatientNotification item) async {
    final marked = await _markRead(item);
    if (!marked || !mounted) return;
    final destination = notificationDestination(item.kind);
    if (destination != null) context.go(destination);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final notices = ref.watch(notificationsProvider);
    final cached = _items;

    final hasUnread =
        (cached ?? notices.valueOrNull)?.any((item) => !item.isRead) ?? false;

    return Scaffold(
      appBar: widget.embedded
          ? null
          : AppBar(
              title: Text(l10n.notificationsTitle),
              actions: [
                if (hasUnread)
                  TextButton(
                    onPressed: _markAllRead,
                    child: Text(l10n.markAllRead),
                  ),
              ],
            ),
      body: cached != null
          ? _NotificationList(
              items: cached,
              onTap: (item) {
                _open(item);
              },
              onMarkAll: hasUnread ? _markAllRead : null,
            )
          : notices.when(
              loading: () => const SkeletonList(
                withBanner: false,
                itemCount: 5,
                lines: 1,
                leading: true,
                listKey: FeedbackKeys.notificationsSkeleton,
              ),
              error: (error, _) => ErrorState(
                message: feedbackErrorText(error, l10n),
                actionLabel: l10n.retry,
                actionKey: FeedbackKeys.notificationsRetry,
                onAction: () => ref.invalidate(notificationsProvider),
              ),
              data: (loaded) => _NotificationList(
                items: loaded,
                onTap: (item) {
                  _open(item);
                },
                onMarkAll: loaded.any((item) => !item.isRead)
                    ? _markAllRead
                    : null,
              ),
            ),
    );
  }
}

class _NotificationList extends StatelessWidget {
  const _NotificationList({
    required this.items,
    required this.onTap,
    this.onMarkAll,
  });

  final List<PatientNotification> items;
  final ValueChanged<PatientNotification> onTap;
  final VoidCallback? onMarkAll;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (items.isEmpty) {
      return EmptyState(
        message: l10n.notificationsEmpty,
        imageAsset: 'assets/images/empty-feedback.png',
      );
    }
    final unread = items.where((item) => !item.isRead).length;
    return PageListView.builder(
      itemCount: items.length + 1,
      spacing: 10,
      itemBuilder: (context, index) {
        if (index == 0) {
          return Row(
            children: [
              _UnreadBadge(count: unread, label: l10n.unreadLabel),
              const Spacer(),
              if (onMarkAll != null)
                TextButton(
                  key: FeedbackKeys.markAllRead,
                  onPressed: onMarkAll,
                  child: Text(l10n.markAllRead),
                ),
            ],
          );
        }
        final item = items[index - 1];
        return _NotificationTile(item: item, onTap: () => onTap(item));
      },
    );
  }
}

class _UnreadBadge extends StatelessWidget {
  const _UnreadBadge({required this.count, required this.label});

  final int count;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Badge(
        isLabelVisible: count > 0,
        label: Text('$count', key: FeedbackKeys.unreadBadge),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(4, 6, 12, 6),
          child: Text(label),
        ),
      ),
    );
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.item, required this.onTap});

  final PatientNotification item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final icon = switch (item.kind) {
      NotificationKind.reply => Icons.reply_outlined,
      NotificationKind.statusChange => Icons.update,
      NotificationKind.escalation => Icons.priority_high,
      NotificationKind.general => Icons.notifications_none_outlined,
      NotificationKind.appointmentApproved ||
      NotificationKind.appointmentRejected ||
      NotificationKind.appointmentRescheduled ||
      NotificationKind.appointmentCancelled => Icons.event_available_outlined,
      NotificationKind.prescriptionIssued => Icons.spa_outlined,
      NotificationKind.invoiceIssued => Icons.receipt_long_outlined,
    };
    final brand = AyurvedaThemeExtension.of(context);
    final escalation = item.kind == NotificationKind.escalation;
    final iconColor = escalation ? theme.colorScheme.error : brand.teal;
    final iconBackground = escalation
        ? theme.colorScheme.errorContainer
        : theme.colorScheme.primaryContainer;

    return ClinicCard(
      padding: EdgeInsets.zero,
      child: ListTile(
        key: FeedbackKeys.notificationTile(item.id),
        onTap: onTap,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        leading: Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(
            color: iconBackground,
            shape: BoxShape.circle,
          ),
          child: Icon(icon, color: iconColor, size: 20),
        ),
        title: Text(
          item.title,
          style: TextStyle(
            fontWeight: item.isRead ? FontWeight.w500 : FontWeight.w700,
          ),
        ),
        subtitle: Text(
          '${notificationKindLabel(l10n, item.kind)}\n${item.message}',
        ),
        isThreeLine: true,
        trailing: item.isRead
            ? null
            : Icon(Icons.circle, size: 10, color: brand.goldAccent),
      ),
    );
  }
}
