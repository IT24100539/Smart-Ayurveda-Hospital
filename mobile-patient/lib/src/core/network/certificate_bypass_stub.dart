import 'package:dio/dio.dart';

/// Web stub: certificate validation is completely delegated to the browser.
void configureDevCertificate(Dio dio, Uri baseUri) {
  // Browsers enforce certificate trust; self-signed dev certs must be accepted in the browser.
}

/// Web stub: the browser decides certificate trust, so never bypass it here.
bool allowDevCertificate(String requestHost, String certificateHost) => false;