import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';

/// Configures self-signed dev certificate bypass ONLY in debug builds and ONLY for local/dev hosts.
void configureDevCertificate(Dio dio, Uri baseUri) {
  if (!kDebugMode) return;
  if (dio.httpClientAdapter is! IOHttpClientAdapter) return;

  final host = baseUri.host.toLowerCase();
  final isDevHost = host == 'localhost' ||
      host == '127.0.0.1' ||
      host == '10.0.2.2' ||
      host.startsWith('192.168.') ||
      host.startsWith('10.') ||
      host.startsWith('172.');

  if (!isDevHost) return;

  (dio.httpClientAdapter as IOHttpClientAdapter).createHttpClient = () {
    final client = HttpClient();
    client.badCertificateCallback =
        (X509Certificate cert, String certHost, int port) {
      final normalized = certHost.toLowerCase();
      return isDevHost &&
          (normalized == host ||
              normalized == 'localhost' ||
              normalized == '127.0.0.1' ||
              normalized == '10.0.2.2');
    };
    return client;
  };
}
