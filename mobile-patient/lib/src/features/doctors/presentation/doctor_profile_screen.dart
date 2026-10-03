import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../application/doctors_provider.dart';
import '../domain/doctor.dart';
import 'widgets/doctor_avatar.dart';
import 'widgets/doctor_skeleton.dart';

class DoctorProfileScreen extends ConsumerWidget {
  const DoctorProfileScreen({required this.doctorId, super.key});

  final String doctorId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final doctor = ref.watch(doctorProvider(doctorId));

    return Scaffold(
      appBar: AppBar(
        title: Text(doctor.valueOrNull?.name ?? l10n.doctorsTitle),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(doctorProvider(doctorId));
          await ref.read(doctorProvider(doctorId).future);
        },
        child: doctor.when(
          loading: () => const DoctorProfileSkeleton(),
          error: (error, stackTrace) => ErrorState(
            message: l10n.doctorProfileLoadError,
            actionLabel: l10n.retry,
            actionKey: DoctorProfileKeys.retry,
            onAction: () => ref.invalidate(doctorProvider(doctorId)),
            scrollable: true,
          ),
          data: (item) => _DoctorProfile(doctor: item),
        ),
      ),
    );
  }
}

class _DoctorProfile extends StatelessWidget {
  const _DoctorProfile({required this.doctor});

  final Doctor doctor;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 32),
      children: [
        Center(child: DoctorAvatar(doctor: doctor, radius: 52)),
        const SizedBox(height: 16),
        Text(
          doctor.name,
          textAlign: TextAlign.center,
          style: theme.textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          doctor.specialty,
          textAlign: TextAlign.center,
          style: theme.textTheme.titleMedium?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
        if (doctor.showsRating) ...[
          const SizedBox(height: 8),
          Text(
            l10n.doctorRatingSummary(
              formatDoctorRating(doctor.rating!),
              doctor.ratingCount!,
            ),
            key: DoctorProfileKeys.rating,
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium,
          ),
        ],
        const SizedBox(height: 24),
        Text(
          l10n.doctorQualificationsLabel,
          style: theme.textTheme.titleSmall?.copyWith(
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 6),
        Text(doctor.qualifications),
        if (doctor.bio != null) ...[
          const SizedBox(height: 20),
          Text(
            l10n.doctorAboutLabel,
            style: theme.textTheme.titleSmall?.copyWith(
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 6),
          Text(doctor.bio!),
        ],
      ],
    );
  }
}
