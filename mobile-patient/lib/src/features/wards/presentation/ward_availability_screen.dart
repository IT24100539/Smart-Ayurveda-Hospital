import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../shared/widgets/safe_asset_image.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../../shared/widgets/skeleton.dart';
import '../../../theme/app_theme.dart';
import '../../auth/application/auth_controller.dart';
import '../../../l10n/feature_localizations.dart';
import '../data/ward_repository.dart';
import '../domain/ward.dart';

abstract final class WardScreenKeys {
  static const skeleton = ValueKey('wards-skeleton');
  static const retry = ValueKey('wards-retry');
  static const empty = ValueKey('wards-empty');
}

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
          loading: () => const SkeletonList(
            twoColumns: true,
            lines: 3,
            listKey: WardScreenKeys.skeleton,
          ),
          error: (error, _) => ErrorState(
            message: copy.wardLoadError(error),
            actionLabel: copy.tryAgain,
            actionKey: WardScreenKeys.retry,
            onAction: () => ref.invalidate(wardsProvider),
            scrollable: true,
          ),
          data: (items) => PageListView(
            maxWidth: wideContentWidth,
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(
                  AyurvedaThemeExtension.of(context).cardRadius,
                ),
                child: SafeAssetImage(
                  asset: 'assets/images/ayurveda-ward.png',
                  height: 150,
                  width: double.infinity,
                  fallbackIcon: Icons.hotel_outlined,
                ),
              ),
              const SizedBox(height: 16),
              SectionBanner(
                kicker: copy.text('In-patient care', 'ඇතුළත රෝගී සත්කාර'),
                title: copy.wardAvailability,
                body: copy.privacyNote,
              ),
              if (items.isEmpty)
                EmptyState(
                  key: WardScreenKeys.empty,
                  compact: true,
                  icon: Icons.hotel_outlined,
                  message: copy.text(
                    'No wards are listed yet.',
                    'වාට්ටු තවම ලැයිස්තුගත කර නැත.',
                  ),
                )
              else
                ResponsiveColumns(
                  children: [for (final ward in items) _WardCard(ward: ward)],
                ),
              const SizedBox(height: 24),
              // The request form stays one readable column on tablets and web.
              CenteredContent(
                maxWidth: 640,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      copy.requestAdmission,
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                    const SizedBox(height: 12),
                    _reviewCallout(context),
                    const SizedBox(height: 16),
                    _form(context, items),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _reviewCallout(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: brand.pillBackground,
        borderRadius: BorderRadius.circular(brand.cardRadius / 2),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.info_outline, color: brand.pillForeground),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              FeatureLocalizations.of(context).admissionReviewNote,
              style: TextStyle(color: brand.pillForeground, height: 1.4),
            ),
          ),
        ],
      ),
    );
  }

  Widget _form(BuildContext context, List<Ward> wards) => Form(
    key: _formKey,
    child: Column(
      children: [
        DropdownButtonFormField<String>(
          initialValue: _wardId,
          isExpanded: true,
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
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final accent = full ? theme.colorScheme.error : brand.teal;
    return ClinicCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(ward.name, style: theme.textTheme.titleMedium),
              ),
              const SizedBox(width: 8),
              // Status is a pill with an icon, so it never relies on color alone.
              PillChip(
                icon: full ? Icons.block : Icons.check,
                label: full ? copy.full : copy.available(ward.availableBeds),
                background: full
                    ? theme.colorScheme.errorContainer
                    : brand.approvedBackground,
                foreground: full
                    ? theme.colorScheme.error
                    : brand.approvedForeground,
              ),
            ],
          ),
          const SizedBox(height: 14),
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: LinearProgressIndicator(
              value: ward.occupancy,
              minHeight: 9,
              color: accent,
              backgroundColor: theme.colorScheme.onSurface.withValues(
                alpha: 0.1,
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            copy.occupied(ward.occupiedBeds, ward.totalCapacity),
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}
