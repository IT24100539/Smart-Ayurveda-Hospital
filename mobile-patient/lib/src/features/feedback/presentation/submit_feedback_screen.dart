import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../appointments/domain/appointment_models.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../../auth/application/auth_controller.dart';
import '../../treatments/application/treatments_provider.dart';
import '../../treatments/domain/treatment_models.dart';
import '../application/communication_providers.dart';
import '../data/communication_repository.dart';
import 'feedback_banner.dart';
import 'feedback_keys.dart';
import 'feedback_messages.dart';
import 'star_rating.dart';

/// Opens [SubmitFeedbackScreen] with the visit pre-filled when the ids are known.
String submitFeedbackLocation({String? appointmentId, String? treatmentId}) {
  final query = <String, String>{
    if (appointmentId != null && appointmentId.isNotEmpty)
      'appointmentId': appointmentId,
    if (treatmentId != null && treatmentId.isNotEmpty)
      'treatmentId': treatmentId,
  };
  return Uri(
    path: AppRoutes.submitFeedback,
    queryParameters: query.isEmpty ? null : query,
  ).toString();
}

/// Button a completed appointment uses to open the feedback form.
class LeaveFeedbackButton extends StatelessWidget {
  const LeaveFeedbackButton({
    this.appointmentId,
    this.treatmentId,
    this.expanded = true,
    super.key,
  });

  final String? appointmentId;
  final String? treatmentId;

  /// Full-width action. Completed-visit rows pass `false` for a compact button.
  final bool expanded;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    void open() {
      context.push(
        submitFeedbackLocation(
          appointmentId: appointmentId,
          treatmentId: treatmentId,
        ),
      );
    }

    if (!expanded) {
      return TextButton(onPressed: open, child: Text(l10n.leaveFeedback));
    }

    return FilledButton.icon(
      key: FeedbackKeys.leaveFeedback,
      onPressed: open,
      icon: const Icon(Icons.rate_review_outlined),
      label: Text(l10n.leaveFeedback),
    );
  }
}

class SubmitFeedbackScreen extends ConsumerStatefulWidget {
  const SubmitFeedbackScreen({this.appointmentId, this.treatmentId, super.key});

  final String? appointmentId;
  final String? treatmentId;

  @override
  ConsumerState<SubmitFeedbackScreen> createState() =>
      _SubmitFeedbackScreenState();
}

enum _FeedbackLink { visit, treatment }

class _SubmitFeedbackScreenState extends ConsumerState<SubmitFeedbackScreen> {
  final _formKey = GlobalKey<FormState>();
  final _comment = TextEditingController();

  int _rating = 0;
  bool _anonymous = false;
  bool _attempted = false;
  bool _submitting = false;
  late _FeedbackLink _link;
  String? _selectedAppointmentId;
  String? _selectedTreatmentId;

  @override
  void initState() {
    super.initState();
    final hasVisit =
        widget.appointmentId != null && widget.appointmentId!.isNotEmpty;
    final hasTreatment =
        widget.treatmentId != null && widget.treatmentId!.isNotEmpty;
    _link = hasVisit || !hasTreatment
        ? _FeedbackLink.visit
        : _FeedbackLink.treatment;
    _selectedAppointmentId = widget.appointmentId;
    _selectedTreatmentId = widget.treatmentId;
  }

  bool get _hasLink => _link == _FeedbackLink.visit
      ? _selectedAppointmentId != null && _selectedAppointmentId!.isNotEmpty
      : _selectedTreatmentId != null && _selectedTreatmentId!.isNotEmpty;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l10n = AppLocalizations.of(context);
    setState(() => _attempted = true);
    final formOk = _formKey.currentState?.validate() ?? false;
    if (!formOk || _rating < 1 || !_hasLink || _submitting) return;

    setState(() => _submitting = true);
    try {
      await ref
          .read(communicationRepositoryProvider)
          .submitFeedback(
            rating: _rating,
            comment: _comment.text.trim(),
            isAnonymous: _anonymous,
            appointmentId: _link == _FeedbackLink.visit
                ? _selectedAppointmentId
                : null,
            treatmentId: _link == _FeedbackLink.treatment
                ? _selectedTreatmentId
                : null,
          );
      ref.invalidate(myFeedbackProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.feedbackSent)));
      context.go('${AppRoutes.feedback}?section=mine');
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
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final fullName = ref.watch(authControllerProvider).user?.fullName.trim();
    final previewName = (fullName == null || fullName.isEmpty)
        ? l10n.yourName
        : fullName;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.submitFeedbackTitle)),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
            FeedbackBanner(
              imageAsset: 'assets/images/feedback-note.png',
              kicker: copy.text('After the visit', 'පැමිණීමෙන් පසු'),
              title: l10n.submitFeedbackTitle,
              body: l10n.commentHint,
            ),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _FeedbackLinkSelector(
                link: _link,
                appointmentId: _selectedAppointmentId,
                treatmentId: _selectedTreatmentId,
                onLink: (value) => setState(() => _link = value),
                onAppointment: (value) =>
                    setState(() => _selectedAppointmentId = value),
                onTreatment: (value) =>
                    setState(() => _selectedTreatmentId = value),
              ),
            ),
            Text(
              l10n.ratingLabel,
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            StarRating(
              value: _rating,
              onChanged: (value) => setState(() => _rating = value),
            ),
            if (_attempted && _rating < 1)
              Text(
                l10n.ratingRequired,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.error,
                ),
              ),
            const SizedBox(height: 12),
            TextFormField(
              key: FeedbackKeys.comment,
              controller: _comment,
              minLines: 4,
              maxLines: 8,
              maxLength: 2000,
              decoration: InputDecoration(
                labelText: l10n.commentLabel,
                hintText: l10n.commentHint,
                alignLabelWithHint: true,
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return l10n.commentRequired;
                }
                return null;
              },
            ),
            const SizedBox(height: 8),
            SwitchListTile(
              key: FeedbackKeys.anonymousToggle,
              contentPadding: EdgeInsets.zero,
              title: Text(l10n.anonymousLabel),
              subtitle: Text(l10n.anonymousHelp),
              value: _anonymous,
              onChanged: (value) => setState(() => _anonymous = value),
            ),
            if (!_anonymous)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: InputDecorator(
                  decoration: InputDecoration(labelText: l10n.postedAsLabel),
                  child: Text(previewName, key: FeedbackKeys.namePreview),
                ),
              ),
            if (_attempted && !_hasLink)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(
                  l10n.feedbackNeedsLink,
                  key: const Key('feedback_link_required'),
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.error,
                  ),
                ),
              ),
            FilledButton(
              key: FeedbackKeys.submit,
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(
                      height: 22,
                      width: 22,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(l10n.submitFeedback),
            ),
            ],
          ),
        ),
      ),
    );
  }
}

class _FeedbackLinkSelector extends ConsumerWidget {
  const _FeedbackLinkSelector({
    required this.link,
    required this.appointmentId,
    required this.treatmentId,
    required this.onLink,
    required this.onAppointment,
    required this.onTreatment,
  });

  final _FeedbackLink link;
  final String? appointmentId;
  final String? treatmentId;
  final ValueChanged<_FeedbackLink> onLink;
  final ValueChanged<String?> onAppointment;
  final ValueChanged<String?> onTreatment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SegmentedButton<_FeedbackLink>(
          showSelectedIcon: false,
          segments: [
            ButtonSegment(
              value: _FeedbackLink.visit,
              label: Text(l10n.linkToVisit, key: FeedbackKeys.linkVisit),
            ),
            ButtonSegment(
              value: _FeedbackLink.treatment,
              label: Text(l10n.linkToTreatment, key: FeedbackKeys.linkTreatment),
            ),
          ],
          selected: {link},
          onSelectionChanged: (selection) => onLink(selection.first),
        ),
        const SizedBox(height: 12),
        if (link == _FeedbackLink.visit)
          _VisitMenu(
            selectedId: appointmentId,
            onSelected: onAppointment,
            onSwitchToTreatment: () => onLink(_FeedbackLink.treatment),
          )
        else
          _TreatmentMenu(
            selectedId: treatmentId,
            onSelected: onTreatment,
          ),
      ],
    );
  }
}

class _VisitMenu extends ConsumerWidget {
  const _VisitMenu({
    required this.selectedId,
    required this.onSelected,
    this.onSwitchToTreatment,
  });

  final String? selectedId;
  final ValueChanged<String?> onSelected;
  final VoidCallback? onSwitchToTreatment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final visits = ref.watch(myAppointmentsProvider);
    return visits.when(
      loading: () => const LinearProgressIndicator(),
      error: (error, _) => Text(
        feedbackErrorText(error, l10n),
        style: theme.textTheme.bodySmall?.copyWith(
          color: theme.colorScheme.error,
        ),
      ),
      data: (items) {
        final completed = items
            .where((visit) => visit.status == AppointmentStatus.completed)
            .toList();
        if (completed.isEmpty) {
          return InkWell(
            onTap: onSwitchToTreatment,
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Text(
                l10n.noCompletedVisit,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.primary,
                ),
              ),
            ),
          );
        }
        if (selectedId == null && completed.isNotEmpty) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            onSelected(completed.first.id);
          });
        }
        final selected = completed.any((visit) => visit.id == selectedId)
            ? selectedId
            : completed.first.id;
        return DropdownButtonFormField<String>(
          key: ValueKey('visit-$selected'),
          initialValue: selected,
          decoration: InputDecoration(labelText: l10n.chooseCompletedVisit),
          items: [
            for (final visit in completed)
              DropdownMenuItem(
                value: visit.id,
                child: Text(visit.treatmentName),
              ),
          ],
          onChanged: onSelected,
        );
      },
    );
  }
}

class _TreatmentMenu extends ConsumerWidget {
  const _TreatmentMenu({required this.selectedId, required this.onSelected});

  final String? selectedId;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final treatments = ref.watch(treatmentsProvider);
    return treatments.when(
      loading: () => const LinearProgressIndicator(),
      error: (error, _) => Text(
        feedbackErrorText(error, l10n),
        style: theme.textTheme.bodySmall?.copyWith(
          color: theme.colorScheme.error,
        ),
      ),
      data: (items) => _treatmentMenu(l10n, items),
    );
  }

  Widget _treatmentMenu(AppLocalizations l10n, List<Treatment> items) {
    if (selectedId == null && items.isNotEmpty) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        onSelected(items.first.id);
      });
    }
    final selected = items.any((item) => item.id == selectedId)
        ? selectedId
        : (items.isNotEmpty ? items.first.id : null);
    return DropdownButtonFormField<String>(
      key: ValueKey('treatment-$selected'),
      initialValue: selected,
      decoration: InputDecoration(labelText: l10n.chooseTreatment),
      items: [
        for (final treatment in items)
          DropdownMenuItem(
            value: treatment.id,
            child: Text(treatment.nameEnglish),
          ),
      ],
      onChanged: onSelected,
    );
  }
}
