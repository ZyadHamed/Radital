using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Domain;
using Domain.People;

namespace Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        // DbSets
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Radiologist> Radiologists { get; set; }
        public DbSet<HospitalStaffMember> HospitalStaffMembers { get; set; }
        public DbSet<MedicalImage> MedicalImages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportingRequest> ReportingRequests { get; set; }
        public DbSet<AvaliabilityTime> AvaliabilityTimes { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Inheritance (TPT)
            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<Radiologist>().ToTable("Radiologists");
            modelBuilder.Entity<HospitalStaffMember>().ToTable("HospitalStaffMembers");

            // Person
            modelBuilder.Entity<Person>(entity =>
            {
                entity.Property(p => p.Name).HasMaxLength(100);
                entity.Property(p => p.PhoneNumber).HasMaxLength(20);
                entity.Property(p => p.Address).HasMaxLength(250);
            });

            // Radiologist
            modelBuilder.Entity<Radiologist>(entity =>
            {
                entity.Property(r => r.Email).HasMaxLength(150);
                entity.Property(r => r.HashedPassword).HasMaxLength(500);
                entity.Property(r => r.LoginId).HasMaxLength(250);
            });

            // HospitalStaffMember
            modelBuilder.Entity<HospitalStaffMember>(entity =>
            {
                entity.Property(h => h.Email).HasMaxLength(150);
                entity.Property(h => h.HashedPassword).HasMaxLength(500);
            });

            // Patient
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.Property(p => p.MedicalHistory).HasMaxLength(2000);
                entity.Property(p => p.Notes).HasMaxLength(1000);
            });

            // MedicalImage
            modelBuilder.Entity<MedicalImage>(entity =>
            {
                entity.Property(m => m.StorageReference).HasMaxLength(2000);
            });

            // Report
            modelBuilder.Entity<Report>(entity =>
            {
                entity.Property(r => r.ClinicalHistory).HasMaxLength(2000);
                entity.Property(r => r.Technique).HasMaxLength(1000);
                entity.Property(r => r.Findings).HasMaxLength(2000);
                entity.Property(r => r.Impression).HasMaxLength(2000);
                entity.Property(r => r.Recommendation).HasMaxLength(2000);
                entity.Property(r => r.StorageReference).HasMaxLength(2000);
            });

            // ReportingRequest
            modelBuilder.Entity<ReportingRequest>(entity =>
            {
                entity.Property(r => r.SuggestedDepartment).HasMaxLength(100);
                entity.Property(r => r.EmergencyJustification).HasMaxLength(1000);

                entity.HasOne(r => r.AssignedRadiologist)
                      .WithMany()
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Report)
                      .WithOne()
                      .HasForeignKey<ReportingRequest>(r => r.ReportId)  // Specify FK
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.Image)
                      .WithMany()
                      .IsRequired(true)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.RequestedBy)
                      .WithMany()
                      .IsRequired(true)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(r => r.EscalationHistory)
                      .HasConversion(
                          v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                          v => string.IsNullOrWhiteSpace(v)   // ← handle empty string from existing rows
                              ? new List<Guid>()
                              : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<Guid>()
                      )
                      .HasColumnType("longtext")
                      .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
                          (c1, c2) => c1.SequenceEqual(c2),
                          c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                          c => c.ToList()
                      ));
            });
        }
    }
}
