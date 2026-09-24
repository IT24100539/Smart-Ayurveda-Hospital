import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/placeholder_panel.dart';
import '../../feedback/presentation/submit_feedback_screen.dart';

/// A visit the appointments list can hand to the feedback form.
class PatientVisit {
  const PatientVisit({
    required this.id,
    required this.reason,
    required this.status,
    this.treatmentId,
  });

  final String id;
  final String? treatmentId;
  final String reason;

  /// API `AppointmentStatus` name, for example `Completed`.
  final String status;

  bool get isCompleted => status == 'Completed';
}

/// The signed-in patient's visits. Widget tests pass [AppointmentsScreen.visits]
/// and skip this request.
final myVisitsProvider = FutureProvider<List<PatientVisit>>((ref) async {
  final dio = ref.watch(dioProvider);
  try {
    final response = await dio.get<Map<String, dynamic>>('/appointments/mine');
    final raw = response.data?['items'];
    if (raw is! List) return const [];
    return raw
        .map(
          (item) => PatientVisit(
            id: (item as Map)['id'] as String? ?? '',
            reason: item['reason'] as String? ?? '',
            status: item['status'] as String? ?? '',
          ),
        )
        .toList();
  } on DioException catch (error) {
    throw ApiException.fromDioException(error);
  }
});

class AppointmentsScreen extends ConsumerWidget {
  const AppointmentsScreen({super.key, this.visits});

  /// When set, the screen shows this list instead of calling the API.
  final List<PatientVisit>? visits;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (visits != null) {
      return _AppointmentsBody(visits: visits!);
    }

    final l10n = AppLocalizations.of(context);
    final loaded = ref.watch(myVisitsProvider);
    return loaded.when(
      loading: () => Scaffold(
        appBar: AppBar(title: Text(l10n.navAppointments)),
        body: const Center(child: CircularProgressIndicator()),
      ),
      error: (error, _) => Scaffold(
        appBar: AppBar(title: Text(l10n.navAppointments)),
        body: PlaceholderPanel(
          icon: Icons.cloud_off_outlined,
          message: error is ApiException && error.isNetworkError
              ? l10n.networkErrorMessage
              : (error is ApiException ? error.message : null) ??
                    l10n.genericErrorMessage,
        ),
      ),
      data: (items) => _AppointmentsBody(visits: items),
    );
  }
}

class _AppointmentsBody extends StatelessWidget {
  const _AppointmentsBody({required this.visits});

  final List<PatientVisit> visits;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navAppointments)),
      body: visits.isEmpty
          ? PlaceholderPanel(
              icon: Icons.event_available_outlined,
              message: l10n.appointmentsPlaceholder,
            )
          : ListView.separated(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              itemCount: visits.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                final visit = visits[index];
                return Card(
                  child: ListTile(
                    title: Text(visit.reason),
                    subtitle: Text(visit.status),
                    trailing: visit.isCompleted
                        ? LeaveFeedbackButton(
                            appointmentId: visit.id,
                            treatmentId: visit.treatmentId,
                            expanded: false,
                          )
                        : null,
                  ),
                );
              },
            ),
    );
  }
}
