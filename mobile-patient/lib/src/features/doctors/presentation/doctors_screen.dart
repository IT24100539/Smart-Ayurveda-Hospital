import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../application/doctors_provider.dart';
import '../domain/doctor.dart';
import 'widgets/doctor_avatar.dart';
import 'widgets/doctor_skeleton.dart';

class DoctorsScreen extends ConsumerWidget {
  const DoctorsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final doctors = ref.watch(doctorsListProvider);
    final query = ref.watch(doctorsSearchQueryProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.doctorsTitle)),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 4),
            child: TextField(
              key: DoctorsScreenKeys.search,
              decoration: InputDecoration(
                hintText: l10n.doctorsSearchHint,
                prefixIcon: const Icon(Icons.search),
              ),
              onChanged: (value) {
                ref.read(doctorsSearchQueryProvider.notifier).state = value;
              },
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async {
                ref.invalidate(doctorsListProvider);
                await ref.read(doctorsListProvider.future);
              },
              child: doctors.when(
                loading: () => const DoctorListSkeleton(),
                error: (error, stackTrace) => ErrorState(
                  message: l10n.doctorsLoadError,
                  actionLabel: l10n.retry,
                  actionKey: DoctorsScreenKeys.retry,
                  onAction: () => ref.invalidate(doctorsListProvider),
                  scrollable: true,
                ),
                data: (items) => _DoctorList(
                  doctors: items,
                  query: query,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _DoctorList extends StatelessWidget {
  const _DoctorList({required this.doctors, required this.query});

  final List<Doctor> doctors;
  final String query;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final searching = query.trim().isNotEmpty;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
      children: [
        SectionBanner(
          kicker: l10n.appTitle,
          title: l10n.doctorsTitle,
          body: l10n.doctorsSubtitle,
        ),
        if (doctors.isEmpty)
          EmptyState(
            message: searching ? l10n.doctorsSearchEmpty : l10n.doctorsEmpty,
            icon: Icons.groups_outlined,
          )
        else
          ...doctors.map(
            (doctor) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _DoctorCard(doctor: doctor),
            ),
          ),
      ],
    );
  }
}

class _DoctorCard extends StatelessWidget {
  const _DoctorCard({required this.doctor});

  final Doctor doctor;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Card(
      child: InkWell(
        key: ValueKey('doctor-tile-${doctor.id}'),
        borderRadius: BorderRadius.circular(16),
        onTap: () => context.push(AppRoutes.doctorById(doctor.id)),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              DoctorAvatar(doctor: doctor),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      doctor.name,
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      doctor.specialty,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    if (doctor.showsRating) ...[
                      const SizedBox(height: 4),
                      Text(
                        l10n.doctorRatingSummary(
                          formatDoctorRating(doctor.rating!),
                          doctor.ratingCount!,
                        ),
                        key: ValueKey('doctor-rating-${doctor.id}'),
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
