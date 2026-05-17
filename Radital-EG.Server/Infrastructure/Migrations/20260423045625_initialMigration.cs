using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HospitalStaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    LoginId = table.Column<string>(type: "longtext", nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    HashedPassword = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalStaffMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalStaffMembers_Person_Id",
                        column: x => x.Id,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    MedicalHistory = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Person_Id",
                        column: x => x.Id,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Radiologists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    LoginId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    HashedPassword = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Speciality = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Radiologists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Radiologists_Person_Id",
                        column: x => x.Id,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicalImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    PatientId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ImageModality = table.Column<int>(type: "int", nullable: false),
                    StorageReference = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalImages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AvaliabilityTimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    RadiologistId = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliabilityTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliabilityTimes_Radiologists_RadiologistId",
                        column: x => x.RadiologistId,
                        principalTable: "Radiologists",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ClinicalHistory = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Technique = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Findings = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Impression = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StorageReference = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Radiologists_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Radiologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReportingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RequestedById = table.Column<Guid>(type: "char(36)", nullable: false),
                    ImageId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SuggestedDepartment = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmissionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedRadiologistId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReportId = table.Column<Guid>(type: "char(36)", nullable: true),
                    IsEmergency = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmergencyJustification = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingRequests_HospitalStaffMembers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "HospitalStaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportingRequests_MedicalImages_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MedicalImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportingRequests_Radiologists_AssignedRadiologistId",
                        column: x => x.AssignedRadiologistId,
                        principalTable: "Radiologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportingRequests_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliabilityTimes_RadiologistId",
                table: "AvaliabilityTimes",
                column: "RadiologistId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalImages_PatientId",
                table: "MedicalImages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRequests_AssignedRadiologistId",
                table: "ReportingRequests",
                column: "AssignedRadiologistId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRequests_ImageId",
                table: "ReportingRequests",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRequests_ReportId",
                table: "ReportingRequests",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRequests_RequestedById",
                table: "ReportingRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AuthorId",
                table: "Reports",
                column: "AuthorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaliabilityTimes");

            migrationBuilder.DropTable(
                name: "ReportingRequests");

            migrationBuilder.DropTable(
                name: "HospitalStaffMembers");

            migrationBuilder.DropTable(
                name: "MedicalImages");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Radiologists");

            migrationBuilder.DropTable(
                name: "Person");
        }
    }
}
