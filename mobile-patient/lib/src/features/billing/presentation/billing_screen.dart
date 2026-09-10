import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/placeholder_panel.dart';

/// Invoices and treatment feedback share a tab for now; the label is a
/// placeholder until the two features are specced separately.
class BillingScreen extends StatelessWidget {
  const BillingScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navBilling)),
      body: PlaceholderPanel(
        icon: Icons.receipt_long_outlined,
        message: l10n.billingPlaceholder,
      ),
    );
  }
}
