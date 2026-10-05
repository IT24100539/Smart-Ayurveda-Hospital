using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Hospital.Application.Doctors.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Doctors;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hospital.IntegrationTests;

public sealed class DoctorsIntegrationTests : IClassFixture<HospitalApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly byte[] TinyJpeg = [0xFF, 0xD8, 0xFF, 0xD9];

    private readonly HospitalApiFactory _factory;

    public DoctorsIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task DoctorEndpoints_Unauthenticated_Return401()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/doctors")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/doctors/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/doctors/{id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/doctors", SampleRequest("Unauth"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/api/doctors/{id}", SampleRequest("Unauth"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/doctors/{id}/deactivate", null)).StatusCode);
        using var photo = JpegContent(TinyJpeg);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/doctors/{id}/photo", photo)).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Patient)]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.FrontDeskStaff)]
    [InlineData(UserRole.Therapist)]
    public async Task ManageDoctors_OtherRoles_Return403(UserRole role)
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role);
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/doctors", SampleRequest(role.ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/doctors/{id}", SampleRequest(role.ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/doctors/{id}/deactivate", null)).StatusCode);
        using var photo = JpegContent(TinyJpeg);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/doctors/{id}/photo", photo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/doctors/{id}/photo")).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.FrontDeskStaff)]
    [InlineData(UserRole.Therapist)]
    public async Task ReadDoctors_OtherRoles_Return403(UserRole role)
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role);
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/doctors")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/doctors/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/doctors/{id}/photo")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateEditAndDeactivate_PatientReadsActiveOnly()
    {
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var patient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Patient);
        var name = UniqueName("Vd. Meera Joshi");

        var invalid = await admin.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest("", "Kayachikitsa", "BAMS", null));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var create = await admin.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest(
            name,
            "Kayachikitsa",
            "BAMS, MD (Kayachikitsa)",
            "Nadi pariksha and chikitsa for vata imbalance."));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdJson = await create.Content.ReadAsStringAsync();
        using (var createdDoc = JsonDocument.Parse(createdJson))
        {
            Assert.False(createdDoc.RootElement.TryGetProperty("rating", out _));
            Assert.False(createdDoc.RootElement.TryGetProperty("ratingCount", out _));
            Assert.False(createdDoc.RootElement.GetProperty("isSample").GetBoolean());
            Assert.False(createdDoc.RootElement.GetProperty("hasPhoto").GetBoolean());
        }

        var created = JsonSerializer.Deserialize<DoctorDto>(createdJson, JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Kayachikitsa", created.Specialty);
        Assert.True(created.IsActive);

        var editedName = name + " edited";
        var update = await admin.PutAsJsonAsync($"/api/doctors/{created.Id}", new UpdateDoctorRequest(
            editedName,
            "Panchakarma",
            "BAMS",
            "   "));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(editedName, updated.Name);
        Assert.Equal("Panchakarma", updated.Specialty);
        Assert.Null(updated.Bio);

        var patientBefore = await patient.GetFromJsonAsync<DoctorPage>($"/api/doctors?query={Uri.EscapeDataString(editedName)}", JsonOptions);
        Assert.NotNull(patientBefore);
        Assert.Contains(patientBefore.Items, item => item.Id == created.Id && item.IsActive);

        var deactivate = await admin.PostAsync($"/api/doctors/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        var inactive = await deactivate.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        Assert.NotNull(inactive);
        Assert.False(inactive.IsActive);

        var patientList = await patient.GetAsync($"/api/doctors?query={Uri.EscapeDataString(editedName)}&activeOnly=false");
        Assert.Equal(HttpStatusCode.OK, patientList.StatusCode);
        var hidden = await patientList.Content.ReadFromJsonAsync<DoctorPage>(JsonOptions);
        Assert.NotNull(hidden);
        Assert.DoesNotContain(hidden.Items, item => item.Id == created.Id);

        var patientGet = await patient.GetAsync($"/api/doctors/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, patientGet.StatusCode);

        var adminGet = await admin.GetAsync($"/api/doctors/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, adminGet.StatusCode);
    }

    [Fact]
    public async Task Rating_IsOmittedUntilVisibleFeedbackOnCompletedAppointment()
    {
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var patient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Patient);
        var name = UniqueName("Vd. Nadi Sharma");
        var create = await admin.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest(name, "Nadi pariksha", "BAMS", null));
        var doctor = await create.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        Assert.NotNull(doctor);

        var before = await patient.GetAsync($"/api/doctors/{doctor.Id}");
        using (var beforeDoc = JsonDocument.Parse(await before.Content.ReadAsStringAsync()))
        {
            Assert.False(beforeDoc.RootElement.TryGetProperty("rating", out _));
            Assert.False(beforeDoc.RootElement.TryGetProperty("ratingCount", out _));
        }

        await SeedFeedbackAsync(doctor.Id);

        var after = await patient.GetAsync($"/api/doctors/{doctor.Id}");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        using var afterDoc = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.Equal(4m, afterDoc.RootElement.GetProperty("rating").GetDecimal());
        Assert.Equal(1, afterDoc.RootElement.GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task Photo_IsStoredOutsideWebRoot_AndRequiresAuthorization()
    {
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var patient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Patient);
        var other = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var name = UniqueName("Vd. Portrait");
        var create = await admin.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest(name, "Shalya Tantra", "BAMS", null));
        var doctor = await create.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        Assert.NotNull(doctor);

        using var upload = JpegContent(TinyJpeg);
        var uploaded = await admin.PostAsync($"/api/doctors/{doctor.Id}/photo", upload);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        using var uploadedDoc = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        Assert.True(uploadedDoc.RootElement.GetProperty("hasPhoto").GetBoolean());
        Assert.Equal($"/api/doctors/{doctor.Id}/photo", uploadedDoc.RootElement.GetProperty("photoUrl").GetString());
        Assert.False(uploadedDoc.RootElement.TryGetProperty("photoStorageKey", out _));

        using (var scope = _factory.Services.CreateScope())
        {
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DoctorPhotoOptions>>().Value;
            var webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
                ? Path.Combine(env.ContentRootPath, "wwwroot")
                : env.WebRootPath;
            Assert.False(DoctorPhotoPaths.IsInsideWebRoot(options.StoragePath, webRoot));
            var saved = Directory.GetFiles(options.StoragePath, $"{doctor.Id:N}.*");
            Assert.Single(saved);
            Assert.False(DoctorPhotoPaths.IsInsideWebRoot(saved[0], webRoot));
            Assert.Equal(TinyJpeg, await File.ReadAllBytesAsync(saved[0]));
        }

        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/doctors/{doctor.Id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/doctors/{doctor.Id}/photo")).StatusCode);

        var patientPhoto = await patient.GetAsync($"/api/doctors/{doctor.Id}/photo");
        Assert.Equal(HttpStatusCode.OK, patientPhoto.StatusCode);
        Assert.Equal("image/jpeg", patientPhoto.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyJpeg, await patientPhoto.Content.ReadAsByteArrayAsync());
        Assert.NotNull(patientPhoto.Headers.CacheControl);
        Assert.True(patientPhoto.Headers.CacheControl.Private);
        Assert.True(patientPhoto.Headers.CacheControl.NoStore);

        var deactivate = await admin.PostAsync($"/api/doctors/{doctor.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await patient.GetAsync($"/api/doctors/{doctor.Id}/photo")).StatusCode);
        var adminPhoto = await admin.GetAsync($"/api/doctors/{doctor.Id}/photo");
        Assert.Equal(HttpStatusCode.OK, adminPhoto.StatusCode);
        Assert.Equal(TinyJpeg, await adminPhoto.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Photo_RejectsWrongTypeAndOversizedFile()
    {
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var create = await admin.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest(UniqueName("Vd. File"), "Kayachikitsa", "BAMS", null));
        var doctor = await create.Content.ReadFromJsonAsync<DoctorDto>(JsonOptions);
        Assert.NotNull(doctor);

        using var text = JpegContent(Encoding.UTF8.GetBytes("not an image"));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/doctors/{doctor.Id}/photo", text)).StatusCode);

        var oversized = new byte[DoctorPhotoOptions.DefaultMaxBytes + 1];
        oversized[0] = 0xFF;
        oversized[1] = 0xD8;
        oversized[2] = 0xFF;
        using var tooBig = JpegContent(oversized);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/doctors/{doctor.Id}/photo", tooBig)).StatusCode);
    }

    private async Task SeedFeedbackAsync(Guid doctorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var treatment = new Treatment
        {
            Name = $"Nadi pariksha {suffix}",
            NameSinhala = "නාඩි",
            Description = "Pulse examination",
            DescriptionSinhala = "නාඩි පරීක්ෂාව",
            Category = TreatmentCategory.Consultation,
            DurationMinutes = 30,
            UnitPrice = 1500,
            IsActive = true
        };
        var chart = new Patient
        {
            Uhid = $"SAH-DOC-{suffix}",
            FirstName = "Meera",
            LastName = "Nair",
            DateOfBirth = new DateOnly(1990, 6, 15),
            Gender = Gender.Female,
            Phone = $"98{suffix}",
            Prakriti = DoshaType.Vata,
            Vikriti = DoshaType.Pitta
        };
        db.Treatments.Add(treatment);
        db.Patients.Add(chart);
        db.Appointments.AddRange(
            Visit(chart, treatment, doctorId, new DateOnly(2026, 8, 1), AppointmentStatus.Completed),
            Visit(chart, treatment, doctorId, new DateOnly(2026, 8, 2), AppointmentStatus.Completed),
            Visit(chart, treatment, doctorId, new DateOnly(2026, 8, 3), AppointmentStatus.Approved));
        await db.SaveChangesAsync();

        var visits = db.Appointments.Where(x => x.DoctorId == doctorId).ToList();
        var completed = visits.Where(x => x.Status == AppointmentStatus.Completed).OrderBy(x => x.RequestedDate).ToList();
        var approved = visits.Single(x => x.Status == AppointmentStatus.Approved);
        db.Feedbacks.AddRange(
            Review(chart, completed[0], 4, FeedbackStatus.Visible),
            Review(chart, completed[1], 1, FeedbackStatus.Hidden),
            Review(chart, approved, 2, FeedbackStatus.Visible));
        await db.SaveChangesAsync();
    }

    private static Appointment Visit(Patient chart, Treatment treatment, Guid doctorId, DateOnly date, AppointmentStatus status) =>
        new()
        {
            Patient = chart,
            Treatment = treatment,
            DoctorId = doctorId,
            RequestedDate = date,
            RequestedTimeSlot = "09:00",
            Status = status
        };

    private static Feedback Review(Patient chart, Appointment visit, int rating, FeedbackStatus status) =>
        new()
        {
            Patient = chart,
            Appointment = visit,
            Rating = rating,
            Comment = "The nadi pariksha was careful and clear.",
            PatientNameSnapshot = "Meera Nair",
            Status = status
        };

    private static CreateDoctorRequest SampleRequest(string name) =>
        new(UniqueName(name), "Kayachikitsa", "BAMS", null);

    private static string UniqueName(string name) => $"{name} {Guid.NewGuid():N}";

    private static MultipartFormDataContent JpegContent(byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "photo", "portrait.jpg");
        return content;
    }

    private sealed record DoctorPage(IReadOnlyList<DoctorDto> Items, int TotalCount, int Page, int PageSize);
}

public sealed class DoctorSeedTests
{
    [Fact]
    public async Task ProductionSeed_DoesNotInsertDoctors()
    {
        await using var db = NewDb();
        try
        {
            await DbSeeder.SeedAsync(db, new SilentLogger(), isDevelopment: false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sequence contains no elements", StringComparison.Ordinal))
        {
            // Catalog seed still looks up a development doctor login. Physician directory rows are not part of that path.
        }

        Assert.Equal(0, await db.Doctors.CountAsync());
    }

    [Fact]
    public async Task DevelopmentSeed_LabelsSampleDoctors()
    {
        await using var db = NewDb();
        await DbSeeder.SeedAsync(db, new SilentLogger(), isDevelopment: true);
        var doctors = await db.Doctors.ToListAsync();
        Assert.NotEmpty(doctors);
        Assert.All(doctors, doctor =>
        {
            Assert.True(doctor.IsSample);
            Assert.StartsWith(Doctor.SampleNamePrefix, doctor.Name);
        });
    }

    private static HospitalDbContext NewDb() =>
        new(new DbContextOptionsBuilder<HospitalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class SilentLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}

public sealed class DoctorPhotoPathTests
{
    [Fact]
    public void DefaultStorage_IsOutsideWebRoot()
    {
        var content = Path.Combine(Path.GetTempPath(), "sah-content");
        var web = Path.Combine(content, "wwwroot");
        var options = new DoctorPhotoOptions();
        DoctorPhotoPaths.Apply(options, content, web);
        Assert.False(DoctorPhotoPaths.IsInsideWebRoot(options.StoragePath, web));
        Assert.EndsWith(Path.Combine("data", "doctor-photos"), options.StoragePath);
    }

    [Fact]
    public void StorageInsideWebRoot_IsRejected()
    {
        var content = Path.Combine(Path.GetTempPath(), "sah-content");
        var web = Path.Combine(content, "wwwroot");
        var inside = new DoctorPhotoOptions { StoragePath = Path.Combine(web, "photos") };
        var relative = new DoctorPhotoOptions { StoragePath = Path.Combine("wwwroot", "photos") };

        var absolute = Assert.Throws<InvalidOperationException>(() => DoctorPhotoPaths.Apply(inside, content, web));
        var fromContent = Assert.Throws<InvalidOperationException>(() => DoctorPhotoPaths.Apply(relative, content, web));
        Assert.Contains("outside the web root", absolute.Message);
        Assert.Contains("outside the web root", fromContent.Message);
    }
}
