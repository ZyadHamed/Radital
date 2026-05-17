using Domain;
using Domain.People;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadiologistAppCore.Hubs;
using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.Services
{
    // Services/EmergencyEscalationService.cs
    public class EmergencyEscalationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<RadiologistHub> _hubContext;
        private readonly ILogger<EmergencyEscalationService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan AcceptanceWindow = TimeSpan.FromMinutes(5);

        public EmergencyEscalationService(
            IServiceScopeFactory scopeFactory,
            IHubContext<RadiologistHub> hubContext,
            ILogger<EmergencyEscalationService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, stoppingToken);
                try { await EscalateStaleEmergenciesAsync(); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during emergency escalation check");
                }
            }
        }

        private async Task EscalateStaleEmergenciesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var requestRepo = scope.ServiceProvider.GetRequiredService<IRepository<ReportingRequest>>();
            var radiologistRepo = scope.ServiceProvider.GetRequiredService<IRepository<Radiologist>>();

            var cutoff = DateTime.UtcNow - AcceptanceWindow;

            // Find emergency requests that are still Pending AND were assigned more than 5 min ago
            var stale = (await requestRepo.FindNestedSearchAsync(
                filter: r =>
                    r.IsEmergency &&
                    r.Status == ReportingRequestStatusEnum.Pending &&
                    r.AssignedRadiologist != null &&
                    r.AssignedAt.HasValue &&
                    r.AssignedAt.Value < cutoff,
                maxLevel: 2
            )).ToList();

            foreach (var request in stale)
            {
                var timedOutRadiologistId = request.AssignedRadiologist!.Id;
                _logger.LogWarning(
                    "Emergency request {RequestId} not accepted by {RadiologistId} within window. Escalating.",
                    request.Id, timedOutRadiologistId);

                // Add the timed-out radiologist to the skip list
                request.EscalationHistory.Add(timedOutRadiologistId);

                // Find the next best radiologist (reuse your existing scoring, skip already-tried ones)
                var allRadiologists = (await radiologistRepo.GetAllAsync()).ToList();
                var allRequests = (await requestRepo.FindNestedSearchAsync(_ => true, maxLevel: 1)).ToList();

                var nextRadiologist = allRadiologists
                    .Where(r => !request.EscalationHistory.Contains(r.Id))
                    .Select(r => (Radiologist: r, Score: ScoreForEmergency(r, request, allRequests)))
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Radiologist)
                    .FirstOrDefault();

                if (nextRadiologist is null)
                {
                    // Nobody left — alert admins or set a special status
                    _logger.LogCritical(
                        "Emergency request {RequestId} has exhausted all available radiologists!", request.Id);
                    request.Status = ReportingRequestStatusEnum.Pending; // keep visible
                    request.AssignedRadiologist = null;
                    request.AssignedAt = null;
                }
                else
                {
                    request.AssignedRadiologist = nextRadiologist;
                    request.AssignedAt = DateTime.UtcNow;

                    // Notify the new radiologist via SignalR
                    await _hubContext.Clients
                        .Group($"radiologist-{nextRadiologist.Id}")
                        .SendAsync("EmergencyAssigned", new
                        {
                            RequestId = request.Id,
                            PatientName = request.Image?.Patient?.Name,
                            Modality = request.Image?.ImageModality.ToString(),
                            EmergencyJustification = request.EmergencyJustification,
                            AssignedAt = request.AssignedAt,
                            DeadlineUtc = request.AssignedAt.Value.AddMinutes(5),
                            EscalationRound = request.EscalationHistory.Count
                        });
                }

                await requestRepo.UpdateAsync(request);
                await requestRepo.CommitAsync(Guid.Empty);
            }
        }

        // Lightweight version of the scoring — specialty + queue only (no need to re-fetch all requests twice)
        private static double ScoreForEmergency(
            Radiologist rad, ReportingRequest req, List<ReportingRequest> allRequests)
        {
            int activeCount = allRequests.Count(r =>
                r.AssignedRadiologist?.Id == rad.Id &&
                (r.Status == ReportingRequestStatusEnum.Pending ||
                 r.Status == ReportingRequestStatusEnum.InProgress));

            double queueScore = activeCount >= 10 ? 0 : 100.0 * (1.0 - activeCount / 10.0);

            // Reuse specialty matching from MatchScoreWeights constants
            return queueScore * 0.5; // simplified; specialty check needs the full method if you want it
        }
    }
}
