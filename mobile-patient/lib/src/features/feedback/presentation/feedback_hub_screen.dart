import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../application/communication_providers.dart';
import 'feedback_keys.dart';
import 'my_complaints_screen.dart';
import 'my_feedback_screen.dart';
import 'notifications_screen.dart';
import 'public_feedback_feed_screen.dart';

int feedbackSectionIndex(String? section) => switch (section) {
  'mine' => 1,
  'complaints' => 2,
  'notifications' => 3,
  _ => 0,
};

/// Patient feedback area: community, own notes, complaints, and notifications.
class FeedbackHubScreen extends ConsumerStatefulWidget {
  const FeedbackHubScreen({this.section = 0, super.key});

  final int section;

  @override
  ConsumerState<FeedbackHubScreen> createState() => _FeedbackHubScreenState();
}

class _FeedbackHubScreenState extends ConsumerState<FeedbackHubScreen> {
  late int _section = widget.section;

  @override
  void didUpdateWidget(covariant FeedbackHubScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.section != widget.section) {
      _section = widget.section;
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final unread = ref.watch(notificationsProvider).maybeWhen(
      data: (items) => items.where((item) => !item.isRead).length,
      orElse: () => 0,
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.navFeedback),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(56),
          child: Align(
            alignment: Alignment.centerLeft,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: SegmentedButton<int>(
                showSelectedIcon: false,
                segments: [
                  ButtonSegment(
                    value: 0,
                    label: Text(l10n.hubCommunity, key: FeedbackKeys.hubCommunity),
                  ),
                  ButtonSegment(
                    value: 1,
                    label: Text(l10n.hubMine, key: FeedbackKeys.hubMine),
                  ),
                  ButtonSegment(
                    value: 2,
                    label: Text(
                      l10n.hubComplaints,
                      key: FeedbackKeys.hubComplaints,
                    ),
                  ),
                  ButtonSegment(
                    value: 3,
                    label: Badge(
                      isLabelVisible: unread > 0,
                      label: Text('$unread'),
                      child: Text(
                        l10n.hubNotifications,
                        key: FeedbackKeys.hubNotifications,
                      ),
                    ),
                  ),
                ],
                selected: {_section},
                onSelectionChanged: (selection) {
                  setState(() => _section = selection.first);
                },
              ),
            ),
          ),
        ),
      ),
      body: IndexedStack(
        index: _section,
        children: const [
          PublicFeedbackFeedScreen(embedded: true),
          MyFeedbackScreen(embedded: true),
          MyComplaintsScreen(embedded: true),
          NotificationsScreen(embedded: true),
        ],
      ),
    );
  }
}
