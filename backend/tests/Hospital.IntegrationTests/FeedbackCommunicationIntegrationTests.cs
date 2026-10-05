using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class FeedbackCommunicationIntegrationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static int _slot;

    private readonly HospitalApiFactory _factory;

    public FeedbackCommunicationIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SubmitFeedback_StaffApprovesAiDraft_PatientReceivesReplyNotification()
    {
        var agentCallsBefore = _factory.Agent.Calls;
        var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
        var treatmentId = await FirstTreatmentIdAsync(admin);

        var email = $"patient-{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";
        const string fullName = "Devika Nambiar";
        await RegisterPatientAsync(email, password, fullName, $"91{Random.Shared.Next(10000000, 99999999)}");
        var patientId = await CreatePatientRecordAsync(admin, email, "Devika", "Nambiar");
        var appointmentId = await CreateCompletedAppointmentAsync(admin, patientId, treatmentId);

        var patient = await LoginAsync(email, password);
        const string comment = "The completed nadi pariksha explained my vata imbalance clearly.";
        var feedback = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
        {
            appointmentId,
            rating = 5,
            comment,
            isAnonymous = false
        }, HttpStatusCode.Created);

        feedback.Status.Should().Be("PendingModeration");
        feedback.Comment.Should().Be(comment);

        var doctor = await LoginAsync("doctor@smartayurveda.local", "ChangeMe!Doctor1");
        var draft = await SendAsync<ReplyPayload>(
            doctor,
            HttpMethod.Post,
            $"/api/feedback/{feedback.Id}/replies/ai-draft",
            body: null,
            HttpStatusCode.Created);

        draft.Status.Should().Be("Draft");
        draft.IsAiGenerated.Should().BeTrue();
        draft.Reply.Should().Be(_factory.Agent.Reply);
        // Submit analyses the comment, then staff asks for a fresh draft. Neither call publishes it.
        _factory.Agent.Calls.Should().Be(agentCallsBefore + 2);
        _factory.Agent.LastPath.Should().Be("/internal/agents/feedback-support");
        _factory.Agent.LastSecret.Should().Be("integration-test-agent-secret");

        var notificationsBeforeApproval = await GetAsync<List<NotificationPayload>>(patient, "/api/notifications/me");
        notificationsBeforeApproval.Should().NotContain(x => x.Message == _factory.Agent.Reply);

        var approved = await SendAsync<ReplyPayload>(
            doctor,
            HttpMethod.Patch,
            $"/api/replies/{draft.Id}/decision",
            new { decision = ReplyDecision.Approve },
            HttpStatusCode.OK);

        approved.Status.Should().Be("Posted");
        approved.IsAiGenerated.Should().BeTrue();
        approved.Reply.Should().Be(_factory.Agent.Reply);

        var notifications = await GetAsync<List<NotificationPayload>>(patient, "/api/notifications/me");
        var notice = notifications.Should().ContainSingle(x => x.Type == "FeedbackReply" && x.Message == _factory.Agent.Reply).Subject;
        notice.Title.Should().Be("Reply to your feedback");
        notice.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task PublicFeed_HidesAnonymousName_AfterStaffShowsTheComment()
    {
        var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
        var treatmentId = await FirstTreatmentIdAsync(admin);

        var email = $"anon-{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";
        const string fullName = "Lakshmi Vaidyar";
        await RegisterPatientAsync(email, password, fullName, $"91{Random.Shared.Next(10000000, 99999999)}");
        var patientId = await CreatePatientRecordAsync(admin, email, "Lakshmi", "Vaidyar");
        var appointmentId = await CreateCompletedAppointmentAsync(admin, patientId, treatmentId);

        var patient = await LoginAsync(email, password);
        const string comment = "I prefer this shirodhara note to stay unnamed on the public board.";
        var created = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
        {
            appointmentId,
            rating = 4,
            comment,
            isAnonymous = true
        }, HttpStatusCode.Created);

        created.IsAnonymous.Should().BeTrue();
        created.PatientName.Should().Be("Anonymous patient");
        created.PatientId.Should().BeNull();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var stored = await db.Feedbacks.SingleAsync(x => x.Id == created.Id);
            stored.PatientNameSnapshot.Should().Be(fullName);
            stored.IsAnonymous.Should().BeTrue();
            stored.Status.Should().Be(FeedbackStatus.PendingModeration);
        }

        var shown = await SendAsync<FeedbackPayload>(
            admin,
            HttpMethod.Patch,
            $"/api/feedback/{created.Id}/moderate",
            new { action = FeedbackModerationAction.Show },
            HttpStatusCode.OK);
        shown.Status.Should().Be("Visible");
        shown.PatientName.Should().Be("Anonymous patient");

        var publicClient = _factory.CreateClient();
        var response = await publicClient.GetAsync("/api/feedback");
        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body: {0}", json);

        json.Should().Contain(comment);
        json.Should().Contain("Anonymous patient");
        json.Should().NotContain(fullName);
        json.Should().NotContain("Lakshmi");
        json.Should().NotContain("therapist was dismissive");

        var feed = JsonSerializer.Deserialize<List<FeedItem>>(json, Json)
            ?? throw new InvalidOperationException("Public feed payload was empty.");
        var item = feed.Single(x => x.Comment == comment);
        item.IsAnonymous.Should().BeTrue();
        item.PatientName.Should().Be("Anonymous patient");
        item.PatientId.Should().BeNull();
        item.Status.Should().Be("Visible");
        feed.Should().NotContain(x => x.Status == "Hidden");
        feed.Should().NotContain(x => x.Comment.Contains("therapist was dismissive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StaffManualReply_IsPosted_AndVisibleOnThePatientsOwnFeedback()
    {
        var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
        var treatmentId = await FirstTreatmentIdAsync(admin);
        var email = $"reply-{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";
        await RegisterPatientAsync(email, password, "Anita Deshmukh", $"91{Random.Shared.Next(10000000, 99999999)}");
        var patientId = await CreatePatientRecordAsync(admin, email, "Anita", "Deshmukh");
        var appointmentId = await CreateCompletedAppointmentAsync(admin, patientId, treatmentId);
        var patient = await LoginAsync(email, password);
        const string comment = "The abhyanga wait ran long, and I am writing this anonymously.";
        var created = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
        {
            appointmentId,
            rating = 2,
            comment,
            isAnonymous = true
        }, HttpStatusCode.Created);

        const string reply = "Namaste. The care team will review the abhyanga queue.";
        var posted = await PostAsync<ReplyPayload>(admin, $"/api/feedback/{created.Id}/replies", new { reply }, HttpStatusCode.Created);
        posted.Status.Should().Be("Posted");
        posted.IsAiGenerated.Should().BeFalse();

        var mine = await GetAsync<List<MineItem>>(patient, "/api/feedback/mine");
        var own = mine.Should().ContainSingle(x => x.Id == created.Id).Subject;
        own.Status.Should().Be("PendingModeration");
        var visibleReply = own.Replies.Should().ContainSingle().Subject;
        visibleReply.UserRole.Should().Be("Staff");
        visibleReply.Reply.Should().Be(reply);

        var notices = await GetAsync<List<NotificationPayload>>(patient, "/api/notifications/me");
        notices.Should().Contain(x => x.Type == "FeedbackReply" && x.Message == reply);

        var publicClient = _factory.CreateClient();
        var beforeShow = await publicClient.GetStringAsync("/api/feedback");
        beforeShow.Should().NotContain(reply);

        await SendAsync<FeedbackPayload>(
            admin,
            HttpMethod.Patch,
            $"/api/feedback/{created.Id}/moderate",
            new { action = FeedbackModerationAction.Show },
            HttpStatusCode.OK);
        var afterShow = await publicClient.GetStringAsync("/api/feedback");
        afterShow.Should().Contain(reply);
        afterShow.Should().Contain("Anonymous patient");
        afterShow.Should().NotContain("Anita Deshmukh");
    }

    [Fact]
    public async Task Withdraw_HidesTheComment_AndClosesReplies()
    {
        var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
        var treatmentId = await FirstTreatmentIdAsync(admin);
        var email = $"withdraw-{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";
        await RegisterPatientAsync(email, password, "Meera Nair", $"91{Random.Shared.Next(10000000, 99999999)}");
        var patientId = await CreatePatientRecordAsync(admin, email, "Meera", "Nair");
        var appointmentId = await CreateCompletedAppointmentAsync(admin, patientId, treatmentId);
        var patient = await LoginAsync(email, password);
        const string comment = "Please withdraw this note about the crowded waiting area.";
        var created = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
        {
            appointmentId,
            rating = 3,
            comment,
            isAnonymous = false
        }, HttpStatusCode.Created);

        await SendAsync<FeedbackPayload>(
            admin,
            HttpMethod.Patch,
            $"/api/feedback/{created.Id}/moderate",
            new { action = FeedbackModerationAction.Show },
            HttpStatusCode.OK);

        var withdrawn = await SendAsync<FeedbackPayload>(
            patient,
            HttpMethod.Patch,
            $"/api/feedback/{created.Id}",
            new { withdraw = true },
            HttpStatusCode.OK);
        withdrawn.Status.Should().Be("Withdrawn");

        var mine = await GetAsync<List<MineItem>>(patient, "/api/feedback/mine");
        mine.Should().ContainSingle(x => x.Id == created.Id && x.Status == "Withdrawn");

        var publicFeed = await _factory.CreateClient().GetStringAsync("/api/feedback");
        publicFeed.Should().NotContain(comment);

        var staffView = await GetAsync<FeedbackPayload>(admin, $"/api/feedback/{created.Id}");
        staffView.Status.Should().Be("Withdrawn");

        var replyAttempt = await SendRawAsync(
            admin,
            HttpMethod.Post,
            $"/api/feedback/{created.Id}/replies",
            new { reply = "This reply should be refused." });
        replyAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await replyAttempt.Content.ReadAsStringAsync()).Should().Contain("withdrawn");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var stored = await db.Feedbacks.SingleAsync(x => x.Id == created.Id);
            stored.CreatedAt = DateTimeOffset.UtcNow.AddHours(-25);
            await db.SaveChangesAsync();
        }

        var late = await SendRawAsync(patient, HttpMethod.Patch, $"/api/feedback/{created.Id}", new { withdraw = true });
        late.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await late.Content.ReadAsStringAsync()).Should().Contain("24 hours");
    }

    [Fact]
    public async Task SubmitFeedback_WhenAgentIsDown_StillStoresTheComment()
    {
        _factory.Agent.Fail = true;
        try
        {
            var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
            var treatmentId = await FirstTreatmentIdAsync(admin);
            var email = $"agent-down-{Guid.NewGuid():N}@example.local";
            const string password = "ChangeMe!Patient1";
            await RegisterPatientAsync(email, password, "Meera Nair", $"91{Random.Shared.Next(10000000, 99999999)}");
            var patientId = await CreatePatientRecordAsync(admin, email, "Meera", "Nair");
            var appointmentId = await CreateCompletedAppointmentAsync(admin, patientId, treatmentId);
            var patient = await LoginAsync(email, password);
            const string comment = "The panchakarma wait was long, but the comment must still be saved.";

            var created = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
            {
                appointmentId,
                rating = 2,
                comment,
                isAnonymous = false
            }, HttpStatusCode.Created);

            created.Comment.Should().Be(comment);
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var stored = await db.Feedbacks.SingleAsync(x => x.Id == created.Id);
            stored.Sentiment.Should().BeNull();
            stored.Category.Should().BeNull();
            (await db.FeedbackReplies.CountAsync(x => x.FeedbackId == created.Id)).Should().Be(0);
        }
        finally
        {
            _factory.Agent.Fail = false;
        }
    }

    private async Task<Guid> FirstTreatmentIdAsync(string token)
    {
        var page = await GetAsync<AppointmentPage>(token, "/api/appointments");
        page.Items.Should().NotBeEmpty();
        return page.Items[0].TreatmentId;
    }

    [Fact]
    public async Task PatientComplaint_IsListedForStaff_AndAReplyReachesThePatientInbox()
    {
        var admin = await LoginAsync("admin@smartayurveda.local", "ChangeMe!Admin1");
        var treatmentId = await FirstTreatmentIdAsync(admin);
        var email = $"queue-{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";
        await RegisterPatientAsync(email, password, "Hiruni Silva", $"91{Random.Shared.Next(10000000, 99999999)}");
        var chartId = await CreatePatientRecordAsync(admin, email, "Hiruni", "Silva");
        var patient = await LoginAsync(email, password);

        const string subject = "The abhyanga queue spilled into the corridor";
        var created = await PostAsync<ComplaintPayload>(patient, "/api/complaints", new
        {
            subject,
            description = "There was no update at reception while the wait grew.",
            priority = ComplaintPriority.Normal
        }, HttpStatusCode.Created);

        created.Status.Should().Be("Open");
        created.PatientId.Should().Be(chartId);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var login = await db.Users.SingleAsync(x => x.Email == email);
            created.PatientId.Should().NotBe(login.Id);
        }

        var queue = await GetAsync<List<ComplaintPayload>>(admin, "/api/complaints");
        queue.Should().ContainSingle(x => x.Id == created.Id && x.Status == "Open" && x.Subject == subject);
        queue.Should().BeInDescendingOrder(x => x.CreatedAt);

        var overdue = await GetAsync<List<ComplaintPayload>>(admin, "/api/complaints?overdue=true");
        overdue.Should().NotContain(x => x.Id == created.Id);

        var staffNotices = await GetAsync<List<NotificationPayload>>(admin, "/api/notifications/staff");
        staffNotices.Should().Contain(x => x.Type == "FeedbackAlert" && x.Message.Contains(subject));

        var patientNotices = await GetAsync<List<NotificationPayload>>(patient, "/api/notifications/me");
        patientNotices.Should().Contain(x => x.Type == "ComplaintUpdate" && x.Title == "Complaint received");
        patientNotices.Should().NotContain(x => x.Type == "FeedbackAlert");

        var appointmentId = await CreateCompletedAppointmentAsync(admin, chartId, treatmentId);
        var feedback = await PostAsync<FeedbackPayload>(patient, "/api/feedback", new
        {
            appointmentId,
            rating = 2,
            comment = "The wait for abhyanga was longer than the slot.",
            isAnonymous = false
        }, HttpStatusCode.Created);
        const string reply = "Namaste. A vaidya will review the abhyanga queue.";
        await PostAsync<ReplyPayload>(admin, $"/api/feedback/{feedback.Id}/replies", new { reply }, HttpStatusCode.Created);

        var afterReply = await GetAsync<List<NotificationPayload>>(patient, "/api/notifications/me");
        afterReply.Should().Contain(x => x.Type == "FeedbackReply" && x.Message == reply);
    }

    private async Task RegisterPatientAsync(string email, string password, string fullName, string phoneNumber)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName,
            email,
            phoneNumber,
            password,
            dateOfBirth = new DateOnly(1990, 6, 15),
            gender = Gender.Female
        }, Json);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body: {0}", body);
    }

    private async Task<Guid> CreatePatientRecordAsync(string token, string email, string firstName, string lastName)
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var openedAtRegistration = await db.Patients
                .SingleOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email.ToLower());
            if (openedAtRegistration is not null)
            {
                return openedAtRegistration.Id;
            }
        }

        var created = await PostAsync<PatientPayload>(token, "/api/patients", new
        {
            firstName,
            lastName,
            dateOfBirth = new DateOnly(1990, 6, 15),
            gender = Gender.Female,
            phone = $"98{Random.Shared.Next(10000000, 99999999)}",
            email,
            address = "Kochi",
            bloodGroup = "O+",
            prakriti = DoshaType.Pitta,
            vikriti = DoshaType.Vata
        }, HttpStatusCode.Created);

        created.Email.Should().Be(email);
        return created.Id;
    }

    private async Task<Guid> CreateCompletedAppointmentAsync(string token, Guid patientId, Guid treatmentId)
    {
        var slot = Interlocked.Increment(ref _slot);
        var created = await PostAsync<AppointmentPayload>(token, "/api/appointments", new
        {
            patientId,
            treatmentId,
            requestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40 + slot)),
            requestedTimeSlot = "09:00-09:30"
        }, HttpStatusCode.Created);

        created.Status.Should().Be("Pending");

        var completed = await SendAsync<AppointmentPayload>(
            token,
            HttpMethod.Patch,
            $"/api/appointments/{created.Id}/status",
            new { status = AppointmentStatus.Completed },
            HttpStatusCode.OK);

        completed.Status.Should().Be("Completed");
        return completed.Id;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password }, Json);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body: {0}", body);
        var payload = JsonSerializer.Deserialize<AuthPayload>(body, Json);
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
        return payload.Token;
    }

    private async Task<HttpResponseMessage> SendRawAsync(string token, HttpMethod method, string url, object? body)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await client.SendAsync(request);
    }

    private Task<T> GetAsync<T>(string token, string url) =>
        SendAsync<T>(token, HttpMethod.Get, url, body: null, HttpStatusCode.OK);

    private Task<T> PostAsync<T>(string token, string url, object body, HttpStatusCode expected) =>
        SendAsync<T>(token, HttpMethod.Post, url, body, expected);

    private async Task<T> SendAsync<T>(string token, HttpMethod method, string url, object? body, HttpStatusCode expected)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, "response body: {0}", payload);
        return JsonSerializer.Deserialize<T>(payload, Json)
            ?? throw new InvalidOperationException($"Empty payload from {method} {url}.");
    }

    private sealed record AuthPayload(string Token);
    private sealed record PatientPayload(Guid Id, string? Email);
    private sealed record AppointmentPayload(Guid Id, Guid TreatmentId, string Status);
    private sealed record AppointmentPage(List<AppointmentPayload> Items);
    private sealed record FeedbackPayload(
        Guid Id,
        Guid? PatientId,
        string PatientName,
        string Comment,
        bool IsAnonymous,
        string Status);
    private sealed record ReplyPayload(Guid Id, string Status, bool IsAiGenerated, string Reply);
    private sealed record PublicReplyPayload(Guid Id, string UserRole, string Reply);
    private sealed record MineItem(Guid Id, string Status, string Comment, List<PublicReplyPayload> Replies);
    private sealed record NotificationPayload(Guid Id, string Title, string Message, string Type, bool IsRead);
    private sealed record ComplaintPayload(
        Guid Id,
        Guid PatientId,
        string Subject,
        string Status,
        DateTimeOffset CreatedAt);
    private sealed record FeedItem(Guid? PatientId, string PatientName, string Comment, bool IsAnonymous, string Status);
}
