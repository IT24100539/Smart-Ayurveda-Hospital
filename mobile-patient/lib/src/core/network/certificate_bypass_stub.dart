import 'package:dio/dio.dart';

/// Web stub: certificate validation is completely delegated to the browser.
void configureDevCertificate(Dio dio, Uri baseUri) {
  // Browsers enforce certificate trust; self-signed dev certs must be accepted in the browser.
}
