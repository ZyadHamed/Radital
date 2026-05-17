using Domain;
using Domain.People;
using RadiologistAppCore.DTOs;
using RadiologistAppCore.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using RadiologistAppCore.Hubs;

namespace RadiologistAppCore.Services
{
    /// <summary>
    /// Scoring weights and tuning constants for <see cref="WorkloadManagementService.GetDoctorMatchScoresAsync"/>.
    /// Centralised here so they can be moved to IOptions/config without touching scoring logic.
    /// </summary>
    internal static class MatchScoreWeights
    {
        // Weights (must sum to 1.0)
        public const double SpecialtyWeight  = 0.50;
        public const double QueueWeight      = 0.30;
        public const double TurnaroundWeight = 0.20;

        // Queue saturation ceiling: a radiologist with this many active cases scores 0 on queue.
        public const int MaxQueueSize = 10;

        // Turnaround saturation ceiling in hours: at or beyond this average, turnaround score = 0.
        public const double MaxTurnaroundHours = 48.0;

        // Specialty score levels
        public const double ExactSpecialtyScore   = 100.0;
        public const double RelatedSpecialtyScore =  50.0;
        public const double NoSpecialtyScore      =   0.0;
    }

    public class WorkloadManagementService : IWorkloadManagementService
    {
        private readonly IRepository<ReportingRequest> _requestRepository;
        private readonly IRepository<Radiologist>      _radiologistRepository;
        private readonly ILogger<WorkloadManagementService> _logger;
        private readonly IHubContext<RadiologistHub> _hubContext;

        public WorkloadManagementService(
            IRepository<ReportingRequest> requestRepository,
            IRepository<Radiologist>      radiologistRepository,
            ILogger<WorkloadManagementService> logger,
            IHubContext<RadiologistHub> hubContext)
        {
            _requestRepository     = requestRepository;
            _radiologistRepository = radiologistRepository;
            _logger                = logger;
            _hubContext = hubContext;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Existing methods
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<RadiologistRequestResponseDto>> GetAssignedRequestsAsync(
            Guid radiologistId)
        {
            _logger.LogInformation("Fetching assigned requests for radiologist: {RadiologistId}", radiologistId);

            var radiologist = await _radiologistRepository.GetByIdAsync(radiologistId);
            if (radiologist is null)
                throw new UnauthorizedAccessException("Radiologist not found.");

            var requests = await _requestRepository.FindNestedSearchAsync(
                filter:   r => r.AssignedRadiologist.Id == radiologistId,
                maxLevel: 2
            );

            return requests.Select(MapToResponseDto);
        }

        public async Task<RadiologistRequestResponseDto> GetAssignedRequestByIdAsync(
            Guid requestId, Guid radiologistId)
        {
            _logger.LogInformation("Fetching request {RequestId} for radiologist {RadiologistId}",
                requestId, radiologistId);

            var request = await _requestRepository.GetByIdNestedSearchAsync(requestId, maxLevel: 2);

            if (request is null)
                throw new KeyNotFoundException($"Reporting request with Id '{requestId}' was not found.");

            if (request.AssignedRadiologist?.Id != radiologistId)
                throw new UnauthorizedAccessException("You are not assigned to this reporting request.");

            return MapToResponseDto(request);
        }

        public async Task<RadiologistRequestResponseDto> UpdateRequestStatusAsync(
            Guid requestId, Guid radiologistId, ReportingRequestStatusEnum newStatus)
        {
            _logger.LogInformation(
                "Updating status of request {RequestId} to {Status} by radiologist {RadiologistId}",
                requestId, newStatus, radiologistId);

            var request = await _requestRepository.GetByIdNestedSearchAsync(requestId, maxLevel: 2);

            if (request is null)
                throw new KeyNotFoundException($"Reporting request with Id '{requestId}' was not found.");

            if (request.AssignedRadiologist?.Id != radiologistId)
                throw new UnauthorizedAccessException("You are not assigned to this reporting request.");

            request.Status = newStatus;

            await _requestRepository.UpdateAsync(request);
            await _requestRepository.CommitAsync(Guid.Empty);

            _logger.LogInformation("Request {RequestId} status updated to {Status}", requestId, newStatus);

            return MapToResponseDto(request);
        }

        public async Task AssignEmergencyRequestAsync(Guid requestId, Guid radiologistId)
        {
            var request = await _requestRepository.GetByIdNestedSearchAsync(requestId, maxLevel: 2);
            if (request is null) throw new KeyNotFoundException("Request not found.");

            request.AssignedRadiologist = await _radiologistRepository.GetByIdAsync(radiologistId)
                ?? throw new KeyNotFoundException("Radiologist not found.");
            request.AssignedAt = DateTime.UtcNow;
            request.Status = ReportingRequestStatusEnum.Pending;

            await _requestRepository.UpdateAsync(request);
            await _requestRepository.CommitAsync(Guid.Empty);

            // Push to the assigned radiologist's SignalR group immediately
            await _hubContext.Clients
                .Group($"radiologist-{radiologistId}")
                .SendAsync("EmergencyAssigned", new
                {
                    RequestId = request.Id,
                    PatientName = request.Image?.Patient?.Name,
                    Modality = request.Image?.ImageModality.ToString(),
                    EmergencyJustification = request.EmergencyJustification,
                    AssignedAt = request.AssignedAt,
                    DeadlineUtc = request.AssignedAt.Value.AddMinutes(5)
                });
        }

        public async Task<RadiologistRequestResponseDto> AcceptRequestAsync(Guid requestId, Guid radiologistId)
        {
            var request = await _requestRepository.GetByIdNestedSearchAsync(requestId, maxLevel: 2);
            if (request is null) throw new KeyNotFoundException("Request not found.");
            if (request.AssignedRadiologist?.Id != radiologistId)
                throw new UnauthorizedAccessException("Not assigned to you.");

            request.Status = ReportingRequestStatusEnum.InProgress;
            request.AssignedAt = null; // accepted — stop the escalation clock

            await _requestRepository.UpdateAsync(request);
            await _requestRepository.CommitAsync(Guid.Empty);

            return MapToResponseDto(request);
        }

        // ─────────────────────────────────────────────────────────────────────
        // US-16 – Doctor Match Score
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<IEnumerable<DoctorMatchScoreDto>> GetDoctorMatchScoresAsync(Guid requestId)
        {
            _logger.LogInformation("Calculating doctor match scores for request {RequestId}", requestId);

            // 1. Load the target request (we need its modality/department for specialty matching).
            var request = await _requestRepository.GetByIdNestedSearchAsync(requestId, maxLevel: 2);
            if (request is null)
                throw new KeyNotFoundException($"Reporting request with Id '{requestId}' was not found.");

            // 2. Load all radiologists.
            var allRadiologists = await _radiologistRepository.GetAllAsync();

            // 3. Load every request in bulk so we can derive queue sizes and turnaround stats
            //    without issuing one query per radiologist.
            var allRequests = (await _requestRepository.FindNestedSearchAsync(
                filter:   _ => true,
                maxLevel: 1          // depth-1 is enough; we only need AssignedRadiologist.Id + status/times
            )).ToList();

            // 4. Score each radiologist and project to DTO.
            var scores = allRadiologists
                .Select(rad => ScoreRadiologist(rad, request, allRequests))
                .OrderByDescending(dto => dto.OverallScore)
                .ToList();

            _logger.LogInformation(
                "Match scores calculated for {Count} radiologist(s) against request {RequestId}",
                scores.Count, requestId);

            return scores;
        }
        // ─────────────────────────────────────────────────────────────────────
        // Scoring helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the composite match score for a single radiologist.
        /// </summary>
        private static DoctorMatchScoreDto ScoreRadiologist(
            Radiologist            radiologist,
            ReportingRequest       targetRequest,
            IReadOnlyList<ReportingRequest> allRequests)
        {
            // --- Filter requests belonging to this radiologist ---
            var ownRequests = allRequests
                .Where(r => r.AssignedRadiologist?.Id == radiologist.Id)
                .ToList();

            // ── Component 1: Specialty alignment (weight 50 %) ───────────────
            double specialtyScore = CalculateSpecialtyScore(radiologist, targetRequest);

            // ── Component 2: Queue size (weight 30 %) ────────────────────────
            int activeCount = ownRequests.Count(r =>
                r.Status == ReportingRequestStatusEnum.Pending ||
                r.Status == ReportingRequestStatusEnum.InProgress);

            double queueScore = CalculateQueueScore(activeCount);

            // ── Component 3: Historical turnaround (weight 20 %) ─────────────
            var completedRequests = ownRequests
                .Where(r =>
                    r.Status           == ReportingRequestStatusEnum.Completed &&
                    r.CompletionTime.HasValue &&
                    r.SubmissionTime   != default)
                .ToList();

            double? avgTurnaroundHours = completedRequests.Any()
                ? completedRequests.Average(r =>
                    (r.CompletionTime!.Value - r.SubmissionTime).TotalHours)
                : (double?)null;

            double turnaroundScore = CalculateTurnaroundScore(avgTurnaroundHours);

            // ── Composite ────────────────────────────────────────────────────
            double overall =
                specialtyScore  * MatchScoreWeights.SpecialtyWeight  +
                queueScore      * MatchScoreWeights.QueueWeight       +
                turnaroundScore * MatchScoreWeights.TurnaroundWeight;

            return new DoctorMatchScoreDto
            {
                RadiologistId          = radiologist.Id,
                RadiologistName        = radiologist.Name ?? string.Empty,
                Specialty              = radiologist.Speciality.ToString() ?? string.Empty,
                CurrentQueueSize       = activeCount,
                AverageTurnaroundHours = avgTurnaroundHours.HasValue
                                             ? Math.Round(avgTurnaroundHours.Value, 2)
                                             : null,
                SpecialtyScore         = Math.Round(specialtyScore,  2),
                QueueScore             = Math.Round(queueScore,       2),
                TurnaroundScore        = Math.Round(turnaroundScore,  2),
                OverallScore           = Math.Round(overall,          2)
            };
        }

        /// <summary>
        /// Returns 100 for an exact specialty match, 50 for a related one, 0 otherwise.
        /// <para>
        /// "Related" is determined by whether the radiologist's specialty appears in the
        /// request's suggested department string (case-insensitive substring match).
        /// Extend this method if a formal specialty taxonomy is introduced later.
        /// </para>
        /// </summary>
        private static double CalculateSpecialtyScore(Radiologist rad, ReportingRequest req)
        {
            string radiologistSpeciality = rad.Speciality.ToString();
            if (string.IsNullOrWhiteSpace(radiologistSpeciality))
                return MatchScoreWeights.NoSpecialtyScore;

            // Exact match against the suggested department
            if (!string.IsNullOrWhiteSpace(req.SuggestedDepartment) &&
                string.Equals(
                    radiologistSpeciality.Trim(),
                    req.SuggestedDepartment.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return MatchScoreWeights.ExactSpecialtyScore;
            }

            // Related: specialty keyword found inside the department string or image modality name
            var modalityName = req.Image?.ImageModality.ToString() ?? string.Empty;
            var department   = req.SuggestedDepartment ?? string.Empty;

            bool isRelated =
                department.Contains(radiologistSpeciality,   StringComparison.OrdinalIgnoreCase) ||
                modalityName.Contains(radiologistSpeciality, StringComparison.OrdinalIgnoreCase);

            return isRelated
                ? MatchScoreWeights.RelatedSpecialtyScore
                : MatchScoreWeights.NoSpecialtyScore;
        }

        /// <summary>
        /// Converts queue size to a score in [0, 100].
        /// Linear decay: 0 cases → 100, <see cref="MatchScoreWeights.MaxQueueSize"/> cases → 0.
        /// </summary>
        private static double CalculateQueueScore(int activeCount)
        {
            if (activeCount <= 0)
                return 100.0;

            if (activeCount >= MatchScoreWeights.MaxQueueSize)
                return 0.0;

            return 100.0 * (1.0 - (double)activeCount / MatchScoreWeights.MaxQueueSize);
        }

        /// <summary>
        /// Converts average turnaround time to a score in [0, 100].
        /// No history → neutral score of 50 (avoid penalising new radiologists).
        /// Linear decay: 0 h → 100, <see cref="MatchScoreWeights.MaxTurnaroundHours"/> h → 0.
        /// </summary>
        private static double CalculateTurnaroundScore(double? avgHours)
        {
            if (avgHours is null)
                return 50.0; // neutral — no history available

            if (avgHours <= 0)
                return 100.0;

            if (avgHours >= MatchScoreWeights.MaxTurnaroundHours)
                return 0.0;

            return 100.0 * (1.0 - avgHours.Value / MatchScoreWeights.MaxTurnaroundHours);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mapper
        // ─────────────────────────────────────────────────────────────────────

        private static RadiologistRequestResponseDto MapToResponseDto(ReportingRequest request)
        {
            return new RadiologistRequestResponseDto
            {
                Id = request.Id,

                PatientId            = request.Image?.Patient?.Id            ?? Guid.Empty,
                PatientName          = request.Image?.Patient?.Name          ?? string.Empty,
                PatientDateOfBirth   = request.Image?.Patient?.DateOfBirth   ?? default,
                PatientPhoneNumber   = request.Image?.Patient?.PhoneNumber   ?? string.Empty,
                PatientGender        = request.Image?.Patient?.Gender        ?? default,
                PatientAddress       = request.Image?.Patient?.Address       ?? string.Empty,
                PatientMedicalHistory= request.Image?.Patient?.MedicalHistory,
                PatientNotes         = request.Image?.Patient?.Notes,

                MedicalImageId       = request.Image?.Id                     ?? Guid.Empty,
                ImageModality        = request.Image?.ImageModality          ?? default,
                StorageReference     = request.Image?.StorageReference       ?? string.Empty,

                RequestedByName      = request.RequestedBy?.Name             ?? string.Empty,
                SuggestedDepartment  = request.SuggestedDepartment           ?? string.Empty,
                Priority             = request.Priority,
                IsEmergency          = request.IsEmergency,
                EmergencyJustification = request.EmergencyJustification,

                ReportId             = request.ReportId,

                Status               = request.Status,
                SubmissionTime       = request.SubmissionTime,
                DueDate              = request.DueDate
            };
        }
    }
}
