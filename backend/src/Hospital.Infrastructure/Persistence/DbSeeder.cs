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

        await SeedPatientsAsync(db, cancellationToken);
        await SeedTreatmentSchedulesAsync(db, cancellationToken);
        await SeedWardsAndBedsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SeedAdmissionRequestsAsync(db, cancellationToken);
        await SeedAppointmentsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database schema ready and seed data applied.");
    }

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
}
