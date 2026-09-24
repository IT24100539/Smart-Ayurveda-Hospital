using Hospital.Application.Abstractions;
using Hospital.Application.Agents;
using Hospital.Application.Agents.Dtos;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;
using Hospital.Application.Communication;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

internal static class FeedbackTestClock
{
    public static readonly DateTimeOffset Now = new(2026, 9, 23, 8, 0, 0, TimeSpan.Zero);
}

internal sealed class FeedbackHarness
{
    public FeedbackHarness(bool actAsStaff = false)
    {
        Clock = new FixedClock(FeedbackTestClock.Now);
        Patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Secret",
            LastName = "Name",
            Email = "patient@example.local"
        };
        User = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Secret Name",
            Email = Patient.Email,
            Role = UserRole.Patient,
            IsActive = true
        };
        Staff = new StaffUser
        {
            Id = Guid.NewGuid(),
            Email = "doctor@smartayurveda.local",
            FullName = "Dr. Rao",
            Role = StaffRole.Doctor
        };
        StaffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = Staff.Email,
            FullName = Staff.FullName,
            Role = UserRole.Doctor,
            IsActive = true
        };
        var actors = new FakeActors(actAsStaff ? StaffUser : User, Patient, Staff);
        FeedbackStore = new FakeFeedbackRepository();
        ReactionStore = new FakeReactionRepository();
        ReplyStore = new FakeReplyRepository();
        ComplaintStore = new FakeComplaintRepository();
        NotificationStore = new FakeNotificationRepository();
        Appointments = new FakeAppointmentService();
        Treatments = new FakeTreatmentCatalog();
        Agents = new FakeAgentClient();
        var unitOfWork = new FakeUnitOfWork();
        var staffRepo = new FakeStaffRepository(Staff);

        Reactions = new ReactionService(FeedbackStore, ReactionStore, actors, unitOfWork);
        Replies = new ReplyService(FeedbackStore, ReplyStore, NotificationStore, actors, Agents, staffRepo, unitOfWork);
        Feedback = new FeedbackService(FeedbackStore, Appointments, Treatments, actors, unitOfWork, Clock, Replies);
        Complaints = new ComplaintService(
            ComplaintStore,
            FeedbackStore,
            NotificationStore,
            staffRepo,
            actors,
            unitOfWork,
            Clock);
        Notifications = new NotificationService(NotificationStore, actors, unitOfWork);
    }

    public FixedClock Clock { get; }
    public Patient Patient { get; }
    public User User { get; }
    public User StaffUser { get; }
    public StaffUser Staff { get; }
    public FakeFeedbackRepository FeedbackStore { get; }
    public FakeReactionRepository ReactionStore { get; }
    public FakeReplyRepository ReplyStore { get; }
    public FakeComplaintRepository ComplaintStore { get; }
    public FakeNotificationRepository NotificationStore { get; }
    public FakeAppointmentService Appointments { get; }
    public FakeTreatmentCatalog Treatments { get; }
    public FakeAgentClient Agents { get; }
    public FeedbackService Feedback { get; }
    public ReactionService Reactions { get; }
    public ReplyService Replies { get; }
    public ComplaintService Complaints { get; }
    public NotificationService Notifications { get; }

    public Feedback SeedFeedback(DateTimeOffset createdAt)
    {
        var feedback = new Feedback
        {
            PatientId = Patient.Id,
            PatientNameSnapshot = "Secret Name",
            Rating = 4,
            Comment = "Original comment.",
            Status = FeedbackStatus.PendingModeration,
            CreatedAt = createdAt
        };
        FeedbackStore.Items.Add(feedback);
        return feedback;
    }

    public static AppointmentDto Visit(Guid patientId, AppointmentStatus status) => new(
        Guid.NewGuid(),
        patientId,
        "Secret Name",
        "SAH-2026-00009",
        Guid.NewGuid(),
        "Nadi pariksha",
        null,
        DateOnly.FromDateTime(FeedbackTestClock.Now.AddDays(-2).UtcDateTime),
        "09:00-09:30",
        status,
        null,
        null);

    public static Complaint OpenComplaint(Patient patient, DateTimeOffset createdAt, ComplaintStatus status) => new()
    {
        PatientId = patient.Id,
        Patient = patient,
        Subject = "Late nadi pariksha",
        Description = "The slot started an hour late.",
        Status = status,
        CreatedAt = createdAt
    };

    internal sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeActors : IActorContext
    {
        private readonly User _user;
        private readonly Patient _patient;
        private readonly StaffUser _staff;

        public FakeActors(User user, Patient patient, StaffUser staff)
        {
            _user = user;
            _patient = patient;
            _staff = staff;
        }

        public Task<User> RequireUserAsync(CancellationToken cancellationToken) => Task.FromResult(_user);
        public Task<Patient> RequirePatientAsync(CancellationToken cancellationToken) => Task.FromResult(_patient);
        public Task<StaffUser> RequireStaffAsync(CancellationToken cancellationToken) => Task.FromResult(_staff);
    }

    internal sealed class FakeAppointmentService : IAppointmentService
    {
        public Task CancelAsync(Guid appointmentId, Guid requestingPatientId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public AppointmentDto? Appointment { get; set; }
        public int GetCalls { get; private set; }

        public Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetCalls++;
            if (Appointment is null || Appointment.Id != id)
            {
                throw new NotFoundException(nameof(Appointment), id);
            }

            return Task.FromResult(Appointment);
        }

        public Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PagedResult<AppointmentDto>> ListAsync(
            DateOnly? onDate, Guid? patientId, Guid? doctorId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<AppointmentDto> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    internal sealed class FakeTreatmentCatalog : ITreatmentCatalog
    {
        public HashSet<Guid> Ids { get; } = new();
        public Task<bool> ExistsAsync(Guid treatmentId, CancellationToken cancellationToken) =>
            Task.FromResult(Ids.Contains(treatmentId));
    }

    internal sealed class FakeAgentClient : IAgentClient
    {
        public string Reply { get; set; } = "Namaste. This is a draft.";
        public string? Sentiment { get; set; } = "Positive";
        public string? Category { get; set; } = "TreatmentQuality";
        public string? Priority { get; set; } = "Normal";
        public bool ImmediateDashboardAlert { get; set; }
        public bool DraftSkipped { get; set; }
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }

        public Task<AgentInvokeResponse> InvokeAsync(AgentInvokeRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new AgentInvokeResponse("feedback", Reply, new Dictionary<string, string>()));
        }

        public Task<FeedbackSupportAgentResponse> DraftFeedbackSupportAsync(
            FeedbackSupportAgentRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(new FeedbackSupportAgentResponse(
                Sentiment,
                Category,
                Priority,
                0,
                DraftSkipped ? null : Reply,
                DraftSkipped,
                "wf-test",
                "awaiting_review",
                ImmediateDashboardAlert));
        }
    }

    internal sealed class FakeFeedbackRepository : IFeedbackRepository
    {
        public List<Feedback> Items { get; } = new();

        public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(Feedback feedback, CancellationToken cancellationToken)
        {
            Items.Add(feedback);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<Feedback> Items, int Total)> SearchAsync(
            FeedbackStatus? status,
            int? rating,
            FeedbackCategory? category,
            FeedbackSentiment? sentiment,
            int page,
            int pageSize,
            string? sort,
            string? search,
            CancellationToken cancellationToken)
        {
            IEnumerable<Feedback> query = Items;
            if (status is not null)
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.Comment.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (!x.IsAnonymous && x.PatientNameSnapshot.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var list = query.ToList();
            return Task.FromResult(((IReadOnlyList<Feedback>)list, list.Count));
        }

        public Task<IReadOnlyList<Feedback>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Feedback>)Items.Where(x => x.PatientId == patientId).ToList());

        public Task<IReadOnlyList<FeedbackStatRow>> ListForStatsAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<FeedbackStatRow>)Items
                .Select(x => new FeedbackStatRow(x.Status, x.Sentiment, x.Category, x.Rating))
                .ToList());

        public Task<IReadOnlyList<Feedback>> ListVisibleAsync(int take, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Feedback>)Items.Where(x => x.Status == FeedbackStatus.Visible).Take(take).ToList());

        public Task<Feedback?> GetWithPatientAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<int> CountRecentInCategoryExcludingPatientAsync(
            FeedbackCategory category,
            Guid excludePatientId,
            DateTimeOffset createdAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Count(x =>
                x.Category == category
                && x.PatientId != excludePatientId
                && x.CreatedAt >= createdAfter));
    }

    internal sealed class FakeReactionRepository : IFeedbackReactionRepository
    {
        public List<FeedbackReaction> Items { get; } = new();

        public Task<FeedbackReaction?> GetByUserAsync(Guid feedbackId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.FeedbackId == feedbackId && x.UserId == userId));

        public Task AddAsync(FeedbackReaction reaction, CancellationToken cancellationToken)
        {
            Items.Add(reaction);
            return Task.CompletedTask;
        }

        public void Remove(FeedbackReaction reaction) => Items.Remove(reaction);
    }

    internal sealed class FakeReplyRepository : IFeedbackReplyRepository
    {
        public List<FeedbackReply> Items { get; } = new();

        public Task<FeedbackReply?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(FeedbackReply reply, CancellationToken cancellationToken)
        {
            Items.Add(reply);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeComplaintRepository : IComplaintRepository
    {
        public List<Complaint> Items { get; } = new();

        public Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Complaint>> ListAsync(
            ComplaintStatus? status,
            ComplaintPriority? priority,
            CancellationToken cancellationToken)
        {
            IEnumerable<Complaint> query = Items;
            if (status is not null)
            {
                query = query.Where(x => x.Status == status);
            }

            if (priority is not null)
            {
                query = query.Where(x => x.Priority == priority);
            }

            return Task.FromResult((IReadOnlyList<Complaint>)query.ToList());
        }

        public Task<IReadOnlyList<Complaint>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Complaint>)Items.Where(x => x.PatientId == patientId).ToList());

        public Task<IReadOnlyList<Complaint>> ListOpenOlderThanAsync(DateTimeOffset createdBefore, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Complaint>)Items
                .Where(x => x.Status == ComplaintStatus.Open && x.CreatedAt < createdBefore)
                .ToList());

        public Task AddAsync(Complaint complaint, CancellationToken cancellationToken)
        {
            Items.Add(complaint);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Items { get; } = new();

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Notification>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Notification>)Items.Where(x => x.PatientId == patientId && x.StaffUserId == null).ToList());

        public Task<IReadOnlyList<Notification>> ListForStaffAsync(Guid staffUserId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Notification>)Items.Where(x => x.StaffUserId == staffUserId).ToList());

        public Task<int> MarkAllReadForPatientAsync(Guid patientId, CancellationToken cancellationToken)
        {
            var unread = Items.Where(x => x.PatientId == patientId && x.StaffUserId == null && !x.IsRead).ToList();
            foreach (var item in unread)
            {
                item.IsRead = true;
            }

            return Task.FromResult(unread.Count);
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Items.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStaffRepository : IStaffUserRepository
    {
        private readonly StaffUser _staff;
        public FakeStaffRepository(StaffUser staff) => _staff = staff;
        public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == _staff.Id ? _staff : null);
        public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<StaffUser?>(null);

        public Task<StaffUser?> FindActiveByRoleAsync(StaffRole role, CancellationToken cancellationToken) =>
            Task.FromResult(_staff.IsActive && _staff.Role == role ? _staff : null);

        public Task<IReadOnlyList<StaffUser>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_staff.IsActive
                ? (IReadOnlyList<StaffUser>)new List<StaffUser> { _staff }
                : Array.Empty<StaffUser>());
    }
}
