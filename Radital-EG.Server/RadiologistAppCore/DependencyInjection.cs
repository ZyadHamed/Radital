using Domain.People;
using RadiologistAppCore.Interfaces;
using RadiologistAppCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RadiologistAppCore
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all application-layer services defined in HospitalRequestsAppCore.
        /// </summary>
        public static IServiceCollection AddAppCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IWorkloadManagementService, WorkloadManagementService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IPasswordHasher<Radiologist>, PasswordHasher<Radiologist>>();
            services.AddHostedService<EmergencyEscalationService>();
            return services;
        }

    }
}
