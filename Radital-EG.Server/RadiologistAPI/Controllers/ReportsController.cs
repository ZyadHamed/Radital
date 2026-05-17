using RadiologistAppCore.DTOs;
using RadiologistAppCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RadiologistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Radiologist")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _service;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportingService service,
            ILogger<ReportsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var result = await _service.CreateReportAsync(dto, radiologistId);
                return CreatedAtAction(nameof(GetReportById), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while creating the report.");
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var result = await _service.GetReportByIdAsync(id, radiologistId);
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
                _logger.LogError(ex, "Error fetching report {ReportId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching the report.");
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ReportResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllReports()
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var results = await _service.GetAllReportsByRadiologistAsync(radiologistId);
                return Ok(results);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while fetching reports.");
            }
        }

        [HttpGet("{id:guid}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadReportPdf(Guid id)
        {
            try
            {
                var radiologistId = GetRadiologistIdFromToken();
                var pdfBytes = await _service.GetReportPdfAsync(id, radiologistId);
                return File(pdfBytes, "application/pdf", $"Report_{id}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF for report {ReportId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while downloading the report.");
            }
        }


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