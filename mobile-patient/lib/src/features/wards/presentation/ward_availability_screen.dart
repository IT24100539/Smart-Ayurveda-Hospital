import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../auth/application/auth_controller.dart';
import '../../../l10n/feature_localizations.dart';
import '../data/ward_repository.dart';
import '../domain/ward.dart';

class WardAvailabilityScreen extends ConsumerStatefulWidget {
  const WardAvailabilityScreen({super.key});
  @override
  ConsumerState<WardAvailabilityScreen> createState() =>
      _WardAvailabilityScreenState();
}

class _WardAvailabilityScreenState
    extends ConsumerState<WardAvailabilityScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reason = TextEditingController();
  String? _wardId;
  DateTime? _preferredDate;
  bool _sending = false;

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final wards = ref.watch(wardsProvider);
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(copy.wardAvailability)),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(wardsProvider.future),
        child: wards.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) =>
              ErrorState(message: copy.wardLoadError(error), scrollable: true),
          data: (items) => ListView(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(20),
                child: Image.asset(
                  'assets/images/ayurveda-ward.png',
                  height: 150,
                  width: double.infinity,
                  fit: BoxFit.cover,
                ),
              ),
              const SizedBox(height: 16),
              SectionBanner(
                kicker: copy.text('In-patient care', 'ඇතුළත රෝගී සත්කාර'),
                title: copy.wardAvailability,
                body: copy.privacyNote,
              ),
              ...items.map(
                (ward) => Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _WardCard(ward: ward),
                ),
              ),
              const SizedBox(height: 14),
              Text(
                copy.requestAdmission,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 12),
              _reviewCallout(context),
              const SizedBox(height: 16),
              _form(context, items),
            ],
          ),
        ),
      ),
    );
  }

  Widget _reviewCallout(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: Theme.of(context).colorScheme.secondaryContainer,
      borderRadius: BorderRadius.circular(14),
    ),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Icon(Icons.info_outline),
        const SizedBox(width: 10),
        Expanded(
          child: Text(FeatureLocalizations.of(context).admissionReviewNote),
        ),
      ],
    ),
  );

  Widget _form(BuildContext context, List<Ward> wards) => Form(
    key: _formKey,
    child: Column(
      children: [
        DropdownButtonFormField<String>(
          initialValue: _wardId,
          decoration: InputDecoration(
            labelText: FeatureLocalizations.of(context).preferredWard,
          ),
          items: wards
              .where((ward) => ward.availableBeds > 0)
              .map(
                (ward) => DropdownMenuItem(
                  value: ward.id,
                  child: Text(
                    '${ward.name} (${FeatureLocalizations.of(context).available(ward.availableBeds)})',
                  ),
                ),
              )
              .toList(),
          onChanged: (value) => setState(() => _wardId = value),
          validator: (value) => value == null
              ? FeatureLocalizations.of(context).chooseWard
              : null,
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _reason,
          minLines: 3,
          maxLines: 5,
          decoration: InputDecoration(
            labelText: FeatureLocalizations.of(context).admissionReason,
            alignLabelWithHint: true,
          ),
          validator: (value) => value == null || value.trim().isEmpty
              ? FeatureLocalizations.of(context).reasonRequired
              : null,
        ),
        const SizedBox(height: 12),
        InkWell(
          onTap: _pickDate,
          borderRadius: BorderRadius.circular(14),
          child: InputDecorator(
            decoration: InputDecoration(
              labelText: FeatureLocalizations.of(context).preferredDate,
              errorText: _preferredDate == null && _dateValidationShown
                  ? FeatureLocalizations.of(context).chooseDateError
                  : null,
              suffixIcon: const Icon(Icons.calendar_today_outlined),
            ),
            child: Text(
              _preferredDate == null
                  ? FeatureLocalizations.of(context).selectDate
                  : FeatureLocalizations.of(
                      context,
                    ).date('EEEE, d MMMM yyyy', _preferredDate!),
            ),
          ),
        ),
        const SizedBox(height: 18),
        FilledButton(
          onPressed: _sending ? null : _submit,
          child: _sending
              ? SizedBox.square(
                  dimension: 20,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Theme.of(context).colorScheme.onPrimary,
                  ),
                )
              : Text(FeatureLocalizations.of(context).sendAdmissionRequest),
        ),
      ],
    ),
  );

  bool _dateValidationShown = false;
  Future<void> _pickDate() async {
    final today = DateUtils.dateOnly(DateTime.now());
    final picked = await showDatePicker(
      context: context,
      initialDate: _preferredDate ?? today,
      firstDate: today,
      lastDate: today.add(const Duration(days: 180)),
    );
    if (picked != null) setState(() => _preferredDate = picked);
  }

  Future<void> _submit() async {
    setState(() => _dateValidationShown = true);
    if (!_formKey.currentState!.validate() || _preferredDate == null) return;
    final patientId = ref.read(authControllerProvider).user?.id;
    if (patientId == null || patientId.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(FeatureLocalizations.of(context).profileUnavailable),
        ),
      );
      return;
    }
    setState(() => _sending = true);
    try {
      await ref
          .read(wardRepositoryProvider)
          .requestAdmission(
            patientId: patientId,
            wardId: _wardId!,
            reason: _reason.text.trim(),
            preferredDate: _preferredDate!,
          );
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          icon: const Icon(Icons.hourglass_top_rounded),
          title: Text(FeatureLocalizations.of(context).requestPendingReview),
          content: Text(
            FeatureLocalizations.of(context).admissionPendingExplanation,
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: Text(FeatureLocalizations.of(context).understood),
            ),
          ],
        ),
      );
      _formKey.currentState?.reset();
      setState(() {
        _wardId = null;
        _preferredDate = null;
        _reason.clear();
        _dateValidationShown = false;
      });
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              FeatureLocalizations.of(context).admissionError(error),
            ),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }
}

class _WardCard extends StatelessWidget {
  const _WardCard({required this.ward});
  final Ward ward;
  @override
  Widget build(BuildContext context) {
    final full = ward.availableBeds == 0;
    final copy = FeatureLocalizations.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    ward.name,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                Text(
                  full ? copy.full : copy.available(ward.availableBeds),
                  style: TextStyle(
                    fontWeight: FontWeight.w700,
                    color: full
                        ? Theme.of(context).colorScheme.error
                        : Theme.of(context).colorScheme.primary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            LinearProgressIndicator(
              value: ward.occupancy,
              minHeight: 9,
              borderRadius: BorderRadius.circular(8),
              color: full
                  ? Theme.of(context).colorScheme.error
                  : Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(height: 8),
            Text(
              copy.occupied(ward.occupiedBeds, ward.totalCapacity),
              style: TextStyle(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
