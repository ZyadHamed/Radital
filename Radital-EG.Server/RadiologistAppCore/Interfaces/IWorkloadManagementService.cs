using RadiologistAppCore.DTOs;

namespace RadiologistAppCore.Interfaces
{
    public interface IWorkloadManagementService
    {
        /// <summary>
        /// Fetches all reporting requests assigned to the authenticated radiologist.
        /// </summary>
        Task<IEnumerable<RadiologistRequestResponseDto>> GetAssignedRequestsAsync(Guid radiologistId);

        /// <summary>
        /// Fetches a single assigned request by Id.
        /// Throws <see cref="KeyNotFoundException"/> if not found.
        /// Throws <see cref="UnauthorizedAccessException"/> if not assigned to this radiologist.
        /// </summary>
        Task<RadiologistRequestResponseDto> GetAssignedRequestByIdAsync(Guid requestId, Guid radiologistId);

        /// <summary>
        /// Updates the status of an assigned request (e.g., InProgress).
        /// Throws <see cref="KeyNotFoundException"/> if not found.
        /// Throws <see cref="UnauthorizedAccessException"/> if not assigned to this radiologist.
        /// </summary>
        Task<RadiologistRequestResponseDto> UpdateRequestStatusAsync(
            Guid requestId, Guid radiologistId, Domain.ReportingRequestStatusEnum newStatus);

        /// <summary>
        /// Calculates a real-time match score for every available radiologist
        /// relative to the given reporting request.
        /// <para>
        /// The composite score (0–100) is a weighted sum of:
        /// <list type="bullet">
        ///   <item>Specialty alignment  – 50 %</item>
        ///   <item>Current queue size   – 30 %</item>
        ///   <item>Historical turnaround – 20 %</item>
        /// </list>
        /// </para>
        /// Results are returned in descending score order (best match first).
        /// Throws <see cref="KeyNotFoundException"/> if the request is not found.
        /// </summary>
        Task<IEnumerable<DoctorMatchScoreDto>> GetDoctorMatchScoresAsync(Guid requestId);

        Task<RadiologistRequestResponseDto> AcceptRequestAsync(Guid requestId, Guid radiologistId);

        Task AssignEmergencyRequestAsync(Guid requestId, Guid radiologistId);

    }
}
