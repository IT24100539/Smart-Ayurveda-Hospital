import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../auth/application/auth_controller.dart';
import '../../../l10n/feature_localizations.dart';
import '../data/appointment_repository.dart';
import '../domain/appointment_models.dart';

abstract final class BookAppointmentKeys {
  static Key date(DateTime date) =>
      ValueKey('booking-date-${DateFormat('yyyy-MM-dd').format(date)}');
  static const next = ValueKey('booking-next');
  static const submit = ValueKey('booking-submit');
  static const pendingConfirmation = ValueKey('booking-pending-confirmation');
}

class BookAppointmentFlow extends ConsumerStatefulWidget {
  const BookAppointmentFlow({
    required this.treatment,
    this.candidateDates,
    super.key,
  });

  final TreatmentBooking treatment;
  final List<DateTime>? candidateDates;

  @override
  ConsumerState<BookAppointmentFlow> createState() =>
      _BookAppointmentFlowState();
}

class _BookAppointmentFlowState extends ConsumerState<BookAppointmentFlow> {
  late final List<DateTime> _dates;
  late final Map<DateTime, Future<TreatmentAvailability>> _availability;
  int _step = 0;
  DateTime? _date;
  TreatmentAvailability? _selectedAvailability;
  TreatmentSlot? _slot;
  bool _submitting = false;
  bool _pending = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final today = DateUtils.dateOnly(DateTime.now());
    _dates =
        widget.candidateDates ??
        List.generate(14, (index) => today.add(Duration(days: index)));
    final repository = ref.read(appointmentRepositoryProvider);
    _availability = {
      for (final date in _dates)
        DateUtils.dateOnly(date): repository.availability(
          widget.treatment.id,
          date,
        ),
    };
  }

  @override
  Widget build(BuildContext context) {
    if (_pending) return _buildPending(context);
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(copy.bookAppointment)),
      body: SafeArea(
        child: Column(
          children: [
            _StepHeader(current: _step),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
                child: switch (_step) {
                  0 => _dateStep(context),
                  1 => _slotStep(context),
                  _ => _confirmStep(context),
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _dateStep(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        widget.treatment.name,
        style: Theme.of(
          context,
        ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
      ),
      if (widget.treatment.durationMinutes != null ||
          widget.treatment.doctorName != null) ...[
        const SizedBox(height: 6),
        Text(
          [
            if (widget.treatment.durationMinutes != null)
              FeatureLocalizations.of(
                context,
              ).duration(widget.treatment.durationMinutes!),
            if (widget.treatment.doctorName != null)
              widget.treatment.doctorName!,
          ].join(' • '),
          style: TextStyle(
            color: Theme.of(context).colorScheme.onSurfaceVariant,
          ),
        ),
      ],
      const SizedBox(height: 8),
      Text(
        FeatureLocalizations.of(context).chooseDate,
        style: Theme.of(context).textTheme.headlineSmall,
      ),
      const SizedBox(height: 6),
      Text(FeatureLocalizations.of(context).unavailableGrey),
      const SizedBox(height: 18),
      Wrap(
        spacing: 10,
        runSpacing: 10,
        children: _dates.map(_dateButton).toList(),
      ),
      const SizedBox(height: 24),
      FilledButton(
        key: BookAppointmentKeys.next,
        onPressed: _date == null ? null : () => setState(() => _step = 1),
        child: Text(FeatureLocalizations.of(context).chooseTime),
      ),
    ],
  );

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

  Widget _slotStep(BuildContext context) {
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
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Text(copy.noSlots),
            ),
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
                onPressed: () => setState(() => _step = 0),
                child: Text(copy.back),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: FilledButton(
                onPressed: _slot == null
                    ? null
                    : () => setState(() => _step = 2),
                child: Text(copy.review),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _confirmStep(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          copy.confirmRequest,
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              children: [
                _SummaryRow(
                  label: copy.treatment,
                  value: widget.treatment.name,
                ),
                const Divider(height: 26),
                _SummaryRow(
                  label: copy.dateLabel,
                  value: copy.date('EEE, d MMM yyyy', _date!),
                ),
                const Divider(height: 26),
                _SummaryRow(label: copy.timeLabel, value: _slot!.time),
                if ((_slot!.doctorName ?? widget.treatment.doctorName) !=
                    null) ...[
                  const Divider(height: 26),
                  _SummaryRow(
                    label: copy.doctor,
                    value: _slot!.doctorName ?? widget.treatment.doctorName!,
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 14),
        _ReviewCallout(text: copy.appointmentReviewNote),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(top: 12),
            child: Text(
              _error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ),
        const SizedBox(height: 20),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: _submitting ? null : () => setState(() => _step = 1),
                child: Text(copy.back),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: FilledButton(
                key: BookAppointmentKeys.submit,
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? const SizedBox.square(
                        dimension: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : Text(copy.requestAppointment),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Future<void> _submit() async {
    final patientId = ref.read(authControllerProvider).user?.id;
    if (patientId == null || patientId.isEmpty) {
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
      await ref
          .read(appointmentRepositoryProvider)
          .create(
            patientId: patientId,
            treatment: widget.treatment,
            date: _date!,
            slot: _slot!,
          );
      if (mounted) setState(() => _pending = true);
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Widget _buildPending(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(FeatureLocalizations.of(context).appointmentRequested),
    ),
    body: Center(
      child: Padding(
        padding: const EdgeInsets.all(28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.hourglass_top_rounded,
              key: BookAppointmentKeys.pendingConfirmation,
              size: 72,
              color: Theme.of(context).colorScheme.secondary,
            ),
            const SizedBox(height: 18),
            Text(
              FeatureLocalizations.of(context).pendingApproval,
              style: Theme.of(
                context,
              ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 10),
            Text(
              FeatureLocalizations.of(context).pendingExplanation,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => Navigator.of(context).pop(),
              child: Text(FeatureLocalizations.of(context).done),
            ),
          ],
        ),
      ),
    ),
  );
}

class _StepHeader extends StatelessWidget {
  const _StepHeader({required this.current});
  final int current;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
    child: Row(
      children: List.generate(
        3,
        (index) => Expanded(
          child: Padding(
            padding: EdgeInsets.only(right: index == 2 ? 0 : 6),
            child: LinearProgressIndicator(
              value: index <= current ? 1 : 0,
              minHeight: 5,
              borderRadius: BorderRadius.circular(5),
            ),
          ),
        ),
      ),
    ),
  );
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
        width: 88,
        child: Text(
          label,
          style: TextStyle(
            color: Theme.of(context).colorScheme.onSurfaceVariant,
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
