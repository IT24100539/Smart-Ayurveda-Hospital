import 'dart:typed_data';

/// Records returned by the patient `mine` endpoints.
///
/// Missing lists stay empty. Nothing here invents a medicine, a payment, or a file.
class PrescriptionItem {
  const PrescriptionItem({
    required this.id,
    required this.name,
    required this.dosage,
    required this.frequency,
    required this.duration,
    required this.instructions,
  });

  final String id;
  final String name;
  final String dosage;
  final String frequency;
  final String duration;
  final String instructions;

  factory PrescriptionItem.fromJson(Map<String, dynamic> json) {
    return PrescriptionItem(
      id: (json['id'] ?? '').toString(),
      name: (json['name'] ?? '').toString(),
      dosage: (json['dosage'] ?? '').toString(),
      frequency: (json['frequency'] ?? '').toString(),
      duration: (json['duration'] ?? '').toString(),
      instructions: (json['instructions'] ?? '').toString().trim(),
    );
  }
}

class Prescription {
  const Prescription({
    required this.id,
    required this.doctorName,
    required this.status,
    required this.revisionNumber,
    required this.items,
    this.issuedAt,
    this.cancelledAt,
  });

  final String id;
  final String doctorName;
  final String status;
  final int revisionNumber;
  final DateTime? issuedAt;
  final DateTime? cancelledAt;
  final List<PrescriptionItem> items;

  factory Prescription.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return Prescription(
      id: (json['id'] ?? '').toString(),
      doctorName: (json['doctorName'] ?? '').toString(),
      status: (json['status'] ?? '').toString(),
      revisionNumber: json['revisionNumber'] as int? ?? 1,
      issuedAt: parseApiDate(json['issuedAt']),
      cancelledAt: parseApiDate(json['cancelledAt']),
      items: rawItems
          .map(
            (item) => PrescriptionItem.fromJson(
              Map<String, dynamic>.from(item as Map),
            ),
          )
          .toList(),
    );
  }
}

class InvoiceLine {
  const InvoiceLine({
    required this.id,
    required this.description,
    required this.quantity,
    required this.lineTotal,
  });

  final String id;
  final String description;
  final int quantity;
  final double lineTotal;

  factory InvoiceLine.fromJson(Map<String, dynamic> json) {
    return InvoiceLine(
      id: (json['id'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      quantity: _asInt(json['quantity']) ?? 0,
      lineTotal: _asDouble(json['lineTotal']) ?? 0,
    );
  }
}

class InvoicePayment {
  const InvoicePayment({
    required this.id,
    required this.amount,
    required this.method,
    required this.paidOn,
    this.reference,
  });

  final String id;
  final double amount;
  final String method;
  final DateTime? paidOn;
  final String? reference;

  factory InvoicePayment.fromJson(Map<String, dynamic> json) {
    return InvoicePayment(
      id: (json['id'] ?? '').toString(),
      amount: _asDouble(json['amount']) ?? 0,
      method: (json['method'] ?? '').toString(),
      paidOn: parseApiDate(json['paidOn']),
      reference: _blankToNull(json['reference']?.toString()),
    );
  }
}

class Invoice {
  const Invoice({
    required this.id,
    required this.invoiceNumber,
    required this.currency,
    required this.status,
    required this.total,
    required this.amountPaid,
    required this.balance,
    required this.lines,
    required this.payments,
    this.issuedAt,
    this.notes,
  });

  final String id;
  final String invoiceNumber;
  final String currency;
  final String status;
  final double total;
  final double amountPaid;
  final double balance;
  final DateTime? issuedAt;
  final String? notes;
  final List<InvoiceLine> lines;
  final List<InvoicePayment> payments;

  factory Invoice.fromJson(Map<String, dynamic> json) {
    final rawLines = json['lines'] as List<dynamic>? ?? const [];
    final rawPayments = json['payments'] as List<dynamic>? ?? const [];
    return Invoice(
      id: (json['id'] ?? '').toString(),
      invoiceNumber: (json['invoiceNumber'] ?? '').toString(),
      currency: (json['currency'] ?? '').toString(),
      status: (json['status'] ?? '').toString(),
      total: _asDouble(json['total']) ?? 0,
      amountPaid: _asDouble(json['amountPaid']) ?? 0,
      balance: _asDouble(json['balance']) ?? 0,
      issuedAt: parseApiDate(json['issuedAt']),
      notes: _blankToNull(json['notes']?.toString()),
      lines: rawLines
          .map(
            (line) => InvoiceLine.fromJson(Map<String, dynamic>.from(line as Map)),
          )
          .toList(),
      payments: rawPayments
          .map(
            (payment) =>
                InvoicePayment.fromJson(Map<String, dynamic>.from(payment as Map)),
          )
          .toList(),
    );
  }
}

class MedicalDocument {
  const MedicalDocument({
    required this.id,
    required this.title,
    required this.category,
    required this.contentType,
    required this.fileSizeBytes,
    required this.uploadedAt,
    required this.summary,
  });

  final String id;
  final String title;
  final String category;
  final String contentType;
  final int fileSizeBytes;
  final DateTime? uploadedAt;
  final String summary;

  bool get isImage => contentType.startsWith('image/');
  bool get isPdf => contentType == 'application/pdf';

  factory MedicalDocument.fromJson(Map<String, dynamic> json) {
    return MedicalDocument(
      id: (json['id'] ?? '').toString(),
      title: (json['title'] ?? '').toString(),
      category: (json['category'] ?? '').toString(),
      contentType: (json['contentType'] ?? '').toString(),
      fileSizeBytes: _asInt(json['fileSizeBytes']) ?? 0,
      uploadedAt: parseApiDate(json['uploadedAt']),
      summary: (json['summary'] ?? '').toString().trim(),
    );
  }
}

class ClinicalFile {
  const ClinicalFile({
    required this.bytes,
    required this.contentType,
    required this.fileName,
  });

  final Uint8List bytes;
  final String contentType;
  final String fileName;

  bool get isImage => contentType.startsWith('image/');
  bool get isPdf => contentType == 'application/pdf';
}

class RecordPage<T> {
  const RecordPage({
    required this.items,
    required this.totalCount,
  });

  final List<T> items;
  final int totalCount;

  factory RecordPage.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json) parse,
  ) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return RecordPage(
      items: rawItems
          .map((item) => parse(Map<String, dynamic>.from(item as Map)))
          .toList(),
      totalCount: json['totalCount'] as int? ?? rawItems.length,
    );
  }
}

DateTime? parseApiDate(Object? value) {
  if (value == null) return null;
  final text = value.toString().trim();
  if (text.isEmpty) return null;
  return DateTime.tryParse(text);
}

String formatMoney(String currency, double amount) {
  final code = currency.trim().isEmpty ? '' : '${currency.trim()} ';
  return '$code${amount.toStringAsFixed(2)}';
}

String formatFileSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

String? _blankToNull(String? value) {
  if (value == null) return null;
  final trimmed = value.trim();
  return trimmed.isEmpty ? null : trimmed;
}

double? _asDouble(Object? value) {
  if (value is num) return value.toDouble();
  return null;
}

int? _asInt(Object? value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return null;
}
