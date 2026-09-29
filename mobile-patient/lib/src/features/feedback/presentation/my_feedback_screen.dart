import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_banner.dart';
import 'feedback_keys.dart';
import 'feedback_messages.dart';
import 'star_rating.dart';

String _editWindowLabel(AppLocalizations l10n, PatientFeedback item) {
  if (!item.canEdit) return l10n.editingClosed;
  final end = item.createdAt.toUtc().add(const Duration(hours: 24));
  final left = end.difference(DateTime.now().toUtc());
  if (left.isNegative) return l10n.editingClosed;
  return l10n.editTimeRemaining(left.inHours, left.inMinutes.remainder(60));
}

class MyFeedbackScreen extends ConsumerWidget {
  const MyFeedbackScreen({this.embedded = false, super.key});

  final bool embedded;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final feedback = ref.watch(myFeedbackProvider);

    return Scaffold(
      appBar: embedded ? null : AppBar(title: Text(l10n.myFeedbackTitle)),
      body: feedback.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorState(
          message: feedbackErrorText(error, l10n),
          actionLabel: l10n.retry,
          onAction: () => ref.invalidate(myFeedbackProvider),
        ),
        data: (items) {
          final copy = FeatureLocalizations.of(context);
          return RefreshIndicator(
            onRefresh: () => ref.refresh(myFeedbackProvider.future),
            child: ListView.separated(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              itemCount: items.isEmpty ? 2 : items.length + 1,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                if (index == 0) {
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      FeedbackBanner(
                    imageAsset: 'assets/images/feedback-note.png',
                    kicker: copy.text('Your notes', 'ඔබේ සටහන්'),
                    title: l10n.myFeedbackTitle,
                    body: copy.text(
                      'You can edit a note for a short time after you send it.',
                      'යැවූ පසු කෙටි වේලාවක් තුළ සටහනක් සංස්කරණය කළ හැක.',
                    ),
                    trailing: items.isEmpty
                        ? null
                        : copy.text(
                            '${items.length} sent',
                            'යවන ලදී ${items.length}',
                          ),
                  ),
                      const SizedBox(height: 12),
                      FilledButton.icon(
                        key: FeedbackKeys.writeFeedback,
                        onPressed: () => context.push(AppRoutes.submitFeedback),
                        icon: const Icon(Icons.edit_outlined),
                        label: Text(l10n.writeFeedback),
                      ),
                    ],
                  );
                }
                if (items.isEmpty) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 28),
                    child: Text(
                      l10n.myFeedbackEmpty,
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  );
                }
                return _OwnFeedbackCard(item: items[index - 1]);
              },
            ),
          );
        },
      ),
    );
  }
}

class _OwnFeedbackCard extends ConsumerWidget {
  const _OwnFeedbackCard({required this.item});

  final PatientFeedback item;

  Future<void> _edit(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final comment = TextEditingController(text: item.comment);
    var rating = item.rating;
    var anonymous = item.isAnonymous;
    final saved = await showDialog<bool>(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setLocal) {
            return AlertDialog(
              title: Text(l10n.editFeedback),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    StarRating(
                      value: rating,
                      onChanged: (value) => setLocal(() => rating = value),
                    ),
                    TextField(
                      controller: comment,
                      minLines: 3,
                      maxLines: 6,
                      maxLength: 2000,
                      decoration: InputDecoration(labelText: l10n.commentLabel),
                    ),
                    SwitchListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(l10n.anonymousLabel),
                      value: anonymous,
                      onChanged: (value) => setLocal(() => anonymous = value),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: Text(
                    MaterialLocalizations.of(context).cancelButtonLabel,
                  ),
                ),
                FilledButton(
                  onPressed: () => Navigator.pop(context, true),
                  child: Text(l10n.saveChanges),
                ),
              ],
            );
          },
        );
      },
    );
    final text = comment.text.trim();
    comment.dispose();
    if (saved != true || !context.mounted) return;
    if (rating < 1 || text.isEmpty) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.commentRequired)));
      return;
    }
    try {
      await ref
          .read(communicationRepositoryProvider)
          .updateFeedback(
            id: item.id,
            rating: rating,
            comment: text,
            isAnonymous: anonymous,
          );
      ref.invalidate(myFeedbackProvider);
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.feedbackUpdated)));
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    }
  }

  Future<void> _withdraw(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.withdrawFeedback),
        content: Text(l10n.withdrawConfirm),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(MaterialLocalizations.of(context).cancelButtonLabel),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.withdrawFeedback),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    try {
      await ref
          .read(communicationRepositoryProvider)
          .updateFeedback(id: item.id, withdraw: true);
      ref.invalidate(myFeedbackProvider);
      ref.invalidate(publicFeedProvider);
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.feedbackWithdrawn)));
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                for (var star = 1; star <= 5; star++)
                  Icon(
                    star <= item.rating ? Icons.star : Icons.star_border,
                    size: 16,
                    color: AyurvedaColors.gold,
                  ),
                const Spacer(),
                Text(
                  formatWhen(item.createdAt),
                  style: theme.textTheme.labelSmall,
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
            const SizedBox(height: 8),
            Text(
              feedbackStatusLabel(l10n, item.status),
              style: theme.textTheme.labelMedium?.copyWith(
                color: theme.colorScheme.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              _editWindowLabel(l10n, item),
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            if (item.isAnonymous)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  l10n.anonymousPatient,
                  style: theme.textTheme.labelMedium,
                ),
              ),
            if (item.canEdit)
              Row(
                children: [
                  TextButton(
                    onPressed: () => _edit(context, ref),
                    child: Text(l10n.editFeedback),
                  ),
                  TextButton(
                    onPressed: () => _withdraw(context, ref),
                    child: Text(l10n.withdrawFeedback),
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
              Text(
                l10n.noReplies,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              )
            else
              for (final reply in item.replies)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        reply.role == ReplyRole.staff
                            ? l10n.careTeam
                            : l10n.patientRole,
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
      ),
    );
  }
}
