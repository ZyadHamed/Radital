namespace RadiologistAppCore.DTOs
{
    /// <summary>
    /// Represents the real-time match score for an available radiologist
    /// relative to a specific reporting request.
    /// </summary>
    public class DoctorMatchScoreDto
    {
        /// <summary>The radiologist's unique identifier.</summary>
        public Guid RadiologistId { get; set; }

        /// <summary>The radiologist's full name.</summary>
        public string RadiologistName { get; set; } = string.Empty;

        /// <summary>
        /// Composite match score in the range [0, 100].
        /// Higher is better. Weighted sum of the three component scores below.
        /// </summary>
        public double OverallScore { get; set; }

        // ── Component scores (each in [0, 100]) ──────────────────────────────

        /// <summary>
        /// Specialty-alignment score.
        /// 100 = exact specialty match, 50 = related specialty, 0 = no match.
        /// Weight: 50 %
        /// </summary>
        public double SpecialtyScore { get; set; }

        /// <summary>
        /// Queue-size score — inverse of current workload.
        /// 100 = no active cases, scales down linearly to 0 at <see cref="MatchScoreWeights.MaxQueueSize"/> cases.
        /// Weight: 30 %
        /// </summary>
        public double QueueScore { get; set; }

        /// <summary>
        /// Historical turnaround-time score — inverse of average completion time.
        /// 100 = fastest possible, 0 = slowest (at or beyond <see cref="MatchScoreWeights.MaxTurnaroundHours"/> h).
        /// Weight: 20 %
        /// </summary>
        public double TurnaroundScore { get; set; }

        // ── Supporting data ───────────────────────────────────────────────────

        /// <summary>Number of requests currently in progress for this radiologist.</summary>
        public int CurrentQueueSize { get; set; }

        /// <summary>
        /// Average turnaround time in hours, computed from completed requests.
        /// Null when no historical data is available.
        /// </summary>
        public double? AverageTurnaroundHours { get; set; }

        /// <summary>The radiologist's primary specialty.</summary>
        public string Specialty { get; set; } = string.Empty;
    }
}
