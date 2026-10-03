import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/release_text_input.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/page_layout.dart';
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

const _sectionCount = 4;

/// Patient feedback area: community, own notes, complaints, and notifications.
class FeedbackHubScreen extends ConsumerStatefulWidget {
  const FeedbackHubScreen({this.section = 0, super.key});

  final int section;

  @override
  ConsumerState<FeedbackHubScreen> createState() => _FeedbackHubScreenState();
}

class _FeedbackHubScreenState extends ConsumerState<FeedbackHubScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs = TabController(
    length: _sectionCount,
    vsync: this,
    initialIndex: widget.section.clamp(0, _sectionCount - 1).toInt(),
  )..addListener(_releaseFieldOnTabChange);

  void _releaseFieldOnTabChange() {
    if (_tabs.indexIsChanging) releaseTextInput();
  }

  @override
  void didUpdateWidget(covariant FeedbackHubScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.section != widget.section) {
      _tabs.animateTo(widget.section.clamp(0, _sectionCount - 1).toInt());
    }
  }

  @override
  void dispose() {
    _tabs.removeListener(_releaseFieldOnTabChange);
    releaseTextInput();
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final unread = ref
        .watch(notificationsProvider)
        .maybeWhen(
          data: (items) => items.where((item) => !item.isRead).length,
          orElse: () => null,
        );

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.navFeedback),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(48),
          child: CenteredContent(
            child: Align(
              alignment: Alignment.centerLeft,
              child: UnderlineTabBar(
                controller: _tabs,
                tabs: [
                  UnderlineTabItem(
                    label: l10n.hubCommunity,
                    tabKey: FeedbackKeys.hubCommunity,
                  ),
                  UnderlineTabItem(
                    label: l10n.hubMine,
                    tabKey: FeedbackKeys.hubMine,
                  ),
                  UnderlineTabItem(
                    label: l10n.hubComplaints,
                    tabKey: FeedbackKeys.hubComplaints,
                  ),
                  UnderlineTabItem(
                    label: l10n.hubNotifications,
                    count: unread,
                    tabKey: FeedbackKeys.hubNotifications,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
      body: AnimatedBuilder(
        animation: _tabs,
        builder: (context, _) => IndexedStack(
          index: _tabs.index,
          children: const [
            PublicFeedbackFeedScreen(embedded: true),
            MyFeedbackScreen(embedded: true),
            MyComplaintsScreen(embedded: true),
            NotificationsScreen(embedded: true),
          ],
        ),
      ),
    );
  }
}
