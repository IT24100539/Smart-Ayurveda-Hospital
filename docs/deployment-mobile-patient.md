# Patient app release APK

The patient app talks only to `Hospital.Api`. Set the deployed API when building:

```bash
cd mobile-patient
flutter build apk --release --dart-define=API_BASE_URL=https://<deployed-backend>/api
```

`API_BASE_URL` must include `/api` and must not have a trailing slash.

The release APK is written to:

```text
mobile-patient/build/app/outputs/flutter-apk/app-release.apk
```

Install that file on a device. The phone must be able to reach the host in `API_BASE_URL`.
