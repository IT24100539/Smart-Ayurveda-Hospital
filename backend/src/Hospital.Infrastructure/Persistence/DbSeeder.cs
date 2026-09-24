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

        db.Treatments.AddRange(panchakarma, shirodhara, herbalSteam, nasya, general);

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
            Slot(general, nimali, Weekday.Friday, "14:00", "16:00", 10));
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
            DayOfWeek = day,
            StartTime = TimeOnly.Parse(start),
            EndTime = TimeOnly.Parse(end),
            MaxSlotsPerDay = maxSlots,
            IsActive = true
        };
}
