import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../application/treatments_provider.dart';
import 'widgets/treatment_card.dart';

class TreatmentsScreen extends ConsumerWidget {
  const TreatmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final copy = FeatureLocalizations.of(context);
    final treatmentsState = ref.watch(filteredTreatmentsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navTreatments)),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Column(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(20),
                  child: Image.asset(
                    'assets/images/herbal-oils.png',
                    height: 140,
                    width: double.infinity,
                    fit: BoxFit.cover,
                  ),
                ),
                const SizedBox(height: 12),
                SectionBanner(
                  kicker: copy.text('Therapies', 'ප්‍රතිකාර'),
                  title: l10n.navTreatments,
                  body: copy.text(
                    'Panchakarma, abhyanga, and the days each therapy is offered.',
                    'පංචකර්ම, අභ්‍යංග සහ එක් එක් ප්‍රතිකාරය ලබා දෙන දින.',
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: TextField(
              decoration: const InputDecoration(
                hintText: 'Search treatments...',
                prefixIcon: Icon(Icons.search),
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
                  return const EmptyState(message: 'No treatments found.');
                }
                return ListView.builder(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
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
              error: (error, stack) =>
                  ErrorState(message: 'Error loading treatments: $error'),
            ),
          ),
        ],
      ),
    );
  }
}
