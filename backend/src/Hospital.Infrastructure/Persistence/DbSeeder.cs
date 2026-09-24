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

        if (!await db.Users.AnyAsync(u => u.Email == "therapist@smartayurveda.local", cancellationToken))
        {
            db.Users.Add(new User
            {
                Email = "therapist@smartayurveda.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe!Therapist1"),
                FullName = "Nimali Perera",
                PhoneNumber = "0000000002",
                Role = UserRole.Therapist
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

        await db.SaveChangesAsync(cancellationToken);

        await SeedTreatmentsInformationAsync(db, cancellationToken);

        if (!await db.Medicines.AnyAsync(cancellationToken))
        {
            db.Medicines.AddRange(
                new Medicine { Name = "Triphala Churna", Form = MedicineForm.Churna, DosageGuidelines = "3-6 g at bedtime with warm water.", UnitPrice = 180 },
                new Medicine { Name = "Ashwagandha", Form = MedicineForm.Churna, DosageGuidelines = "3 g twice daily with milk.", UnitPrice = 240 },
                new Medicine { Name = "Dashamoola Kashayam", Form = MedicineForm.Kashayam, DosageGuidelines = "15 ml twice daily after food.", UnitPrice = 210 });
        }

        await db.SaveChangesAsync(cancellationToken);

        await SeedPatientsAsync(db, cancellationToken);
        await SeedTreatmentSchedulesAsync(db, cancellationToken);
        await SeedWardsAndBedsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SeedAdmissionRequestsAsync(db, cancellationToken);
        await SeedAppointmentsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SeedFeedbackAndCommunicationAsync(db, cancellationToken);
        logger.LogInformation("Database schema ready and seed data applied.");
    }

    private static async Task SeedTreatmentsInformationAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Treatments.AnyAsync(cancellationToken))
        {
            return;
        }

        var panchakarma = NewTreatment(
            "Panchakarma",
            "පංචකර්ම",
            "Supervised five-fold shodhana programme (vamana, virechana, basti, nasya, raktamokshana as indicated) to clear ama and restore dosha balance.",
            "වෛද්‍ය අධීක්ෂණය යටතේ සිදු කරන පංචකර්ම ශෝධන වැඩසටහන. ආම දුරු කර දෝෂ සමබරතාව යථා තත්ත්වයට පත් කිරීමට උපකාරී වේ.",
            TreatmentCategory.Panchakarma,
            durationMinutes: 90,
            unitPrice: 4500);

        var shirodhara = NewTreatment(
            "Shirodhara",
            "ශිරෝධාරා",
            "Continuous stream of warm medicated oil on the forehead; indicated for vata-related restlessness and sleep disturbance.",
            "නළල මත උණුසුම් ඖෂධ තෙල් ධාරාවක් යෙදෙන ශිරෝධාරා චිකිත්සාව. වාත අශාන්තිය සහ නින්ද ආබාධ සඳහා යොදා ගනී.",
            TreatmentCategory.Shirodhara,
            durationMinutes: 45,
            unitPrice: 2200);

        var herbalSteam = NewTreatment(
            "Herbal Steam Therapy",
            "ඖෂධීය වාෂ්ප චිකිත්සාව",
            "Swedana with herbal steam to loosen ama, open srotas, and prepare the body for panchakarma.",
            "ආම ලිහිල් කර ශ්‍රෝතස් විවෘත කිරීමට සහ පංචකර්ම සඳහා ශරීරය සූදානම් කිරීමට යොදන ඖෂධීය වාෂ්ප (ස්වේදන) චිකිත්සාව.",
            TreatmentCategory.HerbalSteam,
            durationMinutes: 30,
            unitPrice: 1500);

        var nasya = NewTreatment(
            "Nasya Treatment",
            "නස්‍ය",
            "Nasal administration of medicated oils for shiro-roga and kapha accumulated in the head and sinuses.",
            "හිස සහ නාසයේ රැඳී ඇති කඵ දෝෂයට සහ ශිරෝ රෝග සඳහා ඖෂධීය තෙල් නාසයට යෙදෙන නස්‍ය චිකිත්සාව.",
            TreatmentCategory.Nasya,
            durationMinutes: 30,
            unitPrice: 1200);

        var general = NewTreatment(
            "General Consultation",
            "සාමාන්‍ය උපදේශනය",
            "Nadi pariksha with prakriti and vikriti assessment, followed by a personalised chikitsa plan.",
            "නාඩි පරීක්ෂාව, ප්‍රකෘති සහ විකෘති තක්සේරුව, සහ පුද්ගලික චිකිත්සා සැලැස්මක් ඇතුළත් සාමාන්‍ය උපදේශනය.",
            TreatmentCategory.General,
            durationMinutes: 30,
            unitPrice: 500);

        var abhyanga = NewTreatment(
            "Abhyanga",
            "අභ්‍යංග",
            "Full-body herbal oil massage.",
            "මුළු ශරීරයට ඖෂධීය තෙල් ආලේප කරන අභ්‍යංග චිකිත්සාව.",
            TreatmentCategory.Abhyanga,
            durationMinutes: 60,
            unitPrice: 1800);

        var initialConsultation = NewTreatment(
            "Initial Consultation",
            "මූලික උපදේශනය",
            "Nadi pariksha and prakriti assessment.",
            "නාඩි පරීක්ෂාව සහ ප්‍රකෘති තක්සේරුව ඇතුළත් මූලික උපදේශනය.",
            TreatmentCategory.Consultation,
            durationMinutes: 30,
            unitPrice: 500);

        db.Treatments.AddRange(panchakarma, shirodhara, herbalSteam, nasya, general, abhyanga, initialConsultation);

        var therapistUser = await db.Users.FirstOrDefaultAsync(
            u => u.Email == "therapist@smartayurveda.local",
            cancellationToken);

        var nimali = new Therapist
        {
            UserId = therapistUser?.Id,
            FullName = "Nimali Perera",
            Specialization = "Panchakarma and Shirodhara"
        };
        var sunil = new Therapist
        {
            UserId = null,
            FullName = "Sunil Jayawardena",
            Specialization = "Nasya and Swedana"
        };
        db.Therapists.AddRange(nimali, sunil);

        // ILLUSTRATIVE EXAMPLE DATA — weekly availability below is placeholder pending
        // verification with the real hospital (proposal verification note). Do not treat
        // Mon/Wed/Fri and Tue/Thu day tags as confirmed clinical roster.
        db.TreatmentSchedules.AddRange(
            Slot(panchakarma, nimali, Weekday.Monday, "08:00", "12:00", 6),
            Slot(panchakarma, nimali, Weekday.Wednesday, "08:00", "12:00", 6),
            Slot(panchakarma, nimali, Weekday.Friday, "08:00", "12:00", 6),
            Slot(shirodhara, nimali, Weekday.Tuesday, "09:00", "13:00", 6),
            Slot(shirodhara, nimali, Weekday.Thursday, "09:00", "13:00", 6),
            Slot(herbalSteam, sunil, Weekday.Monday, "13:00", "17:00", 8),
            Slot(herbalSteam, sunil, Weekday.Wednesday, "13:00", "17:00", 8),
            Slot(herbalSteam, sunil, Weekday.Friday, "13:00", "17:00", 8),
            Slot(nasya, sunil, Weekday.Tuesday, "14:00", "16:00", 8),
            Slot(nasya, sunil, Weekday.Thursday, "14:00", "16:00", 8),
            Slot(general, nimali, Weekday.Monday, "14:00", "16:00", 10),
            Slot(general, nimali, Weekday.Wednesday, "14:00", "16:00", 10),
            Slot(general, nimali, Weekday.Friday, "14:00", "16:00", 10),
            Slot(abhyanga, nimali, Weekday.Monday, "09:00", "10:00", 8),
            Slot(initialConsultation, nimali, Weekday.Monday, "08:00", "08:30", 8));
    }

    private static Treatment NewTreatment(
        string name,
        string nameSinhala,
        string description,
        string descriptionSinhala,
        TreatmentCategory category,
        int durationMinutes,
        decimal unitPrice) =>
        new()
        {
            Name = name,
            NameSinhala = nameSinhala,
            Description = description,
            DescriptionSinhala = descriptionSinhala,
            Category = category,
            DurationMinutes = durationMinutes,
            UnitPrice = unitPrice,
            IsActive = true
        };

    private static TreatmentSchedule Slot(
        Treatment treatment,
        Therapist therapist,
        Weekday day,
        string start,
        string end,
        int maxSlots) =>
        new()
        {
            Treatment = treatment,
            Therapist = therapist,
            DayOfWeek = Enum.Parse<DayOfWeek>(day.ToString()),
            StartTime = TimeOnly.Parse(start),
            EndTime = TimeOnly.Parse(end),
            TimeSlot = $"{start}-{end}",
            MaxSlotsPerDay = maxSlots,
            MaxPatients = maxSlots,
            IsActive = true
        };

    private static async Task SeedPatientsAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Patients.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Patients.AddRange(
            new Patient
            {
                Uhid = "SAH-2026-00001",
                FirstName = "Nimali",
                LastName = "Perera",
                DateOfBirth = new DateOnly(1986, 3, 14),
                Gender = Gender.Female,
                Phone = "0771000001",
                Email = "nimali.perera@example.local",
                Address = "Kandy",
                Prakriti = DoshaType.Vata | DoshaType.Pitta
            },
            new Patient
            {
                Uhid = "SAH-2026-00002",
                FirstName = "Kasun",
                LastName = "Fernando",
                DateOfBirth = new DateOnly(1979, 11, 2),
                Gender = Gender.Male,
                Phone = "0771000002",
                Email = "kasun.fernando@example.local",
                Address = "Colombo",
                Prakriti = DoshaType.Kapha
            },
            new Patient
            {
                Uhid = "SAH-2026-00003",
                FirstName = "Ishara",
                LastName = "Jayasinghe",
                DateOfBirth = new DateOnly(1994, 7, 21),
                Gender = Gender.Female,
                Phone = "0771000003",
                Email = "ishara.j@example.local",
                Address = "Galle",
                Prakriti = DoshaType.Pitta
            },
            new Patient
            {
                Uhid = "SAH-2026-00004",
                FirstName = "Ruwan",
                LastName = "Silva",
                DateOfBirth = new DateOnly(1968, 1, 9),
                Gender = Gender.Male,
                Phone = "0771000004",
                Address = "Matale",
                Prakriti = DoshaType.Vata
            },
            new Patient
            {
                Uhid = "SAH-2026-00005",
                FirstName = "Sanduni",
                LastName = "Wickramasinghe",
                DateOfBirth = new DateOnly(1991, 5, 30),
                Gender = Gender.Female,
                Phone = "0771000005",
                Address = "Kurunegala",
                Prakriti = DoshaType.Kapha | DoshaType.Pitta
            },
            new Patient
            {
                Uhid = "SAH-2026-00006",
                FirstName = "Tharindu",
                LastName = "Bandara",
                DateOfBirth = new DateOnly(1983, 12, 18),
                Gender = Gender.Male,
                Phone = "0771000006",
                Address = "Anuradhapura",
                Prakriti = DoshaType.Vata | DoshaType.Kapha
            });
    }

    private static async Task SeedTreatmentSchedulesAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.TreatmentSchedules.AnyAsync(cancellationToken))
        {
            return;
        }

        var treatments = await db.Treatments.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        foreach (var treatment in treatments.Where(t => t.DurationMinutes > 0))
        {
            db.TreatmentSchedules.AddRange(
                new TreatmentSchedule
                {
                    TreatmentId = treatment.Id,
                    DayOfWeek = DayOfWeek.Monday,
                    TimeSlot = "09:00-10:00",
                    MaxPatients = 8
                },
                new TreatmentSchedule
                {
                    TreatmentId = treatment.Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    TimeSlot = "14:00-15:00",
                    MaxPatients = 6
                });
        }
    }

    private static async Task SeedWardsAndBedsAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Wards.AnyAsync(cancellationToken))
        {
            return;
        }

        var femaleWard = new Ward
        {
            Name = "Female Ayurveda Ward",
            NameSinhala = "ස්ත්‍රී ආයුර්වේද වාට්ටුව",
            Gender = WardGender.Female,
            TotalCapacity = 12
        };
        var maleWard = new Ward
        {
            Name = "Male Ayurveda Ward",
            NameSinhala = "පුරුෂ ආයුර්වේද වාට්ටුව",
            Gender = WardGender.Male,
            TotalCapacity = 10
        };

        var occupiedFemale = new HashSet<string> { "A-01", "A-02", "A-05" };
        var occupiedMale = new HashSet<string> { "B-01", "B-04" };

        for (var i = 1; i <= 12; i++)
        {
            var label = $"A-{i:D2}";
            femaleWard.Beds.Add(new Bed { BedLabel = label, IsOccupied = occupiedFemale.Contains(label) });
        }

        for (var i = 1; i <= 10; i++)
        {
            var label = $"B-{i:D2}";
            maleWard.Beds.Add(new Bed { BedLabel = label, IsOccupied = occupiedMale.Contains(label) });
        }

        db.Wards.AddRange(femaleWard, maleWard);
    }

    private static async Task SeedAdmissionRequestsAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.AdmissionRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var doctor = await db.Users.FirstAsync(x => x.Role == UserRole.Doctor, cancellationToken);
        var patients = await db.Patients.OrderBy(x => x.Uhid).ToListAsync(cancellationToken);
        var femaleWard = await db.Wards.Include(w => w.Beds).FirstAsync(w => w.Gender == WardGender.Female, cancellationToken);
        var maleWard = await db.Wards.Include(w => w.Beds).FirstAsync(w => w.Gender == WardGender.Male, cancellationToken);
        var decidedAt = new DateTimeOffset(2026, 9, 8, 11, 0, 0, TimeSpan.Zero);

        var nimali = patients.First(p => p.Uhid == "SAH-2026-00001");
        var kasun = patients.First(p => p.Uhid == "SAH-2026-00002");
        var ishara = patients.First(p => p.Uhid == "SAH-2026-00003");
        var ruwan = patients.First(p => p.Uhid == "SAH-2026-00004");
        var sanduni = patients.First(p => p.Uhid == "SAH-2026-00005");
        var tharindu = patients.First(p => p.Uhid == "SAH-2026-00006");

        var occupiedFemaleBeds = femaleWard.Beds.Where(b => b.IsOccupied).OrderBy(b => b.BedLabel).ToList();
        var occupiedMaleBeds = maleWard.Beds.Where(b => b.IsOccupied).OrderBy(b => b.BedLabel).ToList();

        AdmissionRequest Approved(Patient patient, Ward ward, Bed bed, string reason, DateOnly date, bool byAgent) =>
            new()
            {
                PatientId = patient.Id,
                WardId = ward.Id,
                BedId = bed.Id,
                Reason = reason,
                PreferredDate = date,
                Status = AdmissionRequestStatus.Approved,
                RequestedByAgent = byAgent,
                DecidedBy = doctor.Id,
                DecidedAt = decidedAt
            };

        db.AdmissionRequests.AddRange(
            Approved(nimali, femaleWard, occupiedFemaleBeds[0], "In-patient Panchakarma (Snehana / Swedana).", new DateOnly(2026, 9, 7), false),
            Approved(ishara, femaleWard, occupiedFemaleBeds[1], "Post-procedure observation after Shirodhara.", new DateOnly(2026, 9, 8), true),
            Approved(sanduni, femaleWard, occupiedFemaleBeds[2], "Nasya course with overnight stay.", new DateOnly(2026, 9, 9), false),
            Approved(kasun, maleWard, occupiedMaleBeds[0], "Supervised Basti course.", new DateOnly(2026, 9, 6), false),
            Approved(tharindu, maleWard, occupiedMaleBeds[1], "Kizhi series, male ward.", new DateOnly(2026, 9, 8), true),
            new AdmissionRequest
            {
                PatientId = ruwan.Id,
                WardId = maleWard.Id,
                Reason = "Requested bed for Kizhi series.",
                PreferredDate = new DateOnly(2026, 9, 12),
                Status = AdmissionRequestStatus.Pending,
                RequestedByAgent = true
            },
            new AdmissionRequest
            {
                PatientId = ruwan.Id,
                WardId = maleWard.Id,
                Reason = "Elective stay declined — outpatient Abhyanga sufficient.",
                PreferredDate = new DateOnly(2026, 9, 4),
                Status = AdmissionRequestStatus.Rejected,
                RequestedByAgent = false,
                DecidedBy = doctor.Id,
                DecidedAt = decidedAt.AddDays(-2)
            });
    }

    private static async Task SeedAppointmentsAsync(HospitalDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Appointments.AnyAsync(cancellationToken))
        {
            return;
        }

        var doctor = await db.Users.FirstAsync(x => x.Role == UserRole.Doctor, cancellationToken);
        var patients = await db.Patients.OrderBy(x => x.Uhid).ToListAsync(cancellationToken);
        var abhyanga = await db.Treatments.FirstAsync(t => t.Name == "Abhyanga", cancellationToken);
        var shirodhara = await db.Treatments.FirstAsync(t => t.Name == "Shirodhara", cancellationToken);
        var consultation = await db.Treatments.FirstAsync(t => t.Name == "Initial Consultation", cancellationToken);
        var abhyangaMonday = await db.TreatmentSchedules.FirstAsync(
            s => s.TreatmentId == abhyanga.Id && s.TimeSlot == "09:00-10:00", cancellationToken);

        var nimali = patients.First(p => p.Uhid == "SAH-2026-00001");
        var kasun = patients.First(p => p.Uhid == "SAH-2026-00002");
        var ishara = patients.First(p => p.Uhid == "SAH-2026-00003");
        var ruwan = patients.First(p => p.Uhid == "SAH-2026-00004");
        var decidedAt = new DateTimeOffset(2026, 9, 9, 8, 30, 0, TimeSpan.Zero);

        db.Appointments.AddRange(
            new Appointment
            {
                PatientId = nimali.Id,
                TreatmentId = abhyanga.Id,
                ScheduleId = abhyangaMonday.Id,
                RequestedDate = new DateOnly(2026, 9, 14),
                RequestedTimeSlot = "09:00-10:00",
                Status = AppointmentStatus.Pending
            },
            new Appointment
            {
                PatientId = kasun.Id,
                TreatmentId = shirodhara.Id,
                RequestedDate = new DateOnly(2026, 9, 15),
                RequestedTimeSlot = "14:00-15:00",
                Status = AppointmentStatus.Approved,
                DecidedBy = doctor.Id,
                DecidedAt = decidedAt
            },
            new Appointment
            {
                PatientId = ishara.Id,
                TreatmentId = consultation.Id,
                RequestedDate = new DateOnly(2026, 9, 10),
                RequestedTimeSlot = "08:00-08:30",
                Status = AppointmentStatus.Rejected,
                DecidedBy = doctor.Id,
                DecidedAt = decidedAt.AddHours(-20)
            },
            new Appointment
            {
                PatientId = ruwan.Id,
                TreatmentId = abhyanga.Id,
                RequestedDate = new DateOnly(2026, 9, 8),
                RequestedTimeSlot = "09:00-10:00",
                Status = AppointmentStatus.Completed,
                DecidedBy = doctor.Id,
                DecidedAt = decidedAt.AddDays(-1)
            },
            new Appointment
            {
                PatientId = nimali.Id,
                TreatmentId = shirodhara.Id,
                RequestedDate = new DateOnly(2026, 9, 11),
                RequestedTimeSlot = "11:00-12:00",
                Status = AppointmentStatus.Cancelled
            });
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
        var adminUser = await db.Users.FirstAsync(x => x.Email == "admin@smartayurveda.local", cancellationToken);
        var doctorUser = await db.Users.FirstAsync(x => x.Email == "doctor@smartayurveda.local", cancellationToken);

        var abhyanga = await db.Treatments.FirstAsync(x => x.Name == "Abhyanga", cancellationToken);
        var shirodhara = await db.Treatments.FirstAsync(x => x.Name == "Shirodhara", cancellationToken);
        var consultation = await db.Treatments.FirstAsync(x => x.Name == "Initial Consultation", cancellationToken);

        var appointment = await db.Appointments
            .FirstOrDefaultAsync(x => x.PatientId == meera.Id && x.Status == AppointmentStatus.Completed, cancellationToken);
        if (appointment is null)
        {
            var requestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
            appointment = new Appointment
            {
                PatientId = meera.Id,
                TreatmentId = abhyanga.Id,
                RequestedDate = requestedDate,
                RequestedTimeSlot = "09:00-10:00",
                Status = AppointmentStatus.Completed,
                DecidedBy = doctorUser.Id,
                DecidedAt = DateTimeOffset.UtcNow.AddDays(-5)
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
        const string meeraUhid = "SAH-2026-00007";
        const string arjunUhid = "SAH-2026-00008";

        var existing = await db.Patients
            .Where(x => x.Uhid == meeraUhid || x.Uhid == arjunUhid
                || x.Email == "meera.nair@example.local"
                || x.Email == "arjun.menon@example.local")
            .ToListAsync(cancellationToken);

        var meera = existing.FirstOrDefault(x => x.Email == "meera.nair@example.local" || x.Uhid == meeraUhid);
        var arjun = existing.FirstOrDefault(x => x.Email == "arjun.menon@example.local" || x.Uhid == arjunUhid);

        if (meera is null)
        {
            meera = new Patient
            {
                Uhid = meeraUhid,
                FirstName = "Meera",
                LastName = "Nair",
                DateOfBirth = new DateOnly(1988, 4, 12),
                Gender = Gender.Female,
                Phone = "9876500001",
                Email = "meera.nair@example.local",
                Prakriti = DoshaType.Pitta,
                Vikriti = DoshaType.Pitta | DoshaType.Vata
            };
            db.Patients.Add(meera);
        }

        if (arjun is null)
        {
            arjun = new Patient
            {
                Uhid = arjunUhid,
                FirstName = "Arjun",
                LastName = "Menon",
                DateOfBirth = new DateOnly(1991, 11, 3),
                Gender = Gender.Male,
                Phone = "9876500002",
                Email = "arjun.menon@example.local",
                Prakriti = DoshaType.Vata | DoshaType.Kapha,
                Vikriti = DoshaType.Vata
            };
            db.Patients.Add(arjun);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new[] { meera, arjun };
    }
}