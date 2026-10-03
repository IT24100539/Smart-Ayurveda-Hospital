import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/patient_records.dart';

abstract interface class PatientRecordsRepository {
  Future<List<Prescription>> listPrescriptions();
  Future<List<Invoice>> listInvoices();
  Future<List<MedicalDocument>> listDocuments();
  Future<ClinicalFile> downloadDocument(String id);
}

class DioPatientRecordsRepository implements PatientRecordsRepository {
  const DioPatientRecordsRepository(this._dio);

  final Dio _dio;

  static const _pageSize = 100;

  @override
  Future<List<Prescription>> listPrescriptions() => _list(
    '/prescriptions/mine',
    Prescription.fromJson,
  );

  @override
  Future<List<Invoice>> listInvoices() => _list(
    '/invoices/mine',
    Invoice.fromJson,
  );

  @override
  Future<List<MedicalDocument>> listDocuments() => _list(
    '/medical-documents/mine',
    MedicalDocument.fromJson,
  );

  /// Bytes from the ownership-checked file route. The public file URL is not used.
  @override
  Future<ClinicalFile> downloadDocument(String id) async {
    try {
      final response = await _dio.get<List<int>>(
        '/medical-documents/$id/file',
        options: Options(responseType: ResponseType.bytes),
      );
      final data = response.data;
      if (data == null || data.isEmpty) {
        throw const ApiException(statusCode: 404);
      }
      final header = response.headers.value(Headers.contentTypeHeader);
      final contentType = (header ?? 'application/octet-stream')
          .split(';')
          .first
          .trim();
      return ClinicalFile(
        bytes: Uint8List.fromList(data),
        contentType: contentType,
        fileName: fileNameFromDisposition(
          response.headers.value('content-disposition'),
          contentType,
        ),
      );
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }

  Future<List<T>> _list<T>(
    String path,
    T Function(Map<String, dynamic> json) parse,
  ) async {
    final items = <T>[];
    var received = 0;
    var page = 1;
    try {
      while (page <= 20) {
        final response = await _dio.get<Map<String, dynamic>>(
          path,
          queryParameters: {'page': page, 'pageSize': _pageSize},
        );
        final parsed = RecordPage.fromJson(response.data!, parse);
        received += parsed.items.length;
        items.addAll(parsed.items);
        if (parsed.items.isEmpty || received >= parsed.totalCount) break;
        page++;
      }
      return items;
    } on DioException catch (error) {
      throw ApiException.fromDioException(error);
    }
  }
}

String fileNameFromDisposition(String? disposition, String contentType) {
  if (disposition != null) {
    final match = RegExp(
      'filename="([^"]+)"',
      caseSensitive: false,
    ).firstMatch(disposition);
    final name = match?.group(1)?.trim();
    if (name != null && name.isNotEmpty) return name;
  }
  return switch (contentType) {
    'application/pdf' => 'document.pdf',
    'image/png' => 'document.png',
    'image/webp' => 'document.webp',
    'image/gif' => 'document.gif',
    'image/jpeg' => 'document.jpg',
    _ => 'document',
  };
}

final patientRecordsRepositoryProvider = Provider<PatientRecordsRepository>((ref) {
  return DioPatientRecordsRepository(ref.watch(dioProvider));
});
