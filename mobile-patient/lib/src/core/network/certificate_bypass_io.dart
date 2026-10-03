import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';

/// True for loopback, the Android emulator alias, and private LAN addresses.
bool isLocalDevHost(String host) {
  final normalized = host.toLowerCase();
  if (normalized == 'localhost' ||
      normalized == '127.0.0.1' ||
      normalized == '::1' ||
      normalized == '10.0.2.2' ||
      normalized == '10.0.3.2') {
    return true;
  }
  if (normalized.startsWith('192.168.') || normalized.startsWith('10.')) {
    return true;
  }
  final private172 = RegExp(r'^172\.(\d{1,3})\.').firstMatch(normalized);
  if (private172 == null) return false;
  final second = int.tryParse(private172.group(1)!);
  return second != null && second >= 16 && second <= 31;
}

/// Debug-only decision for a self-signed development certificate.
///
/// The handshake host is often the certificate subject (`localhost`) or the
/// machine name, while the app is calling `10.0.2.2` or a LAN address. Accept
/// the certificate when the request URL itself is a local development host.
bool allowDevCertificate(String requestHost, String certificateHost) {
  if (certificateHost.trim().isEmpty) return false;
  return isLocalDevHost(requestHost);
}

/// Configures self-signed dev certificate bypass ONLY in debug builds and ONLY for local/dev hosts.
void configureDevCertificate(Dio dio, Uri baseUri) {
  if (!kDebugMode) return;
  if (!isLocalDevHost(baseUri.host)) return;

  final adapter = dio.httpClientAdapter is IOHttpClientAdapter
      ? dio.httpClientAdapter as IOHttpClientAdapter
      : IOHttpClientAdapter();
  dio.httpClientAdapter = adapter;
  adapter.createHttpClient = () {
    final client = HttpClient();
    client.badCertificateCallback =
        (X509Certificate cert, String certHost, int port) =>
            allowDevCertificate(baseUri.host, certHost);
    return client;
  };
}
