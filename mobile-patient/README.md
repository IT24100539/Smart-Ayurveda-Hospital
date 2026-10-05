# Smart Ayurveda Hospital - Patient Mobile App

Flutter mobile and web application for patients of Smart Ayurveda Hospital.

## Network Configuration & Dev Environments

The app connects to the .NET backend API (`Hospital.Api`) with configurable base URLs:

### Default Platform Base URLs:
- **Web & Desktop / macOS / Windows / Linux**: `https://localhost:7443/api`
- **Android Emulator**: `https://10.0.2.2:7443/api` (Android's host loopback alias)

### Override via `--dart-define=API_BASE_URL` (e.g. for Physical Devices):
To run on a physical phone or external device connected over Wi-Fi / LAN:
```bash
# HTTPS with dev certificate bypass:
flutter run --dart-define=API_BASE_URL=https://192.168.1.100:7443/api

# Or plain HTTP:
flutter run --dart-define=API_BASE_URL=http://192.168.1.100:5080/api
```

> **Note on Physical Devices & Backend Binding**:
> By default, the backend local development server binds to `127.0.0.1` (`localhost`). To reach it from a physical phone on your local Wi-Fi network, the backend must either be launched binding to `0.0.0.0` / LAN IP (e.g., via `dotnet run --urls "https://0.0.0.0:7443;http://0.0.0.0:5080"`), or accessed via port forwarding / reverse proxy.

### Self-Signed Certificate Handling:
- In debug mode (`kDebugMode`), self-signed development certificates are automatically accepted for local development hosts (`localhost`, `127.0.0.1`, `10.0.2.2`, or configured LAN IPs).
- On Flutter Web, certificate trust is managed directly by the web browser (browsers must trust the development certificate).
- Release builds strictly enforce standard platform certificate validation.

## Testing Health Hub with Seeded Patient Data

To test My Health Hub against the live backend with real patient appointments:
1. Ensure the backend API is running (`dotnet run --project backend/src/Hospital.Api`).
2. Launch the Flutter app (`flutter run`).
3. Log in with the pre-seeded patient account (**Local Development Seed Data Only** - from `DbSeeder.cs`):
   - **Email**: `meera.nair@example.local`
   - **Password**: `ChangeMe!Patient1`
   *(Note: These credentials exist solely for local development and integration testing within `DbSeeder.cs`.)*
4. Tap the **Profile** tab in the bottom navigation bar and select **My Health Hub** (or navigate to `/profile/health-hub`).
5. Verify both sections:
   - **My therapy sessions**: Displays real completed and scheduled sessions (e.g. Abhyanga treatment history, session counts, time slots, and status chips).
   - **My registration summary**: Displays authentic patient profile data (UHID `SAH-2026-00007`, Name `Meera Nair`, Phone `9876500001`, Date of Birth, Prakriti, and Vikriti).

