using Domain;
using Domain.People;
using HospitalRequestsAppCore.DTOs;
using HospitalRequestsAppCore.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalRequestsAppCore.Services
{
    /// <summary>
    /// Implements the business logic for managing reporting requests on the Hospital side.
    /// Covers US-01 (create) and US-02 (status tracking).
    /// </summary>
    public class ReportingRequestsManagementService : IReportingRequestsManagementService
    {
        private readonly IRepository<ReportingRequest> _requestRepository;
        private readonly IRepository<HospitalStaffMember> _hospitalStaffMemberRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly IRepository<MedicalImage> _imageRepository;
        private readonly IRepository<Radiologist> _radiologistRepository;
        private readonly ILogger<ReportingRequestsManagementService> _logger;

        public ReportingRequestsManagementService(
            IRepository<ReportingRequest> requestRepository,
            IRepository<HospitalStaffMember> hospitalStaffMemberRepository,
            IRepository<Patient> patientRepository,
            IRepository<MedicalImage> imageRepository,
            IRepository<Radiologist> radiologistRepository,
            ILogger<ReportingRequestsManagementService> logger)
        {
            _requestRepository = requestRepository;
            _hospitalStaffMemberRepository = hospitalStaffMemberRepository;
            _patientRepository = patientRepository;
            _imageRepository = imageRepository;
            _radiologistRepository = radiologistRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ReportingRequestResponseDto> CreateRequestAsync(CreateReportingRequestDto dto, Guid requestedById)
        {
            _logger.LogInformation("Creating new reporting request for patient: {PatientName}", dto.PatientName);

            var hospitalStaffMemberWhoRequestedTheImage = await _hospitalStaffMemberRepository.GetByIdAsync(requestedById);

            Radiologist? assignedRadiologist = null;
            assignedRadiologist = await _radiologistRepository.GetByIdAsync(dto.AssignedRadiologistId);
            if (assignedRadiologist is null)
            {
                throw new KeyNotFoundException(
                    $"Radiologist with Id '{dto.AssignedRadiologistId}' was not found.");
            }

            _logger.LogInformation("Assigning radiologist {RadiologistId} to request",
                dto.AssignedRadiologistId);

            // 1. Create the patient record
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = dto.PatientName,
                DateOfBirth = dto.PatientDateOfBirth,
                PhoneNumber = dto.PatientPhoneNumber,
                Gender = dto.PatientGender,
                Address = dto.PatientAddress,
                MedicalHistory = dto.PatientMedicalHistory,
                Notes = dto.PatientNotes
            };
            await _patientRepository.InsertAsync(patient);
            await _patientRepository.CommitAsync(Guid.Empty);

            // 2. Create the medical image record
            var medicalImage = new MedicalImage
            {
                Id = Guid.NewGuid(),
                Patient = patient,
                ImageModality = dto.ImageModality,
                StorageReference = dto.StorageReference
            };
            await _imageRepository.InsertAsync(medicalImage);
            await _imageRepository.CommitAsync(Guid.Empty);

            // 3. Create the reporting request
            var reportingRequest = new ReportingRequest
            {
                RequestedBy = hospitalStaffMemberWhoRequestedTheImage,
                Id = Guid.NewGuid(),
                Image = medicalImage,
                SuggestedDepartment = dto.SuggestedDepartment,
                Status = ReportingRequestStatusEnum.Pending,
                SubmissionTime = DateTime.UtcNow,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                IsEmergency = dto.IsEmergency,
                AssignedRadiologist = assignedRadiologist,
                AssignedAt = DateTime.UtcNow,
                EmergencyJustification = dto.EmergencyJustification
            };
            await _requestRepository.InsertAsync(reportingRequest);
            await _requestRepository.CommitAsync(Guid.Empty);

            _logger.LogInformation("Reporting request created with Id: {RequestId}", reportingRequest.Id);

            return MapToResponseDto(reportingRequest);
        }

        public async Task<IEnumerable<ReportingRequestResponseDto>> GetAllRequestsAsync(Guid requestedById)
        {
            _logger.LogInformation("Fetching all reporting requests for staff member: {StaffId}", requestedById);

            var requests = await _requestRepository.FindNestedSearchAsync(
                filter: request => request.RequestedBy.Id == requestedById,
                maxLevel: 5
            );

            return requests.Select(MapToResponseDto);
        }

        /// <inheritdoc/>
        public async Task<ReportingRequestResponseDto?> GetRequestByIdAsync(Guid id, Guid requestedById)
        {
            _logger.LogInformation("Fetching reporting request with Id: {RequestId}", id);

            var request = await _requestRepository.GetByIdNestedSearchAsync(id, maxLevel: 5);

            if (request is null)
            {
                _logger.LogWarning("Reporting request with Id: {RequestId} was not found", id);
                throw new KeyNotFoundException($"Reporting request with Id '{id}' was not found.");
            }

            if (request.RequestedBy.Id != requestedById)
            {
                _logger.LogWarning(
                    "Staff member {StaffId} attempted to access reporting request {RequestId} owned by {OwnerId}",
                    requestedById, id, request.RequestedBy.Id);
                throw new UnauthorizedAccessException("You are not authorized to view this reporting request.");
            }

            return MapToResponseDto(request);
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private static ReportingRequestResponseDto MapToResponseDto(ReportingRequest request)
        {
            return new ReportingRequestResponseDto
            {
                Id = request.Id,

                // --- Requested By ---
                RequestedById = request.RequestedBy?.Id ?? Guid.Empty,
                RequestedByName = request.RequestedBy?.Name ?? string.Empty,

                // --- Patient ---
                PatientId = request.Image?.Patient?.Id ?? Guid.Empty,
                PatientName = request.Image?.Patient?.Name ?? string.Empty,
                PatientDateOfBirth = request.Image?.Patient?.DateOfBirth ?? default,
                PatientPhoneNumber = request.Image?.Patient?.PhoneNumber ?? string.Empty,
                PatientGender = request.Image?.Patient?.Gender ?? default,
                PatientAddress = request.Image?.Patient?.Address ?? string.Empty,
                PatientMedicalHistory = request.Image?.Patient?.MedicalHistory,
                PatientNotes = request.Image?.Patient?.Notes,

                // --- Medical Image ---
                MedicalImageId = request.Image?.Id ?? Guid.Empty,
                ImageModality = request.Image?.ImageModality ?? default,
                StorageReference = request.Image?.StorageReference ?? string.Empty,

                // --- Request details ---
                SuggestedDepartment = request.SuggestedDepartment ?? string.Empty,
                Priority = request.Priority,
                IsEmergency = request.IsEmergency,
                EmergencyJustification = request.EmergencyJustification,

                // --- Assigned Radiologist ---
                AssignedRadiologistId = request.AssignedRadiologist?.Id,
                AssignedRadiologistName = request.AssignedRadiologist?.Name,

                // --- Report ---
                ReportId = request.ReportId,

                // --- Status ---
                Status = request.Status,
                SubmissionTime = request.SubmissionTime,
                DueDate = request.DueDate
            };
        }

    }
}
