using RadiologistAppCore.DTOs;

namespace RadiologistAppCore.Interfaces
{
    public interface IReportingService
    {
        /// <summary>
        /// Creates a report for a given reporting request, generates the PDF,
        /// and links the report back to the request.
        /// Throws <see cref="KeyNotFoundException"/> if request not found.
        /// Throws <see cref="UnauthorizedAccessException"/> if not assigned to this radiologist.
        /// Throws <see cref="InvalidOperationException"/> if request already has a report.
        /// </summary>
        Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid radiologistId);

        /// <summary>
        /// Fetches a report by its Id.
        /// Throws <see cref="KeyNotFoundException"/> if not found.
        /// Throws <see cref="UnauthorizedAccessException"/> if not the author.
        /// </summary>
        Task<ReportResponseDto> GetReportByIdAsync(Guid reportId, Guid radiologistId);

        /// <summary>
        /// Fetches all reports authored by this radiologist.
        /// </summary>
        Task<IEnumerable<ReportResponseDto>> GetAllReportsByRadiologistAsync(Guid radiologistId);

        /// <summary>
        /// Returns the PDF file bytes for a given report.
        /// Throws <see cref="KeyNotFoundException"/> if not found.
        /// Throws <see cref="UnauthorizedAccessException"/> if not the author.
        /// </summary>
        Task<byte[]> GetReportPdfAsync(Guid reportId, Guid radiologistId);
    }
}