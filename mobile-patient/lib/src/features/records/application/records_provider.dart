import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/document_file_saver.dart';
import '../data/patient_records_repository.dart';
import '../domain/patient_records.dart';

final prescriptionsProvider = FutureProvider.autoDispose<List<Prescription>>((ref) {
  return ref.watch(patientRecordsRepositoryProvider).listPrescriptions();
});

final invoicesProvider = FutureProvider.autoDispose<List<Invoice>>((ref) {
  return ref.watch(patientRecordsRepositoryProvider).listInvoices();
});

final documentsProvider = FutureProvider.autoDispose<List<MedicalDocument>>((ref) {
  return ref.watch(patientRecordsRepositoryProvider).listDocuments();
});

final documentFileProvider = FutureProvider.autoDispose.family<ClinicalFile, String>((
  ref,
  id,
) {
  return ref.watch(patientRecordsRepositoryProvider).downloadDocument(id);
});

final documentFileSaverProvider = Provider<DocumentFileSaver>((ref) {
  return createDocumentFileSaver();
});
