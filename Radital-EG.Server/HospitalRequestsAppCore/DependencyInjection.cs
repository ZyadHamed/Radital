using Domain.People;
using HospitalRequestsAppCore.Interfaces;
using HospitalRequestsAppCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace HospitalRequestsAppCore
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all application-layer services defined in HospitalRequestsAppCore.
        /// </summary>
        public static IServiceCollection AddAppCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IReportingRequestsManagementService, ReportingRequestsManagementService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPasswordHasher<HospitalStaffMember>, PasswordHasher<HospitalStaffMember>>();
            services.AddScoped<IPasswordHasher<Radiologist>, PasswordHasher<Radiologist>>();

            return services;
        }

    }
}
