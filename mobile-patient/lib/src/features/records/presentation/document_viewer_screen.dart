import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../application/records_provider.dart';
import '../domain/patient_records.dart';

abstract final class DocumentViewerKeys {
  static const retry = ValueKey('document-viewer-retry');
  static const image = ValueKey('document-viewer-image');
  static const download = ValueKey('document-viewer-download');
  static const fileNotice = ValueKey('document-viewer-file-notice');
}

class DocumentViewerScreen extends ConsumerWidget {
  const DocumentViewerScreen({required this.documentId, super.key});

  final String documentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final file = ref.watch(documentFileProvider(documentId));

    return Scaffold(
      appBar: AppBar(title: Text(file.valueOrNull?.fileName ?? l10n.myDocumentsTitle)),
      body: file.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stackTrace) => ErrorState(
          message: l10n.documentOpenError,
          actionLabel: l10n.retry,
          actionKey: DocumentViewerKeys.retry,
          onAction: () => ref.invalidate(documentFileProvider(documentId)),
          scrollable: true,
        ),
        data: (opened) => _OpenedFile(file: opened),
      ),
    );
  }
}

class _OpenedFile extends ConsumerWidget {
  const _OpenedFile({required this.file});

  final ClinicalFile file;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      children: [
        if (file.isImage)
          ClipRRect(
            borderRadius: BorderRadius.circular(16),
            child: Image.memory(
              file.bytes,
              key: DocumentViewerKeys.image,
              fit: BoxFit.contain,
              errorBuilder: (context, error, stackTrace) =>
                  Text(l10n.documentOpenError),
            ),
          )
        else
          Text(
            file.isPdf ? l10n.documentPdfNotice : l10n.documentFileNotice,
            key: DocumentViewerKeys.fileNotice,
          ),
        const SizedBox(height: 16),
        FilledButton(
          style: const ButtonStyle(
            minimumSize: WidgetStatePropertyAll(Size(64, 48)),
          ),
          key: DocumentViewerKeys.download,
          onPressed: () => _save(context, ref),
          child: Text(l10n.documentDownload),
        ),
      ],
    );
  }

  Future<void> _save(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    try {
      final saved = await ref.read(documentFileSaverProvider).save(
        fileName: file.fileName,
        bytes: file.bytes,
      );
      messenger.showSnackBar(SnackBar(content: Text(l10n.documentSaved(saved))));
    } catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.documentDownloadError)));
    }
  }
}
