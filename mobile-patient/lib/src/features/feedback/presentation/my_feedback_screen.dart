import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_messages.dart';
import 'star_rating.dart';

class MyFeedbackScreen extends ConsumerWidget {
  const MyFeedbackScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final feedback = ref.watch(myFeedbackProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.myFeedbackTitle)),
      body: feedback.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _Pane(
          message: feedbackErrorText(error, l10n),
          actionLabel: l10n.retry,
          onAction: () => ref.invalidate(myFeedbackProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return _Pane(message: l10n.myFeedbackEmpty);
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(myFeedbackProvider.future),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              itemCount: items.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) =>
                  _OwnFeedbackCard(item: items[index]),
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
                  child: Text(MaterialLocalizations.of(context).cancelButtonLabel),
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
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.commentRequired)),
      );
      return;
    }
    try {
      await ref.read(communicationRepositoryProvider).updateFeedback(
        id: item.id,
        rating: rating,
        comment: text,
        isAnonymous: anonymous,
      );
      ref.invalidate(myFeedbackProvider);
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.feedbackUpdated)),
      );
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(feedbackErrorText(error, l10n))),
      );
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
      await ref.read(communicationRepositoryProvider).updateFeedback(
        id: item.id,
        withdraw: true,
      );
      ref.invalidate(myFeedbackProvider);
      ref.invalidate(publicFeedProvider);
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.feedbackWithdrawn)),
      );
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(feedbackErrorText(error, l10n))),
      );
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
            const SizedBox(height: 8),
            Text(item.comment),
            const SizedBox(height: 6),
            Text(
              item.canEdit ? l10n.canStillEdit : l10n.editingClosed,
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
          ],
        ),
      ),
    );
  }
}

class _Pane extends StatelessWidget {
  const _Pane({required this.message, this.actionLabel, this.onAction});

  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(message, textAlign: TextAlign.center),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}
