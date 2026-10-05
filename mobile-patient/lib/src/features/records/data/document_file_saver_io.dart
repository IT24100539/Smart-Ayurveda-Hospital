import 'dart:io';
import 'dart:typed_data';

abstract interface class DocumentFileSaver {
  Future<String> save({required String fileName, required Uint8List bytes});
}

DocumentFileSaver createDocumentFileSaver() => const IoDocumentFileSaver();

final class IoDocumentFileSaver implements DocumentFileSaver {
  const IoDocumentFileSaver();

  @override
  Future<String> save({required String fileName, required Uint8List bytes}) async {
    final safeName = fileName.replaceAll(RegExp(r'[\\/:*?"<>|]'), '_');
    final file = File(
      '${Directory.systemTemp.path}${Platform.pathSeparator}sah-$safeName',
    );
    await file.writeAsBytes(bytes, flush: true);
    return file.path;
  }
}
