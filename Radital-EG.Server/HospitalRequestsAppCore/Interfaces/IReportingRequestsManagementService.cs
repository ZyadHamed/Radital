using HospitalRequestsAppCore.DTOs;

namespace HospitalRequestsAppCore.Interfaces
{
    /// <summary>
    /// Defines the application-level contract for managing reporting requests
    /// from the Hospital side.
    /// </summary>
    public interface IReportingRequestsManagementService
    {
        /// <summary>
        /// US-01: Creates a new reporting request along with a new patient record
        /// (if the patient does not already exist) and the associated medical image.
        /// </summary>
        Task<ReportingRequestResponseDto> CreateRequestAsync(CreateReportingRequestDto dto, Guid requestedById);

        /// <summary>
        /// US-02: Returns all reporting requests so the technician can track their statuses.
        /// </summary>
        Task<IEnumerable<ReportingRequestResponseDto>> GetAllRequestsAsync(Guid requestedById);

        /// <summary>
        /// US-02: Returns a single reporting request by Id for detailed status tracking.
        /// </summary>
        Task<ReportingRequestResponseDto?> GetRequestByIdAsync(Guid id, Guid requestedById);
    }
}
