using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RadiologistAppCore.DTOs;
using RadiologistAppCore.Hubs;
using RadiologistAppCore.Interfaces;
using System.Security.Claims;

namespace RadiologistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Radiologist")]
    public class WorkloadController : ControllerBase
    {
        private readonly IWorkloadManagementService _service;
        private readonly ILogger<WorkloadController> _logger;

        public WorkloadController(
            IWorkloadManagementService service,
            ILogger<WorkloadController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Existing endpoints
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RadiologistRequestResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAssignedRequests()
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var results = await _service.GetAssignedRequestsAsync(radiologistId);
                return Ok(results);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assigned requests");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching assigned requests.");
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RadiologistRequestResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAssignedRequestById(Guid id)
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var result = await _service.GetAssignedRequestByIdAsync(id, radiologistId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching request {RequestId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching the request.");
            }
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(RadiologistRequestResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateRequestStatus(
            Guid id, [FromBody] UpdateRequestStatusDto dto)
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var result = await _service.UpdateRequestStatusAsync(id, radiologistId, dto.Status);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request {RequestId} status", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while updating the request status.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // US-16 – Doctor Match Score
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET api/workload/{id}/doctor-match-scores
        ///
        /// Returns a ranked list of available radiologists with their real-time
        /// match scores for the specified reporting request.
        ///
        /// Score components:
        ///   - Specialty alignment  (50 %)
        ///   - Current queue size   (30 %)
        ///   - Historical turnaround (20 %)
        ///
        /// Results are sorted by OverallScore descending (best match first).
        /// </summary>
        [HttpGet("{id:guid}/doctor-match-scores")]
        [ProducesResponseType(typeof(IEnumerable<DoctorMatchScoreDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDoctorMatchScores(Guid id)
        {
            try
            {
                var scores = await _service.GetDoctorMatchScoresAsync(id);
                return Ok(scores);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating doctor match scores for request {RequestId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while calculating doctor match scores.");
            }
        }

        [HttpPost("{id:guid}/accept")]
        [ProducesResponseType(typeof(RadiologistRequestResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptRequest(Guid id)
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var result = await _service.AcceptRequestAsync(id, radiologistId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting request {RequestId}", id);
                return StatusCode(500, "An error occurred while accepting the request.");
            }
        }

        // Internal endpoint — called by HospitalRequestsAPI only, not by Angular
        [HttpPost("{requestId:guid}/notify-emergency/{radiologistId:guid}")]
        [AllowAnonymous] // or use an internal API key header instead
        public async Task<IActionResult> NotifyEmergency(Guid requestId, Guid radiologistId)
        {
            try
            {
                await _service.AssignEmergencyRequestAsync(requestId, radiologistId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying emergency for request {RequestId}", requestId);
                return StatusCode(500);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private Guid GetRadiologistIdFromToken()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var radiologistId))
                throw new UnauthorizedAccessException("Unable to extract radiologist identity from token.");

            return radiologistId;
        }
    }
}
