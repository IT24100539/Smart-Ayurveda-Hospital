import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../../router/app_routes.dart';
import '../../../../shared/widgets/clinic_widgets.dart';
import '../../../../theme/app_theme.dart';

class HospitalInfoCard extends StatelessWidget {
  const HospitalInfoCard({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);

    return ClinicCard(
      padding: EdgeInsets.zero,
      child: InkWell(
        onTap: () => context.push(AppRoutes.contact),
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Icons.local_hospital_outlined, color: brand.teal, size: 24),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      l10n.homeHospitalName,
                      style: theme.textTheme.titleMedium,
                    ),
                  ),
                  Icon(
                    Icons.chevron_right,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ],
              ),
              const Divider(height: 24),
              _InfoRow(icon: Icons.location_on_outlined, text: l10n.homeHospitalAddress),
              const SizedBox(height: 8),
              _InfoRow(icon: Icons.phone_outlined, text: l10n.homeHospitalPhone),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.access_time_outlined,
                text: l10n.homeHospitalHours,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(
          icon,
          size: 18,
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        ),
        const SizedBox(width: 8),
        Expanded(child: Text(text)),
      ],
    );
  }
}
