import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import '../domain/communication_models.dart';
import 'feedback_messages.dart';

class SubmitComplaintScreen extends ConsumerStatefulWidget {
  const SubmitComplaintScreen({super.key});

  @override
  ConsumerState<SubmitComplaintScreen> createState() =>
      _SubmitComplaintScreenState();
}

class _SubmitComplaintScreenState extends ConsumerState<SubmitComplaintScreen> {
  final _formKey = GlobalKey<FormState>();
  final _subject = TextEditingController();
  final _description = TextEditingController();

  ComplaintPriority _priority = ComplaintPriority.normal;
  bool _submitting = false;

  @override
  void dispose() {
    _subject.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l10n = AppLocalizations.of(context);
    if (!(_formKey.currentState?.validate() ?? false) || _submitting) return;

    setState(() => _submitting = true);
    try {
      await ref
          .read(communicationRepositoryProvider)
          .submitComplaint(
            subject: _subject.text.trim(),
            description: _description.text.trim(),
            priority: _priority,
          );
      ref.invalidate(myComplaintsProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.complaintSent)));
      if (context.canPop()) context.pop();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(feedbackErrorText(error, l10n))));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.submitComplaintTitle)),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          children: [
            TextFormField(
              controller: _subject,
              textCapitalization: TextCapitalization.sentences,
              maxLength: 200,
              decoration: InputDecoration(labelText: l10n.subjectLabel),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return l10n.subjectRequired;
                }
                return null;
              },
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _description,
              minLines: 5,
              maxLines: 8,
              maxLength: 2000,
              decoration: InputDecoration(
                labelText: l10n.descriptionLabel,
                alignLabelWithHint: true,
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return l10n.descriptionRequired;
                }
                return null;
              },
            ),
            const SizedBox(height: 8),
            Text(
              l10n.priorityLabel,
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            SegmentedButton<ComplaintPriority>(
              segments: [
                ButtonSegment(
                  value: ComplaintPriority.normal,
                  label: Text(l10n.priorityNormal),
                ),
                ButtonSegment(
                  value: ComplaintPriority.high,
                  label: Text(l10n.priorityHigh),
                ),
              ],
              selected: {_priority},
              onSelectionChanged: (selection) {
                setState(() => _priority = selection.first);
              },
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(
                      height: 22,
                      width: 22,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(l10n.newComplaint),
            ),
          ],
        ),
      ),
    );
  }
}
