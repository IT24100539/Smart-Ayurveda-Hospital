import 'dart:typed_data';

abstract interface class DocumentFileSaver {
  Future<String> save({required String fileName, required Uint8List bytes});
}

DocumentFileSaver createDocumentFileSaver() => const _UnsupportedDocumentFileSaver();

final class _UnsupportedDocumentFileSaver implements DocumentFileSaver {
  const _UnsupportedDocumentFileSaver();

  @override
  Future<String> save({required String fileName, required Uint8List bytes}) {
    throw UnsupportedError('Saving a document is not available on this platform.');
  }
}
