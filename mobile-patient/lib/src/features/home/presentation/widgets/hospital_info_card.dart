import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../../router/app_routes.dart';
import '../../../../theme/app_theme.dart';

class HospitalInfoCard extends StatelessWidget {
  const HospitalInfoCard({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Card(
      elevation: 2,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(
          color: theme.colorScheme.outline.withValues(alpha: 0.5),
        ),
      ),
      child: InkWell(
        onTap: () => context.push(AppRoutes.contact),
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
            Row(
              children: [
                const Icon(
                  Icons.local_hospital_outlined,
                  color: AyurvedaColors.forest,
                  size: 24,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    l10n.homeHospitalName,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                      fontFamily: 'serif',
                    ),
                  ),
                ),
              ],
            ),
            const Divider(height: 20),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(
                  Icons.location_on_outlined,
                  size: 18,
                  color: AyurvedaColors.inkMuted,
                ),
                const SizedBox(width: 8),
                Expanded(child: Text(l10n.homeHospitalAddress)),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                const Icon(
                  Icons.phone_outlined,
                  size: 18,
                  color: AyurvedaColors.inkMuted,
                ),
                const SizedBox(width: 8),
                Text(l10n.homeHospitalPhone),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                const Icon(
                  Icons.access_time_outlined,
                  size: 18,
                  color: AyurvedaColors.inkMuted,
                ),
                const SizedBox(width: 8),
                Text(l10n.homeHospitalHours),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}
}
