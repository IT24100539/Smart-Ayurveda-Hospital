import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_banner.dart';
import 'feedback_messages.dart';

class _ReactionView {
  const _ReactionView({
    required this.reaction,
    required this.likeCount,
    required this.dislikeCount,
  });

  final ReactionType? reaction;
  final int likeCount;
  final int dislikeCount;
}

class PublicFeedbackFeedScreen extends ConsumerStatefulWidget {
  const PublicFeedbackFeedScreen({super.key});

  @override
  ConsumerState<PublicFeedbackFeedScreen> createState() =>
      _PublicFeedbackFeedScreenState();
}

class _PublicFeedbackFeedScreenState
    extends ConsumerState<PublicFeedbackFeedScreen> {
  final Map<String, _ReactionView> _reactions = {};
  String? _pendingId;

  Future<void> _react(PublicFeedback item, ReactionType next) async {
    if (_pendingId != null) return;
    final previous = _reactions[item.id];
    final current = previous?.reaction;
    var likes = previous?.likeCount ?? item.likeCount;
    var dislikes = previous?.dislikeCount ?? item.dislikeCount;

    // One reaction per patient. Tapping the same choice again removes it.
    final removing = current == next;
    if (current == ReactionType.like) likes = likes > 0 ? likes - 1 : 0;
    if (current == ReactionType.dislike) {
      dislikes = dislikes > 0 ? dislikes - 1 : 0;
    }
    if (!removing && next == ReactionType.like) likes += 1;
    if (!removing && next == ReactionType.dislike) dislikes += 1;

    setState(() {
      _pendingId = item.id;
      _reactions[item.id] = _ReactionView(
        reaction: removing ? null : next,
        likeCount: likes,
        dislikeCount: dislikes,
      );
    });

    try {
      final repository = ref.read(communicationRepositoryProvider);
      if (removing) {
        await repository.removeReaction(item.id);
      } else {
        await repository.react(item.id, next);
      }
    } catch (error) {
      if (!mounted) return;
      setState(() {
        if (previous == null) {
          _reactions.remove(item.id);
        } else {
          _reactions[item.id] = previous;
        }
      });
      final l10n = AppLocalizations.of(context);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    } finally {
      if (mounted) setState(() => _pendingId = null);
    }
  }

  Future<void> _reply(PublicFeedback item, String text) async {
    final l10n = AppLocalizations.of(context);
    try {
      await ref
          .read(communicationRepositoryProvider)
          .replyToFeedback(item.id, text);
      ref.invalidate(publicFeedProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.replySent)));
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
      rethrow;
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final feed = ref.watch(publicFeedProvider);
    final notices = ref.watch(notificationsProvider);
    final unread = notices.maybeWhen(
      data: (items) => items.where((item) => !item.isRead).length,
      orElse: () => 0,
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.publicFeedTitle),
        actions: [
          IconButton(
            tooltip: l10n.myFeedbackTitle,
            onPressed: () => context.push(AppRoutes.myFeedback),
            icon: const Icon(Icons.rate_review_outlined),
          ),
          IconButton(
            tooltip: l10n.complaintsTitle,
            onPressed: () => context.push(AppRoutes.complaints),
            icon: const Icon(Icons.report_outlined),
          ),
          IconButton(
            tooltip: l10n.notificationsTitle,
            onPressed: () => context.push(AppRoutes.notifications),
            icon: Badge(
              isLabelVisible: unread > 0,
              label: Text('$unread'),
              child: const Icon(Icons.notifications_outlined),
            ),
          ),
        ],
      ),
      body: feed.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorState(
          message: feedbackErrorText(error, l10n),
          actionLabel: l10n.retry,
          onAction: () => ref.invalidate(publicFeedProvider),
        ),
        data: (items) {
          final copy = FeatureLocalizations.of(context);
          final average = items.isEmpty
              ? 0.0
              : items.map((item) => item.rating).reduce((a, b) => a + b) /
                    items.length;
          return RefreshIndicator(
            onRefresh: () => ref.refresh(publicFeedProvider.future),
            child: ListView.separated(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              itemCount: items.isEmpty ? 2 : items.length + 1,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                if (index == 0) {
                  return FeedbackBanner(
                    imageAsset: 'assets/images/feedback-note.png',
                    kicker: copy.text('After the visit', 'පැමිණීමෙන් පසු'),
                    title: l10n.publicFeedTitle,
                    body: copy.text(
                      'Notes from other patients appear here after the care team reviews them.',
                      'රෝගීන්ගේ සටහන් සත්කාර කණ්ඩායම සමාලෝචනය කළ පසු මෙහි පෙනේ.',
                    ),
                    trailing: items.isEmpty
                        ? null
                        : copy.text(
                            '${items.length} notes · ${average.toStringAsFixed(1)} / 5',
                            'සටහන් ${items.length} · ${average.toStringAsFixed(1)} / 5',
                          ),
                  );
                }
                if (items.isEmpty) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 28),
                    child: Text(
                      l10n.publicFeedEmpty,
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  );
                }
                final item = items[index - 1];
                final reaction = _reactions[item.id];
                return _FeedbackCard(
                  item: item,
                  likeCount: reaction?.likeCount ?? item.likeCount,
                  dislikeCount: reaction?.dislikeCount ?? item.dislikeCount,
                  selected: reaction?.reaction,
                  onReact: (type) => _react(item, type),
                  onReply: (text) => _reply(item, text),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class _FeedbackCard extends StatelessWidget {
  const _FeedbackCard({
    required this.item,
    required this.likeCount,
    required this.dislikeCount,
    required this.selected,
    required this.onReact,
    required this.onReply,
  });

  final PublicFeedback item;
  final int likeCount;
  final int dislikeCount;
  final ReactionType? selected;
  final ValueChanged<ReactionType> onReact;
  final Future<void> Function(String reply) onReply;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final name = item.isAnonymous ? l10n.anonymousPatient : item.patientName;

    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  item.isAnonymous
                      ? Icons.visibility_off_outlined
                      : Icons.person_outline,
                  size: 18,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    name,
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                Text(
                  formatWhen(item.createdAt),
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                for (var star = 1; star <= 5; star++)
                  Icon(
                    star <= item.rating ? Icons.star : Icons.star_border,
                    size: 16,
                    color: AyurvedaColors.gold,
                  ),
              ],
            ),
            const SizedBox(height: 10),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
              decoration: const BoxDecoration(
                color: AyurvedaColors.cream,
                borderRadius: BorderRadius.all(Radius.circular(12)),
                border: Border(
                  left: BorderSide(color: AyurvedaColors.gold, width: 3),
                ),
              ),
              child: Text(
                item.comment,
                style: theme.textTheme.bodyLarge?.copyWith(height: 1.45),
              ),
            ),
            const SizedBox(height: 4),
            Row(
              children: [
                _ReactionButton(
                  icon: selected == ReactionType.like
                      ? Icons.thumb_up
                      : Icons.thumb_up_outlined,
                  label: l10n.helpful,
                  count: likeCount,
                  selected: selected == ReactionType.like,
                  onPressed: () => onReact(ReactionType.like),
                ),
                _ReactionButton(
                  icon: selected == ReactionType.dislike
                      ? Icons.thumb_down
                      : Icons.thumb_down_outlined,
                  label: l10n.notHelpful,
                  count: dislikeCount,
                  selected: selected == ReactionType.dislike,
                  onPressed: () => onReact(ReactionType.dislike),
                ),
              ],
            ),
            const Divider(),
            Text(
              l10n.repliesHeading,
              style: theme.textTheme.labelLarge?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            if (item.replies.isEmpty)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Text(
                  l10n.noReplies,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              )
            else
              for (final reply in item.replies) _ReplyTile(reply: reply),
            _ReplyComposer(onReply: onReply),
          ],
        ),
      ),
    );
  }
}

class _ReplyComposer extends StatefulWidget {
  const _ReplyComposer({required this.onReply});

  final Future<void> Function(String reply) onReply;

  @override
  State<_ReplyComposer> createState() => _ReplyComposerState();
}

class _ReplyComposerState extends State<_ReplyComposer> {
  final _controller = TextEditingController();
  bool _sending = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final text = _controller.text.trim();
    if (text.isEmpty || _sending) return;
    setState(() => _sending = true);
    try {
      await widget.onReply(text);
      if (mounted) _controller.clear();
    } catch (_) {
      // The feed shows the error.
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(
          child: TextField(
            controller: _controller,
            minLines: 1,
            maxLines: 3,
            maxLength: 2000,
            decoration: InputDecoration(
              hintText: l10n.replyHint,
              counterText: '',
            ),
          ),
        ),
        IconButton(
          tooltip: l10n.sendReply,
          onPressed: _sending ? null : _send,
          icon: _sending
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.send_outlined),
        ),
      ],
    );
  }
}

class _ReactionButton extends StatelessWidget {
  const _ReactionButton({
    required this.icon,
    required this.label,
    required this.count,
    required this.selected,
    required this.onPressed,
  });

  final IconData icon;
  final String label;
  final int count;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final color = selected
        ? Theme.of(context).colorScheme.primary
        : Theme.of(context).colorScheme.onSurfaceVariant;
    return TextButton.icon(
      onPressed: onPressed,
      icon: Icon(icon, size: 18, color: color),
      label: Text('$label $count', style: TextStyle(color: color)),
    );
  }
}

class _ReplyTile extends StatelessWidget {
  const _ReplyTile({required this.reply});

  final PublicReply reply;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final fromStaff = reply.role == ReplyRole.staff;

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AyurvedaColors.cream,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            fromStaff ? Icons.spa_outlined : Icons.chat_bubble_outline,
            size: 18,
            color: theme.colorScheme.primary,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  fromStaff ? l10n.careTeam : l10n.patientRole,
                  style: theme.textTheme.labelMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(reply.reply),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
