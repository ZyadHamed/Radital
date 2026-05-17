using Domain;
using Domain.People;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            var staffHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<HospitalStaffMember>>();
            var radiologistHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Radiologist>>();

            await SeedRadiologist(context, radiologistHasher);
            await SeedTechnician(context, staffHasher);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRadiologist(
            ApplicationDbContext context,
            IPasswordHasher<Radiologist> passwordHasher)
        {
            if (await context.Radiologists.AnyAsync(r => r.LoginId == "RAD1234567"))
                return;

            var radiologist = new Radiologist
            {
                Id = Guid.NewGuid(),
                Name = "Test Radiologist",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "01234567890",
                Gender = GenderEnum.Male,
                Address = "Cairo, Egypt",
                LoginId = "RAD1234567",
                Email = "testrad@gmail.com",
                HashedPassword = string.Empty,
                Status = StatusEnum.Active,
                Speciality = SpecialityEnum.General,
                AvailabilityTimes = new List<AvaliabilityTime>
                {
                    new AvaliabilityTime
                    {
                        Id = Guid.NewGuid(),
                        Day = DayOfWeek.Monday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0)
                    },
                    new AvaliabilityTime
                    {
                        Id = Guid.NewGuid(),
                        Day = DayOfWeek.Wednesday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0)
                    }
                }
            };

            radiologist.HashedPassword = passwordHasher.HashPassword(radiologist, "1234");

            await context.Radiologists.AddAsync(radiologist);
        }

        private static async Task SeedTechnician(
            ApplicationDbContext context,
            IPasswordHasher<HospitalStaffMember> passwordHasher)
        {
            if (await context.HospitalStaffMembers.AnyAsync(h => h.LoginId == "TEC1234567"))
                return;

            var technician = new HospitalStaffMember
            {
                Id = Guid.NewGuid(),
                Name = "Test Technician",
                DateOfBirth = new DateTime(1992, 5, 15),
                PhoneNumber = "01098765432",
                Gender = GenderEnum.Male,
                Address = "Cairo, Egypt",
                LoginId = "TEC1234567",
                Email = "testtech@gmail.com",
                HashedPassword = string.Empty,
                Department = DepartmentsEnum.Radiology,
                Role = RolesEnum.Technician
            };

            technician.HashedPassword = passwordHasher.HashPassword(technician, "1234");

            await context.HospitalStaffMembers.AddAsync(technician);
        }
    }
}