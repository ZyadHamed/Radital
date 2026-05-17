using HospitalRequestsAPI.ExternalClients;
using HospitalRequestsAppCore.DTOs;
using HospitalRequestsAppCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HospitalRequestsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingRequestsController : ControllerBase
    {
        private readonly IReportingRequestsManagementService _service;
        private readonly ILogger<ReportingRequestsController> _logger;
        private readonly IRadiologistApiClient _radiologistApi;

        public ReportingRequestsController(
            IReportingRequestsManagementService service,
            ILogger<ReportingRequestsController> logger,
            IRadiologistApiClient radiologistApi)
        {
            _service = service;
            _logger = logger;
            _radiologistApi = radiologistApi;
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ReportingRequestResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRequest([FromBody] CreateReportingRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var staffMemberId = GetStaffMemberIdFromToken();
                var result = await _service.CreateRequestAsync(dto, staffMemberId);

                // ── If emergency, notify the assigned radiologist via RadiologistAPI ──
                if (dto.IsEmergency && result.AssignedRadiologistId.HasValue)
                {
                    try
                    {
                        await _radiologistApi.NotifyEmergencyAsync(result.Id, result.AssignedRadiologistId.Value);
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the whole request if notification fails — log and continue
                        _logger.LogError(ex, "Failed to send emergency SignalR notification for request {RequestId}", result.Id);
                    }
                }

                return CreatedAtAction(nameof(GetRequestById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized request attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reporting request");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while creating the reporting request.");
            }
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<ReportingRequestResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllRequests()
        {
            try
            {
                var staffMemberId = GetStaffMemberIdFromToken();
                var results = await _service.GetAllRequestsAsync(staffMemberId);
                return Ok(results);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized request attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reporting requests");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching reporting requests.");
            }
        }

        /// <summary>
        /// US-02: Get a single reporting request by Id.
        /// Allows the technician to check the real-time status of a specific request.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ReportingRequestResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            try
            {
                var staffMemberId = GetStaffMemberIdFromToken();
                var result = await _service.GetRequestByIdAsync(id, staffMemberId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reporting request {RequestId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching the reporting request.");
            }
        }

        [HttpGet("{id:guid}/report/pdf")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadReportPdf(Guid id, CancellationToken ct)
        {
            try
            {
                var staffMemberId = GetStaffMemberIdFromToken();
                var request = await _service.GetRequestByIdAsync(id, staffMemberId);

                if (request.ReportId is null)
                    return NotFound(new { message = "No report has been generated for this request yet." });

                var (pdfBytes, fileName) = await _radiologistApi.DownloadReportPdfAsync(request.ReportId.Value, ct);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF for reporting request {RequestId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the report.");
            }
        }

        private Guid GetStaffMemberIdFromToken()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var staffMemberId))
            {
                throw new UnauthorizedAccessException("Unable to extract staff member identity from token.");
            }

            return staffMemberId;
        }
    }
}
