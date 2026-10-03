import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../l10n/app_localizations.dart';
import '../application/records_provider.dart';
import '../data/patient_records_repository.dart';
import '../domain/patient_records.dart';
import 'document_viewer_screen.dart';
import 'record_labels.dart';
import 'records_panel.dart';

const _boundedButton = ButtonStyle(
  minimumSize: WidgetStatePropertyAll(Size(64, 48)),
  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
);

abstract final class DocumentsTabKeys {
  static const retry = ValueKey('documents-retry');
  static Key download(String id) => ValueKey('document-download-$id');
  static Key view(String id) => ValueKey('document-view-$id');
}

class DocumentsTab extends ConsumerWidget {
  const DocumentsTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final documents = ref.watch(documentsProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(documentsProvider);
        await ref.read(documentsProvider.future);
      },
      child: RecordsPanel(
        state: documents,
        errorMessage: l10n.documentsLoadError,
        emptyMessage: l10n.documentsEmpty,
        retryKey: DocumentsTabKeys.retry,
        onRetry: () => ref.invalidate(documentsProvider),
        itemBuilder: (document) => _DocumentCard(document: document),
      ),
    );
  }
}

class _DocumentCard extends ConsumerStatefulWidget {
  const _DocumentCard({required this.document});

  final MedicalDocument document;

  @override
  ConsumerState<_DocumentCard> createState() => _DocumentCardState();
}

class _DocumentCardState extends ConsumerState<_DocumentCard> {
  bool _saving = false;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final document = widget.document;
    final uploaded = document.uploadedAt;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              document.title,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              documentCategoryLabel(l10n, document.category),
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              [
                if (uploaded != null) DateFormat.yMMMd().format(uploaded.toLocal()),
                formatFileSize(document.fileSizeBytes),
              ].join(' · '),
              style: theme.textTheme.bodySmall,
            ),
            if (document.summary.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(document.summary),
            ],
            const SizedBox(height: 12),
            OutlinedButton(
              style: _boundedButton,
              key: DocumentsTabKeys.view(document.id),
              onPressed: () {
                Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (context) =>
                        DocumentViewerScreen(documentId: document.id),
                  ),
                );
              },
              child: Text(l10n.documentView),
            ),
            const SizedBox(height: 8),
            FilledButton(
              style: _boundedButton,
              key: DocumentsTabKeys.download(document.id),
              onPressed: _saving ? null : () => _download(document.id),
              child: Text(l10n.documentDownload),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _download(String id) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _saving = true);
    try {
      final file = await ref
          .read(patientRecordsRepositoryProvider)
          .downloadDocument(id);
      final saved = await ref.read(documentFileSaverProvider).save(
        fileName: file.fileName,
        bytes: file.bytes,
      );
      messenger.showSnackBar(SnackBar(content: Text(l10n.documentSaved(saved))));
    } catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.documentDownloadError)));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }
}
