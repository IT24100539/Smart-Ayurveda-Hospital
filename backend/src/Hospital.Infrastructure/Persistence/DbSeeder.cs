using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(HospitalDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                new User
                {
                    Email = "admin@smartayurveda.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Admin1"),
                    FullName = "Hospital Administrator",
                    PhoneNumber = "0000000000",
                    Role = UserRole.Admin
                },
                new User
                {
                    Email = "doctor@smartayurveda.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Doctor1"),
                    FullName = "Dr. Ananya Sharma",
                    PhoneNumber = "0000000001",
                    Role = UserRole.Doctor
                });
        }

        const string patientLoginEmail = "meera.nair@example.local";
        if (!await db.Users.AnyAsync(x => x.Email == patientLoginEmail, cancellationToken))
        {
            db.Users.Add(new User
            {
                Email = patientLoginEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Patient1"),
                FullName = "Meera Nair",
                PhoneNumber = "9876500001",
                Role = UserRole.Patient
            });
        }

        if (!await db.StaffUsers.AnyAsync(cancellationToken))
        {
            db.StaffUsers.AddRange(
                new StaffUser
                {
                    Email = "admin@smartayurveda.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Admin1"),
                    FullName = "Hospital Administrator",
                    Role = StaffRole.Admin,
                    Phone = "0000000000"
                },
                new StaffUser
                {
                    Email = "doctor@smartayurveda.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Doctor1"),
                    FullName = "Dr. Ananya Sharma",
                    Role = StaffRole.Doctor,
                    Specialization = "Kayachikitsa",
                    Phone = "0000000001"
                });
        }

        if (!await db.Treatments.AnyAsync(cancellationToken))
        {
            db.Treatments.AddRange(
                new Treatment { Name = "Initial Consultation", Category = TreatmentCategory.Consultation, Description = "Nadi pariksha and prakriti assessment.", DurationMinutes = 30, UnitPrice = 500 },
                new Treatment { Name = "Abhyanga", Category = TreatmentCategory.Abhyanga, Description = "Full-body herbal oil massage.", DurationMinutes = 60, UnitPrice = 1800 },
                new Treatment { Name = "Shirodhara", Category = TreatmentCategory.Shirodhara, Description = "Continuous stream of warm oil on the forehead.", DurationMinutes = 45, UnitPrice = 2200 },
                new Treatment { Name = "Panchakarma Package (7 day)", Category = TreatmentCategory.Panchakarma, Description = "Supervised detoxification programme.", DurationMinutes = 0, UnitPrice = 28000 });
        }

        if (!await db.Medicines.AnyAsync(cancellationToken))
        {
            db.Medicines.AddRange(
                new Medicine { Name = "Triphala Churna", Form = MedicineForm.Churna, DosageGuidelines = "3-6 g at bedtime with warm water.", UnitPrice = 180 },
                new Medicine { Name = "Ashwagandha", Form = MedicineForm.Churna, DosageGuidelines = "3 g twice daily with milk.", UnitPrice = 240 },
                new Medicine { Name = "Dashamoola Kashayam", Form = MedicineForm.Kashayam, DosageGuidelines = "15 ml twice daily after food.", UnitPrice = 210 });
        }

        await db.SaveChangesAsync(cancellationToken);
        await SeedFeedbackAndCommunicationAsync(db, cancellationToken);
        logger.LogInformation("Database schema ready and seed data applied.");
    }

    private static async Task SeedFeedbackAndCommunicationAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Feedbacks.AnyAsync(cancellationToken))
        {
            return;
        }

        var patients = await EnsureSeedPatientsAsync(db, cancellationToken);
        var meera = patients[0];
        var arjun = patients[1];
        var meeraName = $"{meera.FirstName} {meera.LastName}";
        var arjunName = $"{arjun.FirstName} {arjun.LastName}";

        var adminStaff = await db.StaffUsers.FirstAsync(x => x.Email == "admin@smartayurveda.local", cancellationToken);
        var doctorStaff = await db.StaffUsers.FirstAsync(x => x.Email == "doctor@smartayurveda.local", cancellationToken);
        var adminUser = await db.Users.FirstAsync(x => x.Email == "admin@smartayurveda.local", cancellationToken);
        var doctorUser = await db.Users.FirstAsync(x => x.Email == "doctor@smartayurveda.local", cancellationToken);

        var abhyanga = await db.Treatments.FirstAsync(x => x.Name == "Abhyanga", cancellationToken);
        var shirodhara = await db.Treatments.FirstAsync(x => x.Name == "Shirodhara", cancellationToken);
        var consultation = await db.Treatments.FirstAsync(x => x.Name == "Initial Consultation", cancellationToken);

        var appointment = await db.Appointments.FirstOrDefaultAsync(cancellationToken);
        if (appointment is null)
        {
            var scheduledAt = DateTimeOffset.UtcNow.AddDays(-5);
            appointment = new Appointment
            {
                PatientId = meera.Id,
                DoctorId = doctorStaff.Id,
                ScheduledAt = scheduledAt,
                EndsAt = scheduledAt.AddMinutes(30),
                DurationMinutes = 30,
                Status = AppointmentStatus.Completed,
                Reason = "Follow-up nadi pariksha after abhyanga."
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var moderatedAt = now.AddDays(-1);

        var abhyangaFeedback = new Feedback
        {
            PatientId = meera.Id,
            PatientNameSnapshot = meeraName,
            TreatmentId = abhyanga.Id,
            AppointmentId = appointment.Id,
            Rating = 5,
            Comment = "The abhyanga session eased my vata stiffness. The therapist explained the herbal oil choice clearly.",
            IsAnonymous = false,
            Sentiment = FeedbackSentiment.Positive,
            Category = FeedbackCategory.TreatmentQuality,
            Status = FeedbackStatus.Visible
        };

        var waitingFeedback = new Feedback
        {
            PatientId = arjun.Id,
            PatientNameSnapshot = arjunName,
            Rating = 2,
            Comment = "Waited nearly an hour past my nadi pariksha slot. The front desk did not update the board.",
            IsAnonymous = true,
            Sentiment = FeedbackSentiment.Negative,
            Category = FeedbackCategory.WaitingTime,
            Status = FeedbackStatus.Visible
        };

        var staffFeedback = new Feedback
        {
            PatientId = meera.Id,
            PatientNameSnapshot = meeraName,
            TreatmentId = shirodhara.Id,
            Rating = 1,
            Comment = "A therapist was dismissive when I asked about post-shirodhara rest. This does not match our hospital's seva.",
            IsAnonymous = false,
            Sentiment = FeedbackSentiment.Negative,
            Category = FeedbackCategory.StaffService,
            Status = FeedbackStatus.Hidden,
            ModeratedBy = adminStaff.Id,
            ModeratedAt = moderatedAt
        };

        var facilityFeedback = new Feedback
        {
            PatientId = arjun.Id,
            PatientNameSnapshot = arjunName,
            TreatmentId = shirodhara.Id,
            Rating = 3,
            Comment = "Shirodhara itself was calming, but the recovery room fan was noisy and the linen looked worn.",
            IsAnonymous = false,
            Sentiment = FeedbackSentiment.Neutral,
            Category = FeedbackCategory.FacilityIssue,
            Status = FeedbackStatus.Visible
        };

        var pendingFeedback = new Feedback
        {
            PatientId = meera.Id,
            PatientNameSnapshot = meeraName,
            TreatmentId = consultation.Id,
            Rating = 4,
            Comment = "Prakriti assessment felt thorough. Still waiting to hear back about the panchakarma package dates.",
            IsAnonymous = false,
            Sentiment = FeedbackSentiment.Positive,
            Category = FeedbackCategory.Other,
            Status = FeedbackStatus.PendingModeration
        };

        db.Feedbacks.AddRange(abhyangaFeedback, waitingFeedback, staffFeedback, facilityFeedback, pendingFeedback);
        await db.SaveChangesAsync(cancellationToken);

        db.FeedbackReplies.AddRange(
            new FeedbackReply
            {
                FeedbackId = waitingFeedback.Id,
                UserId = null,
                UserRole = FeedbackReplyUserRole.Staff,
                Reply = "Namaste. We are sorry the nadi pariksha slot ran late. A draft apology and a complimentary consultation offer are ready for staff review.",
                IsAiGenerated = true,
                Status = FeedbackReplyStatus.Draft
            },
            new FeedbackReply
            {
                FeedbackId = abhyangaFeedback.Id,
                UserId = doctorUser.Id,
                UserRole = FeedbackReplyUserRole.Staff,
                Reply = $"Thank you, {meera.FirstName}. I have noted the oil feedback for the kayachikitsa team so we can keep this abhyanga protocol consistent.",
                IsAiGenerated = false,
                Status = FeedbackReplyStatus.Posted
            });

        db.FeedbackReactions.AddRange(
            new FeedbackReaction
            {
                FeedbackId = abhyangaFeedback.Id,
                UserId = doctorUser.Id,
                ReactionType = FeedbackReactionType.Like
            },
            new FeedbackReaction
            {
                FeedbackId = facilityFeedback.Id,
                UserId = adminUser.Id,
                ReactionType = FeedbackReactionType.Like
            });

        var openComplaint = new Complaint
        {
            PatientId = arjun.Id,
            FeedbackId = waitingFeedback.Id,
            Subject = "Long wait before nadi pariksha",
            Description = "The scheduled nadi pariksha started almost an hour late with no update at reception.",
            Priority = ComplaintPriority.Normal,
            Status = ComplaintStatus.Open
        };

        var escalatedComplaint = new Complaint
        {
            PatientId = meera.Id,
            FeedbackId = staffFeedback.Id,
            Subject = "Unprofessional response during shirodhara aftercare",
            Description = "The therapist dismissed questions about rest after shirodhara. Please review staff seva training.",
            Priority = ComplaintPriority.High,
            Status = ComplaintStatus.Escalated,
            AssignedTo = adminStaff.Id,
            EscalatedAt = now.AddHours(-6)
        };

        db.Complaints.AddRange(openComplaint, escalatedComplaint);

        db.Notifications.AddRange(
            new Notification
            {
                PatientId = meera.Id,
                Title = "Reply to your abhyanga feedback",
                Message = "Dr. Ananya Sharma has posted a reply to your treatment quality feedback.",
                Type = NotificationType.FeedbackReply,
                IsRead = false
            },
            new Notification
            {
                PatientId = arjun.Id,
                Title = "Complaint received",
                Message = "We have opened your complaint about the delayed nadi pariksha slot.",
                Type = NotificationType.ComplaintUpdate,
                IsRead = true
            },
            new Notification
            {
                PatientId = meera.Id,
                Title = "Complaint escalated",
                Message = "Your shirodhara aftercare complaint has been escalated to hospital administration.",
                Type = NotificationType.ComplaintEscalated,
                IsRead = false
            },
            new Notification
            {
                PatientId = arjun.Id,
                Title = "Panchakarma wing hours",
                Message = "The panchakarma wing will close early on Purnima. Please check your next therapy slot.",
                Type = NotificationType.General,
                IsRead = false
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Patient>> EnsureSeedPatientsAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Patients.OrderBy(x => x.Uhid).Take(2).ToListAsync(cancellationToken);
        if (existing.Count >= 2)
        {
            return existing;
        }

        var meera = existing.FirstOrDefault(x => x.Uhid == "SAH-2026-00001") ?? new Patient
        {
            Uhid = "SAH-2026-00001",
            FirstName = "Meera",
            LastName = "Nair",
            DateOfBirth = new DateOnly(1988, 4, 12),
            Gender = Gender.Female,
            Phone = "9876500001",
            Email = "meera.nair@example.local",
            Prakriti = DoshaType.Pitta,
            Vikriti = DoshaType.Pitta | DoshaType.Vata
        };

        var arjun = existing.FirstOrDefault(x => x.Uhid == "SAH-2026-00002") ?? new Patient
        {
            Uhid = "SAH-2026-00002",
            FirstName = "Arjun",
            LastName = "Menon",
            DateOfBirth = new DateOnly(1991, 11, 3),
            Gender = Gender.Male,
            Phone = "9876500002",
            Email = "arjun.menon@example.local",
            Prakriti = DoshaType.Vata | DoshaType.Kapha,
            Vikriti = DoshaType.Vata
        };

        if (existing.All(x => x.Uhid != meera.Uhid))
        {
            db.Patients.Add(meera);
        }

        if (existing.All(x => x.Uhid != arjun.Uhid))
        {
            db.Patients.Add(arjun);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new[] { meera, arjun };
    }
}
