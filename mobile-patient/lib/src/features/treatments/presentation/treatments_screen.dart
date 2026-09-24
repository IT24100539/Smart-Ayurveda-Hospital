import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../application/treatments_provider.dart';
import 'widgets/treatment_card.dart';

class TreatmentsScreen extends ConsumerWidget {
  const TreatmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final treatmentsState = ref.watch(filteredTreatmentsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navTreatments)),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: TextField(
              decoration: const InputDecoration(
                hintText: 'Search treatments...',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(),
              ),
              onChanged: (value) {
                ref.read(treatmentsSearchQueryProvider.notifier).state = value;
              },
            ),
          ),
          Expanded(
            child: treatmentsState.when(
              data: (treatments) {
                if (treatments.isEmpty) {
                  return const Center(child: Text('No treatments found.'));
                }
                return ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: treatments.length,
                  itemBuilder: (context, index) {
                    final treatment = treatments[index];
                    return TreatmentCard(
                      treatment: treatment,
                      onTap: () {
                        context.go('/treatments/${treatment.id}');
                      },
                    );
                  },
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, stack) => Center(
                child: Text('Error loading treatments: $error'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

