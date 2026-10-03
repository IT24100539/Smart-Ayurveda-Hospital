import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/health_hub/data/health_hub_repository.dart';
import 'package:patient_app/src/features/health_hub/domain/health_hub_models.dart';
import 'package:patient_app/src/features/health_hub/presentation/health_hub_screen.dart';
import 'package:patient_app/src/features/records/application/records_provider.dart';
import 'package:patient_app/src/features/records/data/document_file_saver.dart';
import 'package:patient_app/src/features/records/data/patient_records_repository.dart';
import 'package:patient_app/src/features/records/domain/patient_records.dart';
import 'package:patient_app/src/features/records/presentation/document_viewer_screen.dart';
import 'package:patient_app/src/features/records/presentation/documents_tab.dart';
import 'package:patient_app/src/features/records/presentation/invoices_tab.dart';
import 'package:patient_app/src/features/records/presentation/prescriptions_tab.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

void main() {
  test('parses prescriptions and leaves missing medicines empty', () {
    final prescription = Prescription.fromJson({
      'id': 'rx-1',
      'doctorName': 'Anjali Perera',
      'status': 'Issued',
      'revisionNumber': 2,
      'issuedAt': '2026-10-02T08:30:00Z',
      'items': [
        {
          'id': 'item-1',
          'name': 'Ashwagandha',
          'dosage': '1 tsp',
          'frequency': 'Twice daily',
          'duration': '14 days',
          'instructions': 'After meals',
        },
      ],
    });

    expect(prescription.doctorName, 'Anjali Perera');
    expect(prescription.status, 'Issued');
    expect(prescription.items.single.name, 'Ashwagandha');
    expect(prescription.issuedAt, isNotNull);

    final bare = Prescription.fromJson({
      'id': 'rx-2',
      'doctorName': 'Nimal Silva',
      'status': 'Cancelled',
    });
    expect(bare.items, isEmpty);
    expect(bare.issuedAt, isNull);
  });

  test('parses an invoice without inventing a payment', () {
    final paid = Invoice.fromJson({
      'id': 'inv-1',
      'invoiceNumber': 'SAH-1001',
      'currency': 'LKR',
      'status': 'Paid',
      'total': 4500,
      'amountPaid': 4500,
      'balance': 0,
      'lines': [
        {
          'id': 'line-1',
          'description': 'Abhyanga',
          'quantity': 1,
          'lineTotal': 4500,
        },
      ],
      'payments': [
        {
          'id': 'pay-1',
          'amount': 4500,
          'method': 'Cash',
          'paidOn': '2026-10-01T09:00:00Z',
          'reference': 'RCPT-9',
        },
      ],
    });
    expect(paid.payments.single.method, 'Cash');
    expect(formatMoney(paid.currency, paid.total), 'LKR 4500.00');

    final issued = Invoice.fromJson({
      'id': 'inv-2',
      'invoiceNumber': 'SAH-1002',
      'currency': 'LKR',
      'status': 'Issued',
      'total': 2000,
      'amountPaid': 0,
      'balance': 2000,
    });
    expect(issued.payments, isEmpty);
    expect(issued.lines, isEmpty);
  });

  test('lists the patient endpoints and downloads through the file route', () async {
    final png = Uint8List.fromList(const [1, 2, 3, 4]);
    final adapter = _ScriptedAdapter((options) {
      if (options.path == '/medical-documents/doc-1/file') {
        expect(options.responseType, ResponseType.bytes);
        return png;
      }
      final body = switch (options.path) {
        '/prescriptions/mine' => {
          'items': [
            {
              'id': 'rx-1',
              'doctorName': 'Anjali Perera',
              'status': 'Issued',
              'items': <Object>[],
            },
          ],
          'totalCount': 1,
        },
        '/invoices/mine' => {
          'items': [
            {
              'id': 'inv-1',
              'invoiceNumber': 'SAH-1001',
              'currency': 'LKR',
              'status': 'Issued',
              'total': 10,
              'amountPaid': 0,
              'balance': 10,
            },
          ],
          'totalCount': 1,
        },
        '/medical-documents/mine' => {
          'items': [
            {
              'id': 'doc-1',
              'title': 'Prakriti note',
              'category': 'General',
              'contentType': 'application/pdf',
              'fileSizeBytes': 12,
              'summary': '',
              'fileUrl': '/api/medical-documents/doc-1/file',
            },
          ],
          'totalCount': 1,
        },
        _ => throw StateError('unexpected ${options.path}'),
      };
      return jsonEncode(body);
    }, bytesPaths: {'/medical-documents/doc-1/file'});
    final repository = DioPatientRecordsRepository(_dio(adapter));

    expect((await repository.listPrescriptions()).single.doctorName, 'Anjali Perera');
    expect((await repository.listInvoices()).single.payments, isEmpty);
    final documents = await repository.listDocuments();
    expect(documents.single.title, 'Prakriti note');

    final file = await repository.downloadDocument('doc-1');
    expect(file.bytes, png);
    expect(file.fileName, 'document.pdf');
    expect(
      adapter.calls.map((call) => call.path),
      containsAll([
        '/prescriptions/mine',
        '/invoices/mine',
        '/medical-documents/mine',
        '/medical-documents/doc-1/file',
      ]),
    );
  });

  testWidgets('health hub tabs show prescriptions, invoices, and documents', (
    tester,
  ) async {
    final saver = _MemorySaver();
    final repository = _FakeRecords()
      ..prescriptions = [
        Prescription(
          id: 'rx-1',
          doctorName: 'Anjali Perera',
          status: 'Issued',
          revisionNumber: 1,
          issuedAt: DateTime.utc(2026, 10, 2),
          items: const [
            PrescriptionItem(
              id: 'item-1',
              name: 'Ashwagandha',
              dosage: '1 tsp',
              frequency: 'Twice daily',
              duration: '14 days',
              instructions: 'After meals',
            ),
          ],
        ),
      ]
      ..invoices = [
        const Invoice(
          id: 'inv-1',
          invoiceNumber: 'SAH-1001',
          currency: 'LKR',
          status: 'Paid',
          total: 4500,
          amountPaid: 4500,
          balance: 0,
          lines: [
            InvoiceLine(
              id: 'line-1',
              description: 'Abhyanga',
              quantity: 1,
              lineTotal: 4500,
            ),
          ],
          payments: [
            InvoicePayment(
              id: 'pay-1',
              amount: 4500,
              method: 'Cash',
              paidOn: null,
              reference: 'RCPT-9',
            ),
          ],
        ),
        const Invoice(
          id: 'inv-2',
          invoiceNumber: 'SAH-1002',
          currency: 'LKR',
          status: 'Issued',
          total: 2000,
          amountPaid: 0,
          balance: 2000,
          lines: [],
          payments: [],
        ),
      ]
      ..documents = [
        MedicalDocument(
          id: 'doc-1',
          title: 'Nadi chart',
          category: 'DiagnosticScan',
          contentType: 'image/png',
          fileSizeBytes: _png.length,
          uploadedAt: DateTime.utc(2026, 9, 1),
          summary: 'Recorded at the desk.',
        ),
      ]
      ..file = ClinicalFile(
        bytes: Uint8List.fromList(_png),
        contentType: 'image/png',
        fileName: 'document.png',
      );

    await tester.pumpWidget(_hub(repository, saver));
    await tester.pumpAndSettle();

    expect(find.text('Upcoming'), findsOneWidget);
    expect(find.text('Prescriptions'), findsOneWidget);
    expect(find.text('Coming soon'), findsNothing);

    await tester.tap(find.byKey(HealthHubTabKeys.prescriptions));
    await tester.pumpAndSettle();
    expect(find.text('Ashwagandha'), findsOneWidget);
    expect(find.text('Anjali Perera'), findsOneWidget);
    expect(find.text('Issued'), findsOneWidget);

    await tester.tap(find.byKey(HealthHubTabKeys.invoices));
    await tester.pumpAndSettle();
    expect(find.text('SAH-1001'), findsOneWidget);
    expect(find.text('Payment history'), findsWidgets);
    expect(find.text('LKR 4500.00 · Cash'), findsOneWidget);
    expect(find.text('RCPT-9'), findsOneWidget);
    expect(find.text('No payments recorded.'), findsOneWidget);
    expect(find.text('LKR 0.00 · Cash'), findsNothing);

    await tester.tap(find.byKey(HealthHubTabKeys.documents));
    await tester.pumpAndSettle();
    expect(find.text('Nadi chart'), findsOneWidget);
    expect(find.text('Diagnostic scan'), findsOneWidget);

    await tester.tap(find.byKey(DocumentsTabKeys.download('doc-1')));
    await tester.pumpAndSettle();
    expect(repository.downloadIds, ['doc-1']);
    expect(saver.fileName, 'document.png');
    expect(saver.bytes, _png);
    expect(find.text('Saved saved-document.png'), findsOneWidget);

    await tester.ensureVisible(find.byKey(DocumentsTabKeys.view('doc-1')));
    await tester.tap(find.byKey(DocumentsTabKeys.view('doc-1')));
    await tester.pumpAndSettle();
    expect(find.byType(DocumentViewerScreen), findsOneWidget);
    final unresolved = find.byKey(DocumentViewerKeys.image, skipOffstage: false);
    final provider = tester.widget<Image>(unresolved).image;
    await tester.runAsync(() async {
      await precacheImage(provider, tester.element(unresolved));
    });
    await tester.pump();
    final image = tester.widget<Image>(find.byKey(DocumentViewerKeys.image));
    expect(image.image, isA<MemoryImage>());
  });

  testWidgets('prescriptions and invoices show empty and error states', (
    tester,
  ) async {
    await tester.pumpWidget(
      _tab(
        const PrescriptionsTab(),
        overrides: [
          prescriptionsProvider.overrideWith((ref) async => const <Prescription>[]),
        ],
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('No prescriptions have been issued yet.'), findsOneWidget);

    var attempts = 0;
    await tester.pumpWidget(
      _tab(
        const InvoicesTab(),
        overrides: [
          invoicesProvider.overrideWith((ref) async {
            attempts++;
            if (attempts == 1) throw StateError('down');
            return const <Invoice>[];
          }),
        ],
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Could not load invoices.'), findsOneWidget);
    await tester.tap(find.byKey(InvoicesTabKeys.retry));
    await tester.pumpAndSettle();
    expect(find.text('No invoices have been issued yet.'), findsOneWidget);
    expect(attempts, 2);
  });

  testWidgets('a PDF opens as a download, not an invented preview', (
    tester,
  ) async {
    await tester.pumpWidget(
      _tab(
        const DocumentViewerScreen(documentId: 'doc-pdf'),
        overrides: [
          documentFileProvider.overrideWith(
            (ref, id) async => ClinicalFile(
              bytes: Uint8List.fromList(const [37, 80, 68, 70]),
              contentType: 'application/pdf',
              fileName: 'document.pdf',
            ),
          ),
        ],
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(DocumentViewerKeys.fileNotice), findsOneWidget);
    expect(find.text('This PDF is available to download.'), findsOneWidget);
    expect(find.byKey(DocumentViewerKeys.image), findsNothing);
  });
}

Widget _hub(_FakeRecords repository, DocumentFileSaver saver) {
  return ProviderScope(
    overrides: [
      patientRecordsRepositoryProvider.overrideWithValue(repository),
      documentFileSaverProvider.overrideWithValue(saver),
      treatmentPlansProvider.overrideWith((ref) async => const <TreatmentPlan>[]),
      myAppointmentsProvider.overrideWith((ref) async => const <Appointment>[]),
      registrationSummaryProvider.overrideWith(
        (ref) async => RegistrationSummary(
          uhid: 'SAH-1',
          fullName: 'Meera Nair',
          phone: '0770000000',
          dateOfBirth: DateTime(1990, 1, 1),
          gender: 'Female',
          prakriti: 'Vata',
          vikriti: 'Pitta',
        ),
      ),
    ],
    child: MaterialApp(
      theme: AppTheme.light,
      locale: const Locale('en'),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: const HealthHubScreen(),
    ),
  );
}

Widget _tab(Widget home, {required List<Override> overrides}) {
  return ProviderScope(
    key: UniqueKey(),
    overrides: overrides,
    child: MaterialApp(
      theme: AppTheme.light,
      locale: const Locale('en'),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: home,
    ),
  );
}

class _FakeRecords implements PatientRecordsRepository {
  List<Prescription> prescriptions = const [];
  List<Invoice> invoices = const [];
  List<MedicalDocument> documents = const [];
  ClinicalFile? file;
  final downloadIds = <String>[];

  @override
  Future<ClinicalFile> downloadDocument(String id) async {
    downloadIds.add(id);
    return file!;
  }

  @override
  Future<List<MedicalDocument>> listDocuments() async => documents;

  @override
  Future<List<Invoice>> listInvoices() async => invoices;

  @override
  Future<List<Prescription>> listPrescriptions() async => prescriptions;
}

class _MemorySaver implements DocumentFileSaver {
  String? fileName;
  Uint8List? bytes;

  @override
  Future<String> save({required String fileName, required Uint8List bytes}) async {
    this.fileName = fileName;
    this.bytes = bytes;
    return 'saved-$fileName';
  }
}

const _png = <int>[
  137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0,
  0, 1, 8, 6, 0, 0, 0, 31, 21, 196, 137, 0, 0, 0, 11, 73, 68, 65, 84, 120, 156,
  99, 96, 0, 2, 0, 0, 5, 0, 1, 122, 94, 171, 63, 0, 0, 0, 0, 73, 69, 78, 68,
  174, 66, 96, 130,
];

Dio _dio(HttpClientAdapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'https://localhost:7443/api'));
  dio.httpClientAdapter = adapter;
  return dio;
}

typedef _Responder = Object Function(RequestOptions options);

class _ScriptedAdapter implements HttpClientAdapter {
  _ScriptedAdapter(this._respond, {this.bytesPaths = const {}});

  final _Responder _respond;
  final Set<String> bytesPaths;
  final calls = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    calls.add(options);
    final body = _respond(options);
    if (bytesPaths.contains(options.path)) {
      return ResponseBody.fromBytes(
        body as List<int>,
        200,
        headers: {
          Headers.contentTypeHeader: ['application/pdf'],
          'content-disposition': ['inline; filename="document.pdf"'],
        },
      );
    }
    return ResponseBody.fromString(
      body as String,
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
