using Domain;
using Domain.People;
using Infrastructure.Implementations;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the database context and all repository implementations
        /// defined in the Infrastructure layer.
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Database context ──────────────────────────────────────────────
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySQL(configuration.GetConnectionString("DefaultConnection")!));

            // ── Repositories ──────────────────────────────────────────────────
            services.AddScoped<IRepository<Patient>, Repository<Patient>>();
            services.AddScoped<IRepository<Radiologist>, Repository<Radiologist>>();
            services.AddScoped<IRepository<HospitalStaffMember>, Repository<HospitalStaffMember>>();
            services.AddScoped<IRepository<MedicalImage>, Repository<MedicalImage>>();
            services.AddScoped<IRepository<Report>, Repository<Report>>();
            services.AddScoped<IRepository<ReportingRequest>, Repository<ReportingRequest>>();
            services.AddScoped<IRepository<AvaliabilityTime>, Repository<AvaliabilityTime>>();

            return services;
        }
    }
}
