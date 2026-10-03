import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../theme/app_theme.dart';
import '../application/records_provider.dart';
import '../domain/patient_records.dart';
import 'record_labels.dart';
import 'records_panel.dart';

abstract final class InvoicesTabKeys {
  static const retry = ValueKey('invoices-retry');
}

class InvoicesTab extends ConsumerWidget {
  const InvoicesTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final invoices = ref.watch(invoicesProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(invoicesProvider);
        await ref.read(invoicesProvider.future);
      },
      child: RecordsPanel(
        state: invoices,
        errorMessage: l10n.invoicesLoadError,
        emptyMessage: l10n.invoicesEmpty,
        retryKey: InvoicesTabKeys.retry,
        onRetry: () => ref.invalidate(invoicesProvider),
        itemBuilder: (invoice) => _InvoiceCard(invoice: invoice),
      ),
    );
  }
}

class _InvoiceCard extends StatelessWidget {
  const _InvoiceCard({required this.invoice});

  final Invoice invoice;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    invoice.invoiceNumber,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                _StatusChip(
                  status: invoice.status,
                  label: invoiceStatusLabel(l10n, invoice.status),
                ),
              ],
            ),
            if (invoice.issuedAt != null) ...[
              const SizedBox(height: 4),
              Text(
                DateFormat.yMMMd().format(invoice.issuedAt!.toLocal()),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            const SizedBox(height: 12),
            for (final line in invoice.lines)
              Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: Row(
                  children: [
                    Expanded(child: Text(line.description)),
                    Text('× ${line.quantity}'),
                    const SizedBox(width: 12),
                    Text(
                      formatMoney(invoice.currency, line.lineTotal),
                      style: AyurvedaType.price(context),
                    ),
                  ],
                ),
              ),
            const Divider(height: 20),
            _AmountRow(
              label: l10n.invoiceTotalLabel,
              value: formatMoney(invoice.currency, invoice.total),
            ),
            _AmountRow(
              label: l10n.amountPaidLabel,
              value: formatMoney(invoice.currency, invoice.amountPaid),
            ),
            _AmountRow(
              label: l10n.balanceLabel,
              value: formatMoney(invoice.currency, invoice.balance),
            ),
            if (invoice.notes != null) ...[
              const SizedBox(height: 8),
              Text(invoice.notes!),
            ],
            const SizedBox(height: 14),
            Text(
              l10n.paymentHistoryTitle,
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 6),
            if (invoice.payments.isEmpty)
              Text(
                l10n.paymentsEmpty,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              )
            else
              for (final payment in invoice.payments)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: _PaymentRow(invoice: invoice, payment: payment),
                ),
          ],
        ),
      ),
    );
  }
}

class _PaymentRow extends StatelessWidget {
  const _PaymentRow({required this.invoice, required this.payment});

  final Invoice invoice;
  final InvoicePayment payment;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final when = payment.paidOn == null
        ? null
        : DateFormat.yMMMd().add_jm().format(payment.paidOn!.toLocal());

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '${formatMoney(invoice.currency, payment.amount)} · ${paymentMethodLabel(l10n, payment.method)}',
          style: theme.textTheme.bodyMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        if (when != null)
          Text(
            when,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        if (payment.reference != null)
          Text(
            payment.reference!,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
      ],
    );
  }
}

class _AmountRow extends StatelessWidget {
  const _AmountRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        children: [
          Expanded(child: Text(label)),
          Text(value, style: AyurvedaType.price(context).copyWith(fontSize: 16)),
        ],
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status, required this.label});

  final String status;
  final String label;

  @override
  Widget build(BuildContext context) {
    final paid = status == 'Paid';
    final brand = AyurvedaThemeExtension.of(context);
    return PillChip(
      label: label,
      icon: paid ? Icons.check : null,
      background: paid ? brand.approvedBackground : brand.pillBackground,
      foreground: paid ? brand.approvedForeground : brand.pillForeground,
    );
  }
}
