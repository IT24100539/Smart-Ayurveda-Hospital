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
        logger.LogInformation("Database schema ready and seed data applied.");
    }
}
