import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/certificate_bypass_io.dart';

void main() {
  test('accepts a dev certificate when the request host is the emulator', () {
    expect(allowDevCertificate('10.0.2.2', 'localhost'), isTrue);
    expect(allowDevCertificate('10.0.2.2', 'DESKTOP-LAB'), isTrue);
    expect(allowDevCertificate('192.168.1.20', 'localhost'), isTrue);
  });

  test('rejects a public host even when the certificate name matches', () {
    expect(allowDevCertificate('api.example.com', 'api.example.com'), isFalse);
    expect(allowDevCertificate('10.0.2.2', '   '), isFalse);
  });

  test('installs the debug bypass only for a local host', () {
    final local = Dio();
    configureDevCertificate(local, Uri.parse('https://10.0.2.2:7443/api'));
    expect(local.httpClientAdapter, isA<IOHttpClientAdapter>());
    expect(
      (local.httpClientAdapter as IOHttpClientAdapter).createHttpClient,
      isNotNull,
    );

    final publicHost = Dio();
    configureDevCertificate(publicHost, Uri.parse('https://api.example.com/api'));
    final adapter = publicHost.httpClientAdapter;
    if (adapter is IOHttpClientAdapter) {
      expect(adapter.createHttpClient, isNull);
    }
  });
}
