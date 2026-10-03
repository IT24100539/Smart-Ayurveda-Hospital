import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_exception.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../theme/app_theme.dart';
import '../../auth/application/auth_controller.dart';
import '../../treatments/application/treatments_provider.dart';
import '../../treatments/domain/treatment_models.dart' show Treatment;
import '../data/appointment_repository.dart';
import '../domain/appointment_models.dart';
import 'appointments_screen.dart';
import 'therapy_card.dart';

abstract final class BookAppointmentKeys {
  static Key date(DateTime date) =>
      ValueKey('booking-date-${DateFormat('yyyy-MM-dd').format(date)}');
  static const therapyNext = ValueKey('booking-therapy-next');
  static const backToTherapy = ValueKey('booking-back-to-therapy');
  static const next = ValueKey('booking-next');
  static const submit = ValueKey('booking-submit');
  static const pendingConfirmation = ValueKey('booking-pending-confirmation');
  static const error = ValueKey('booking-error');
}

/// Wide enough for two therapy cards side by side inside the booking panel.
const _therapyColumnsBreakpoint = 640.0;

/// Steps of the booking stepper.
const _therapyStep = 0;
const _dateStep = 1;
const _timeStep = 2;
const _confirmStep = 3;

/// ProblemDetails text for the booking screen. Validation field errors win
/// over the generic title, and the exception type name is never shown.
String describeBookingError(Object error) {
  if (error is ApiException) {
    return error.message ?? 'The hospital could not accept this request.';
  }
  return error.toString();
}

/// Four-step booking: therapy, date, time, confirm.
///
/// Pass [treatment] to start at the date step with that therapy fixed, as the
/// treatment detail screen and reschedule mode do. Leave it out to pick a
/// therapy from the catalogue first.
class BookAppointmentFlow extends ConsumerStatefulWidget {
  const BookAppointmentFlow({
    this.treatment,
    this.candidateDates,
    this.appointmentIdToReschedule,
    super.key,
  });

  final TreatmentBooking? treatment;
  final List<DateTime>? candidateDates;
  final String? appointmentIdToReschedule;

  @override
  ConsumerState<BookAppointmentFlow> createState() =>
      _BookAppointmentFlowState();
}

class _BookAppointmentFlowState extends ConsumerState<BookAppointmentFlow> {
  late final List<DateTime> _dates;
  TreatmentBooking? _treatment;
  Map<DateTime, Future<TreatmentAvailability>> _availability = const {};
  String? _pickedTherapyId;
  late int _step;
  DateTime? _date;
  TreatmentAvailability? _selectedAvailability;
  TreatmentSlot? _slot;
  bool _submitting = false;
  bool _pending = false;
  String? _error;

  bool get _isRescheduling => widget.appointmentIdToReschedule != null;

  /// The therapy can only change when the flow started without one.
  bool get _canChangeTherapy => widget.treatment == null;

  @override
  void initState() {
    super.initState();
    final today = DateUtils.dateOnly(DateTime.now());
    _dates =
        widget.candidateDates ??
        List.generate(14, (index) => today.add(Duration(days: index)));
    final preset = widget.treatment;
    if (preset != null) {
      _useTherapy(preset);
      _step = _dateStep;
    } else {
      _step = _therapyStep;
    }
  }

  void _useTherapy(TreatmentBooking treatment) {
    final repository = ref.read(appointmentRepositoryProvider);
    _treatment = treatment;
    _availability = {
      for (final date in _dates)
        DateUtils.dateOnly(date): repository.availability(treatment.id, date),
    };
    _date = null;
    _selectedAvailability = null;
    _slot = null;
  }

  @override
  Widget build(BuildContext context) {
    if (_pending) return _buildPending(context);
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(
        centerTitle: true,
        title: Text(
          _isRescheduling
              ? copy.text(
                  'Reschedule appointment',
                  'හමුවීම වෙනත් දිනකට මාරු කරන්න',
                )
              : copy.bookAppointment,
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 960),
            child: Column(
              children: [
                NumberedStepper(
                  current: _step,
                  labels: [
                    copy.therapyStep,
                    copy.dateLabel,
                    copy.timeLabel,
                    copy.confirmStep,
                  ],
                ),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
                    child: RoundedPanel(
                      child: SingleChildScrollView(
                        padding: const EdgeInsets.all(20),
                        child: switch (_step) {
                          _therapyStep => _buildTherapyStep(context),
                          _dateStep => _buildDateStep(context),
                          _timeStep => _buildSlotStep(context),
                          _ => _buildConfirmStep(context),
                        },
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTherapyStep(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final therapies = ref.watch(treatmentsProvider);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          copy.chooseTherapy,
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 18),
        therapies.when(
          loading: () => const Padding(
            padding: EdgeInsets.symmetric(vertical: 32),
            child: Center(child: CircularProgressIndicator()),
          ),
          error: (error, _) => ErrorState(
            message: copy.loadTherapiesError,
            actionLabel: copy.tryAgain,
            onAction: () => ref.invalidate(treatmentsProvider),
          ),
          data: (items) {
            final bookable = items.where((item) => item.isActive).toList();
            if (bookable.isEmpty) {
              return EmptyState(message: copy.noTherapies);
            }
            return ResponsiveColumns(
              breakpoint: _therapyColumnsBreakpoint,
              children: [
                for (final therapy in bookable)
                  TherapyCard(
                    treatment: therapy,
                    selected: _pickedTherapyId == therapy.id,
                    onTap: () => setState(() => _pickedTherapyId = therapy.id),
                  ),
              ],
            );
          },
        ),
        const SizedBox(height: 24),
        FilledButton(
          key: BookAppointmentKeys.therapyNext,
          onPressed: _pickedTherapyId == null
              ? null
              : () => _continueWithPickedTherapy(therapies.valueOrNull),
          child: Text(copy.chooseDate),
        ),
      ],
    );
  }

  void _continueWithPickedTherapy(List<Treatment>? therapies) {
    final picked = therapies
        ?.where((item) => item.id == _pickedTherapyId)
        .firstOrNull;
    if (picked == null) return;
    setState(() {
      if (_treatment?.id != picked.id) {
        _useTherapy(
          TreatmentBooking(
            id: picked.id,
            name: picked.nameEnglish,
            durationMinutes: picked.durationMinutes,
          ),
        );
      }
      _step = _dateStep;
    });
  }

  Widget _buildDateStep(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final treatment = _treatment!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          treatment.name,
          style: theme.textTheme.titleLarge?.copyWith(
            fontFamily: AyurvedaFonts.serif,
            fontFamilyFallback: AyurvedaFonts.fallback,
            fontWeight: FontWeight.w700,
          ),
        ),
        if (treatment.durationMinutes != null ||
            treatment.doctorName != null) ...[
          const SizedBox(height: 6),
          Row(
            children: [
              if (treatment.durationMinutes != null) ...[
                Icon(
                  Icons.schedule,
                  size: 16,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 4),
              ],
              Flexible(
                child: Text(
                  [
                    if (treatment.durationMinutes != null)
                      copy.duration(treatment.durationMinutes!),
                    if (treatment.doctorName != null) treatment.doctorName!,
                  ].join(' • '),
                  style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
                ),
              ),
            ],
          ),
        ],
        const SizedBox(height: 8),
        Text(copy.chooseDate, style: theme.textTheme.headlineSmall),
        const SizedBox(height: 6),
        Text(copy.unavailableGrey),
        const SizedBox(height: 18),
        _AvailabilityNotice(availability: _availability),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: _dates.map(_dateButton).toList(),
        ),
        const SizedBox(height: 24),
        Row(
          children: [
            if (_canChangeTherapy) ...[
              Expanded(
                child: OutlinedButton(
                  key: BookAppointmentKeys.backToTherapy,
                  onPressed: () => setState(() => _step = _therapyStep),
                  child: Text(copy.back),
                ),
              ),
              const SizedBox(width: 12),
            ],
            Expanded(
              flex: _canChangeTherapy ? 1 : 2,
              child: FilledButton(
                key: BookAppointmentKeys.next,
                onPressed: _date == null
                    ? null
                    : () => setState(() => _step = _timeStep),
                child: Text(copy.chooseTime),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _dateButton(DateTime candidate) {
    final date = DateUtils.dateOnly(candidate);
    return FutureBuilder<TreatmentAvailability>(
      future: _availability[date],
      builder: (context, snapshot) {
        final loading = snapshot.connectionState != ConnectionState.done;
        final enabled = snapshot.hasData && snapshot.data!.available;
        final selected = DateUtils.isSameDay(_date, date);
        return SizedBox(
          width: 76,
          child: OutlinedButton(
            key: BookAppointmentKeys.date(date),
            onPressed: enabled
                ? () => setState(() {
                    _date = date;
                    _selectedAvailability = snapshot.data;
                    _slot = null;
                  })
                : null,
            style: OutlinedButton.styleFrom(
              minimumSize: const Size(76, 72),
              padding: const EdgeInsets.symmetric(vertical: 8),
              backgroundColor: selected
                  ? Theme.of(context).colorScheme.primaryContainer
                  : null,
            ),
            child: loading
                ? const SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(FeatureLocalizations.of(context).date('EEE', date)),
                      Text(
                        '${date.day}',
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
          ),
        );
      },
    );
  }

  Widget _buildSlotStep(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final slots =
        _selectedAvailability?.slots.where((slot) => slot.available).toList() ??
        const <TreatmentSlot>[];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(copy.chooseTime, style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 6),
        Text(copy.date('EEEE, d MMMM', _date!)),
        const SizedBox(height: 18),
        if (slots.isEmpty)
          ClinicCard(
            padding: const EdgeInsets.all(18),
            child: Text(copy.noSlots),
          )
        else
          Wrap(
            spacing: 10,
            runSpacing: 10,
            children: slots
                .map(
                  (slot) => ChoiceChip(
                    label: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(slot.time),
                        if (slot.doctorName != null)
                          Text(
                            slot.doctorName!,
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                      ],
                    ),
                    selected: _slot == slot,
                    onSelected: (_) => setState(() => _slot = slot),
                  ),
                )
                .toList(),
          ),
        const SizedBox(height: 24),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: () => setState(() => _step = _dateStep),
                child: Text(copy.back),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: FilledButton(
                onPressed: _slot == null
                    ? null
                    : () => setState(() => _step = _confirmStep),
                child: Text(copy.review),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildConfirmStep(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final treatment = _treatment!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          copy.confirmRequest,
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 16),
        ClinicCard(
          padding: const EdgeInsets.all(18),
          child: Column(
            children: [
              _SummaryRow(label: copy.treatment, value: treatment.name),
              const Divider(height: 26),
              _SummaryRow(
                label: copy.dateLabel,
                value: copy.date('EEE, d MMM yyyy', _date!),
              ),
              const Divider(height: 26),
              _SummaryRow(label: copy.timeLabel, value: _slot!.time),
              if ((_slot!.doctorName ?? treatment.doctorName) != null) ...[
                const Divider(height: 26),
                _SummaryRow(
                  label: copy.doctor,
                  value: _slot!.doctorName ?? treatment.doctorName!,
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: 14),
        _ReviewCallout(text: copy.appointmentReviewNote),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(top: 12),
            child: ErrorLine(key: BookAppointmentKeys.error, message: _error!),
          ),
        const SizedBox(height: 20),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: _submitting
                    ? null
                    : () => setState(() => _step = _timeStep),
                child: Text(copy.back),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: FilledButton(
                key: BookAppointmentKeys.submit,
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? SizedBox.square(
                        dimension: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Theme.of(context).colorScheme.onPrimary,
                        ),
                      )
                    : Text(
                        _isRescheduling
                            ? copy.text(
                                'Confirm Reschedule',
                                'වෙනස් කිරීම තහවුරු කරන්න',
                              )
                            : copy.requestAppointment,
                      ),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Future<void> _submit() async {
    final patientId = ref.read(authControllerProvider).user?.id;
    if (!_isRescheduling && (patientId == null || patientId.isEmpty)) {
      setState(
        () => _error = FeatureLocalizations.of(context).profileUnavailable,
      );
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      if (_isRescheduling) {
        await ref
            .read(appointmentRepositoryProvider)
            .reschedule(
              appointmentId: widget.appointmentIdToReschedule!,
              date: _date!,
              timeSlot: _slot!.time,
              scheduleId: _slot!.scheduleId,
            );
        ref.invalidate(myAppointmentsProvider);
      } else {
        await ref
            .read(appointmentRepositoryProvider)
            .create(
              patientId: patientId!,
              treatment: _treatment!,
              date: _date!,
              slot: _slot!,
            );
        ref.invalidate(myAppointmentsProvider);
      }
      if (mounted) setState(() => _pending = true);
    } catch (error) {
      if (mounted) setState(() => _error = describeBookingError(error));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Widget _buildPending(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(
        centerTitle: true,
        title: Text(
          _isRescheduling
              ? copy.text('Appointment Rescheduled', 'හමුවීම වෙනස් කරන ලදි')
              : copy.appointmentRequested,
        ),
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                _isRescheduling
                    ? Icons.check_circle_outline
                    : Icons.hourglass_top_rounded,
                key: BookAppointmentKeys.pendingConfirmation,
                size: 72,
                color: _isRescheduling
                    ? AyurvedaThemeExtension.of(context).teal
                    : Theme.of(context).colorScheme.secondary,
              ),
              const SizedBox(height: 18),
              Text(
                _isRescheduling
                    ? copy.text(
                        'Reschedule Successful',
                        'හමුවීමේ දිනය සාර්ථකව වෙනස් විය',
                      )
                    : copy.pendingApproval,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 10),
              Text(
                _isRescheduling
                    ? copy.text(
                        'Your appointment has been updated for ${DateFormat('EEE, MMM d').format(_date!)} at ${_slot!.time}.',
                        'ඔබගේ හමුවීම සාර්ථකව යාවත්කාලීන විය.',
                      )
                    : copy.pendingExplanation,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text(copy.done),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AvailabilityNotice extends StatelessWidget {
  const _AvailabilityNotice({required this.availability});

  final Map<DateTime, Future<TreatmentAvailability>> availability;

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<TreatmentAvailability>>(
      future: Future.wait(availability.values),
      builder: (context, snapshot) {
        if (!snapshot.hasError) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: ErrorLine(
            key: BookAppointmentKeys.error,
            message: describeBookingError(snapshot.error!),
          ),
        );
      },
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({required this.label, required this.value});
  final String label;
  final String value;
  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      SizedBox(
        width: 96,
        child: Padding(
          padding: const EdgeInsets.only(top: 3),
          child: Text(
            label.toUpperCase(),
            style: AyurvedaType.eyebrow(context),
          ),
        ),
      ),
      Expanded(
        child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
      ),
    ],
  );
}

class _ReviewCallout extends StatelessWidget {
  const _ReviewCallout({required this.text});
  final String text;
  @override
  Widget build(BuildContext context) => Container(
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
        Expanded(child: Text(text)),
      ],
    ),
  );
}
