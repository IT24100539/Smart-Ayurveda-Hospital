using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;
using Hospital.Infrastructure.Documents;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hospital.IntegrationTests;

public sealed class MedicalDocumentsIntegrationTests : IClassFixture<HospitalApiFactory>
{
    private static readonly byte[] TinyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n");
    private static readonly byte[] TinyJpeg = [0xFF, 0xD8, 0xFF, 0xD9];

    private readonly HospitalApiFactory _factory;

    public MedicalDocumentsIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_StoresGeneratedNameOutsideWebRoot_AndIgnoresTraversalFileName()
    {
        var staff = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.FrontDeskStaff);
        var patient = CreatePatient();
        var anonymous = _factory.CreateClient();
        var traversalName = "..\\..\\wwwroot\\evil.pdf";
        string storagePath;
        using (var pathScope = _factory.Services.CreateScope())
        {
            storagePath = pathScope.ServiceProvider.GetRequiredService<IOptions<MedicalDocumentOptions>>().Value.StoragePath;
        }

        var before = Directory.Exists(storagePath)
            ? Directory.GetFiles(storagePath).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/medical-documents", EmptyForm())).StatusCode);
        using (var patientUpload = DocumentContent(TinyPdf, "application/pdf", "lab.pdf", patient.PatientId, "Prakriti report"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await patient.Client.PostAsync("/api/medical-documents", patientUpload)).StatusCode);
        }

        string body;
        using (var upload = DocumentContent(TinyPdf, "application/pdf", traversalName, patient.PatientId, "Nadi report"))
        {
            var uploaded = await staff.PostAsync("/api/medical-documents", upload);
            body = await uploaded.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        }

        using var uploadedDoc = JsonDocument.Parse(body);
        var root = uploadedDoc.RootElement;
        var id = root.GetProperty("id").GetGuid();
        Assert.Equal($"/api/medical-documents/{id}/file", root.GetProperty("fileUrl").GetString());
        Assert.False(root.TryGetProperty("filePath", out _));
        Assert.False(root.TryGetProperty("storageKey", out _));
        Assert.DoesNotContain("evil", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wwwroot", body, StringComparison.OrdinalIgnoreCase);

        using var scope = _factory.Services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MedicalDocumentOptions>>().Value;
        var scanner = scope.ServiceProvider.GetRequiredService<IAntivirusScanner>();
        var webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
        Assert.IsType<NoOpAntivirusScanner>(scanner);
        Assert.False(MedicalDocumentPaths.IsInsideWebRoot(options.StoragePath, webRoot));
        var saved = Directory.GetFiles(options.StoragePath).Single(path => !before.Contains(path));
        Assert.Matches(new Regex("^[a-f0-9]{32}\\.pdf$"), Path.GetFileName(saved));
        Assert.DoesNotContain("evil", Path.GetFileName(saved), StringComparison.OrdinalIgnoreCase);
        Assert.False(MedicalDocumentPaths.IsInsideWebRoot(saved, webRoot));
        Assert.False(File.Exists(Path.Combine(webRoot, "evil.pdf")));

        var download = await patient.Client.GetAsync($"/api/medical-documents/{id}/file");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyPdf, await download.Content.ReadAsByteArrayAsync());
        Assert.NotNull(download.Headers.CacheControl);
        Assert.True(download.Headers.CacheControl!.Private);
        Assert.True(download.Headers.CacheControl.NoStore);
        var disposition = download.Content.Headers.ContentDisposition?.ToString() ?? string.Empty;
        Assert.Contains("document.pdf", disposition, StringComparison.Ordinal);
        Assert.DoesNotContain("evil", disposition, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("..", disposition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upload_RejectsDisallowedTypeAndMismatchedContents()
    {
        var staff = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var patient = CreatePatient();

        using var text = DocumentContent(Encoding.UTF8.GetBytes("not a chart"), "application/pdf", "notes.pdf", patient.PatientId, "Notes");
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsync("/api/medical-documents", text)).StatusCode);

        using var mismatched = DocumentContent(TinyJpeg, "application/pdf", "scan.pdf", patient.PatientId, "Scan");
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsync("/api/medical-documents", mismatched)).StatusCode);

        using var svg = DocumentContent(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"), "image/svg+xml", "mark.svg", patient.PatientId, "Mark");
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsync("/api/medical-documents", svg)).StatusCode);

        using var jpeg = DocumentContent(TinyJpeg, "image/jpeg", "..\\..\\portrait.jpg", patient.PatientId, "Tongue photo");
        var uploaded = await staff.PostAsync("/api/medical-documents", jpeg);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
    }

    [Fact]
    public async Task Patients_ReadOnlyTheirOwn_AndTraversalKeysAreNotServed()
    {
        var staff = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Therapist);
        var owner = CreatePatient();
        var other = CreatePatient();
        var anonymous = _factory.CreateClient();

        using var upload = DocumentContent(TinyPdf, "application/pdf", "lab.pdf", owner.PatientId, "Lab report");
        var uploaded = await staff.PostAsync("/api/medical-documents", upload);
        var uploadedBody = await uploaded.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var uploadedDoc = JsonDocument.Parse(uploadedBody);
        var id = uploadedDoc.RootElement.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/medical-documents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/medical-documents/{id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.GetAsync($"/api/medical-documents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.GetAsync($"/api/medical-documents/{id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.GetAsync($"/api/medical-documents?patientId={owner.PatientId}")).StatusCode);

        var ownerList = await owner.Client.GetAsync("/api/medical-documents/mine");
        var otherList = await other.Client.GetAsync("/api/medical-documents/mine");
        Assert.Equal(HttpStatusCode.OK, ownerList.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherList.StatusCode);
        Assert.Contains(id.ToString(), await ownerList.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(id.ToString(), await otherList.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var staffRead = await staff.GetAsync($"/api/medical-documents/{id}/file");
        Assert.Equal(HttpStatusCode.OK, staffRead.StatusCode);
        Assert.Equal(TinyPdf, await staffRead.Content.ReadAsByteArrayAsync());

        var marker = Guid.NewGuid().ToString("N");
        var outsideName = marker + ".pdf";
        string outsidePath;
        using (var scope = _factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<MedicalDocumentOptions>>().Value;
            var parent = Directory.GetParent(options.StoragePath)!.FullName;
            outsidePath = Path.Combine(parent, outsideName);
            await File.WriteAllBytesAsync(outsidePath, Encoding.UTF8.GetBytes("OUTSIDE-SECRET-" + marker));
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            db.MedicalDocuments.Add(new MedicalDocument
            {
                PatientId = owner.PatientId,
                Title = "Outside",
                Category = DocumentCategory.General,
                FilePath = "../" + outsideName,
                ContentType = "application/pdf",
                FileSizeBytes = 8,
                UploadedByUserId = Guid.NewGuid(),
                Summary = string.Empty
            });
            await db.SaveChangesAsync();
        }

        try
        {
            var escaped = await owner.Client.GetAsync("/api/medical-documents/mine");
            var escapedBody = await escaped.Content.ReadAsStringAsync();
            using var escapedDoc = JsonDocument.Parse(escapedBody);
            var poisonedId = escapedDoc.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid())
                .Single(itemId => itemId != id);
            var poisoned = await owner.Client.GetAsync($"/api/medical-documents/{poisonedId}/file");
            var poisonedBody = await poisoned.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.NotFound, poisoned.StatusCode);
            Assert.DoesNotContain("OUTSIDE-SECRET", poisonedBody, StringComparison.Ordinal);
            Assert.True(File.Exists(outsidePath));
        }
        finally
        {
            if (File.Exists(outsidePath))
            {
                File.Delete(outsidePath);
            }
        }
    }

    private (HttpClient Client, Guid PatientId) CreatePatient()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId, UserRole.Patient);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var user = db.Users.Single(x => x.Id == userId);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var patient = new Patient
        {
            Uhid = $"SAH-MD-{suffix}",
            FirstName = "Meera",
            LastName = "Nair",
            DateOfBirth = new DateOnly(1992, 4, 12),
            Gender = Gender.Female,
            Phone = $"97{suffix}",
            Email = user.Email,
            Prakriti = DoshaType.Vata,
            Vikriti = DoshaType.Pitta
        };
        db.Patients.Add(patient);
        db.SaveChanges();
        return (client, patient.Id);
    }

    private static MultipartFormDataContent DocumentContent(
        byte[] bytes,
        string contentType,
        string fileName,
        Guid patientId,
        string title)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        content.Add(new StringContent(patientId.ToString()), "patientId");
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(nameof(DocumentCategory.LabReport)), "category");
        return content;
    }

    private static MultipartFormDataContent EmptyForm()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("x"), "title");
        return content;
    }
}

public sealed class SmallMedicalDocumentApiFactory : HospitalApiFactory
{
    public SmallMedicalDocumentApiFactory()
        : base(new Dictionary<string, string?>
        {
            ["MedicalDocuments:MaxBytes"] = "2048",
            ["MedicalDocuments:StoragePath"] = Path.Combine(Path.GetTempPath(), "smart-ayurveda-hospital-tests", "medical-documents-small"),
        })
    {
    }
}

public sealed class MedicalDocumentSizeTests : IClassFixture<SmallMedicalDocumentApiFactory>
{
    private readonly SmallMedicalDocumentApiFactory _factory;

    public MedicalDocumentSizeTests(SmallMedicalDocumentApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_RejectsFileLargerThanConfiguredLimit()
    {
        var staff = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var userId = Guid.NewGuid();
        _ = _factory.CreateAuthenticatedClient(userId, UserRole.Patient);
        Guid patientId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var user = db.Users.Single(x => x.Id == userId);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var patient = new Patient
            {
                Uhid = $"SAH-SZ-{suffix}",
                FirstName = "Anita",
                LastName = "Deshmukh",
                DateOfBirth = new DateOnly(1988, 1, 3),
                Gender = Gender.Female,
                Phone = $"96{suffix}",
                Email = user.Email,
                Prakriti = DoshaType.Kapha,
                Vikriti = DoshaType.Vata
            };
            db.Patients.Add(patient);
            db.SaveChanges();
            patientId = patient.Id;
        }

        var accepted = new byte[2048];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(accepted, 0);
        using (var ok = Form(accepted, patientId))
        {
            Assert.Equal(HttpStatusCode.Created, (await staff.PostAsync("/api/medical-documents", ok)).StatusCode);
        }

        var oversized = new byte[2049];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(oversized, 0);
        using var tooBig = Form(oversized, patientId);
        var rejected = await staff.PostAsync("/api/medical-documents", tooBig);
        var body = await rejected.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.DoesNotContain("medical-documents", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("..", body, StringComparison.Ordinal);
    }

    private static MultipartFormDataContent Form(byte[] bytes, Guid patientId)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "report.pdf");
        content.Add(new StringContent(patientId.ToString()), "patientId");
        content.Add(new StringContent("Oversized report"), "title");
        content.Add(new StringContent(nameof(DocumentCategory.DiagnosticScan)), "category");
        return content;
    }
}

public sealed class MedicalDocumentStoreTests
{
    [Fact]
    public async Task Save_RejectsTypeAndSize_AndGeneratedNameCannotEscape()
    {
        var root = NewRoot();
        try
        {
            var store = Store(root, 2048, new NoOpAntivirusScanner());
            await Assert.ThrowsAsync<DomainException>(() =>
                store.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("hello")), "application/pdf", CancellationToken.None));

            var jpegAsPdf = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
            await Assert.ThrowsAsync<DomainException>(() =>
                store.SaveAsync(jpegAsPdf, "application/pdf", CancellationToken.None));

            var oversized = new byte[2049];
            Encoding.ASCII.GetBytes("%PDF-").CopyTo(oversized, 0);
            await Assert.ThrowsAsync<DomainException>(() =>
                store.SaveAsync(new MemoryStream(oversized), "application/pdf", CancellationToken.None));
            Assert.False(Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any());

            var saved = await store.SaveAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4")), "application/pdf", CancellationToken.None);
            Assert.Matches(new Regex("^[a-f0-9]{32}\\.pdf$"), saved.StorageKey);
            await using var opened = store.OpenRead(saved.StorageKey);
            Assert.NotNull(opened);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task OpenReadAndDelete_IgnoreTraversalKeys()
    {
        var root = NewRoot();
        var parent = Directory.GetParent(root)!.FullName;
        var outsideName = Guid.NewGuid().ToString("N") + ".pdf";
        var outside = Path.Combine(parent, outsideName);
        await File.WriteAllBytesAsync(outside, Encoding.UTF8.GetBytes("OUTSIDE-SECRET"));
        try
        {
            var store = Store(root, 2048, new NoOpAntivirusScanner());
            Assert.Null(store.OpenRead("../" + outsideName));
            Assert.Null(store.OpenRead("..\\" + outsideName));
            Assert.Null(store.OpenRead(outside));
            Assert.Null(store.OpenRead("..\\..\\wwwroot\\evil.pdf"));
            Assert.Null(store.OpenRead("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.pdf/../../" + outsideName));
            await store.DeleteAsync("../" + outsideName, CancellationToken.None);
            await store.DeleteAsync(outside, CancellationToken.None);
            Assert.Equal("OUTSIDE-SECRET", await File.ReadAllTextAsync(outside));
        }
        finally
        {
            if (File.Exists(outside))
            {
                File.Delete(outside);
            }

            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task AntivirusHook_RunsBeforeWrite_AndRejectionLeavesNoFile()
    {
        var root = NewRoot();
        try
        {
            var rejecting = Store(root, 4096, new RejectingScanner());
            var error = await Assert.ThrowsAsync<DomainException>(() =>
                rejecting.SaveAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4")), "application/pdf", CancellationToken.None));
            Assert.Contains("antivirus", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any());

            var recording = new RecordingScanner();
            var store = Store(root, 4096, recording);
            var saved = await store.SaveAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4")), "application/pdf", CancellationToken.None);
            Assert.Equal(1, recording.Calls);
            Assert.Equal("application/pdf", recording.ContentType);
            Assert.True(File.Exists(Path.Combine(root, saved.StorageKey)));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static MedicalDocumentStore Store(string root, int maxBytes, IAntivirusScanner scanner) =>
        new(Options.Create(new MedicalDocumentOptions { StoragePath = root, MaxBytes = maxBytes }), scanner);

    private static string NewRoot() =>
        Path.Combine(Path.GetTempPath(), "sah-doc-" + Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RejectingScanner : IAntivirusScanner
    {
        public Task ScanAsync(ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken) =>
            throw new DomainException("The file was rejected by the antivirus scan.");
    }

    private sealed class RecordingScanner : IAntivirusScanner
    {
        public int Calls { get; private set; }
        public string? ContentType { get; private set; }

        public Task ScanAsync(ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken)
        {
            Calls++;
            ContentType = contentType;
            Assert.True(content.Length > 0);
            return Task.CompletedTask;
        }
    }
}

public sealed class MedicalDocumentPathTests
{
    [Fact]
    public void DefaultStorage_IsOutsideWebRoot()
    {
        var content = Path.Combine(Path.GetTempPath(), "sah-content");
        var web = Path.Combine(content, "wwwroot");
        var options = new MedicalDocumentOptions();
        MedicalDocumentPaths.Apply(options, content, web);
        Assert.False(MedicalDocumentPaths.IsInsideWebRoot(options.StoragePath, web));
        Assert.EndsWith(Path.Combine("data", "medical-documents"), options.StoragePath);
        Assert.Equal(MedicalDocumentOptions.DefaultMaxBytes, options.MaxBytes);
    }

    [Fact]
    public void StorageInsideWebRoot_IsRejected()
    {
        var content = Path.Combine(Path.GetTempPath(), "sah-content");
        var web = Path.Combine(content, "wwwroot");
        var inside = new MedicalDocumentOptions { StoragePath = Path.Combine(web, "charts") };
        var relative = new MedicalDocumentOptions { StoragePath = Path.Combine("wwwroot", "charts") };

        var absolute = Assert.Throws<InvalidOperationException>(() => MedicalDocumentPaths.Apply(inside, content, web));
        var fromContent = Assert.Throws<InvalidOperationException>(() => MedicalDocumentPaths.Apply(relative, content, web));
        Assert.Contains("outside the web root", absolute.Message);
        Assert.Contains("outside the web root", fromContent.Message);
    }

    [Fact]
    public void MaxBytes_UsesConfiguredValueInsideTheAllowedRange()
    {
        var content = Path.Combine(Path.GetTempPath(), "sah-content");
        var web = Path.Combine(content, "wwwroot");
        var options = new MedicalDocumentOptions { MaxBytes = 2048 };
        MedicalDocumentPaths.Apply(options, content, web);
        Assert.Equal(2048, options.MaxBytes);

        var huge = new MedicalDocumentOptions { MaxBytes = MedicalDocumentOptions.AbsoluteMaxBytes + 1 };
        MedicalDocumentPaths.Apply(huge, content, web);
        Assert.Equal(MedicalDocumentOptions.DefaultMaxBytes, huge.MaxBytes);
    }
}
