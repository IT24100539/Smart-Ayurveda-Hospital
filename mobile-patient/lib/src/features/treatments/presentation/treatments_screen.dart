import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/placeholder_panel.dart';

class TreatmentsScreen extends StatelessWidget {
  const TreatmentsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navTreatments)),
      body: PlaceholderPanel(
        icon: Icons.spa_outlined,
        message: l10n.treatmentsPlaceholder,
      ),
    );
  }
}
