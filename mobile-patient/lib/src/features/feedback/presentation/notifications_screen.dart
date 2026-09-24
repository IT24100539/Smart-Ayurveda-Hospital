import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_keys.dart';
import 'feedback_messages.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

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

  Future<void> _markRead(PatientNotification item) async {
    if (item.isRead) return;
    final current = _items ?? ref.read(notificationsProvider).valueOrNull;
    if (current == null) return;

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
    } catch (error) {
      if (!mounted) return;
      setState(() => _items = current);
      final l10n = AppLocalizations.of(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final notices = ref.watch(notificationsProvider);
    final cached = _items;

    final hasUnread =
        (cached ?? notices.valueOrNull)?.any((item) => !item.isRead) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.notificationsTitle),
        actions: [
          if (hasUnread)
            TextButton(onPressed: _markAllRead, child: Text(l10n.markAllRead)),
        ],
      ),
      body: cached != null
          ? _NotificationList(items: cached, onTap: _markRead)
          : notices.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        feedbackErrorText(error, l10n),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 16),
                      OutlinedButton(
                        onPressed: () => ref.invalidate(notificationsProvider),
                        child: Text(l10n.retry),
                      ),
                    ],
                  ),
                ),
              ),
              data: (loaded) =>
                  _NotificationList(items: loaded, onTap: _markRead),
            ),
    );
  }
}

class _NotificationList extends StatelessWidget {
  const _NotificationList({required this.items, required this.onTap});

  final List<PatientNotification> items;
  final ValueChanged<PatientNotification> onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (items.isEmpty) {
      return Center(child: Text(l10n.notificationsEmpty));
    }
    final unread = items.where((item) => !item.isRead).length;
    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
      itemCount: items.length + 1,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (context, index) {
        if (index == 0) {
          return _UnreadBadge(count: unread, label: l10n.unreadLabel);
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
    };
    final iconColor = item.kind == NotificationKind.escalation
        ? AyurvedaColors.danger
        : theme.colorScheme.primary;

    return Card(
      child: ListTile(
        onTap: onTap,
        leading: Icon(icon, color: iconColor),
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
            : const Icon(Icons.circle, size: 10, color: AyurvedaColors.gold),
      ),
    );
  }
}
