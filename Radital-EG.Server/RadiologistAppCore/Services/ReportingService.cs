using Domain;
using Domain.People;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RadiologistAppCore.DTOs;
using RadiologistAppCore.Interfaces;
using RadiologistAppCore.Identities;

namespace RadiologistAppCore.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IRepository<Report> _reportRepository;
        private readonly IRepository<ReportingRequest> _requestRepository;
        private readonly IRepository<Radiologist> _radiologistRepository;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(
            IRepository<Report> reportRepository,
            IRepository<ReportingRequest> requestRepository,
            IRepository<Radiologist> radiologistRepository,
            ILogger<ReportingService> logger)
        {
            _reportRepository = reportRepository;
            _requestRepository = requestRepository;
            _radiologistRepository = radiologistRepository;
            _logger = logger;
        }

        public async Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid radiologistId)
        {
            _logger.LogInformation(
                "Creating report for request {RequestId} by radiologist {RadiologistId}",
                dto.ReportingRequestId, radiologistId);

            // 1. Fetch and validate the reporting request
            var request = await _requestRepository.GetByIdNestedSearchAsync(
                dto.ReportingRequestId, maxLevel: 5);

            if (request is null)
                throw new KeyNotFoundException(
                    $"Reporting request with Id '{dto.ReportingRequestId}' was not found.");

            if (request.AssignedRadiologist?.Id != radiologistId)
                throw new UnauthorizedAccessException(
                    "You are not assigned to this reporting request.");

            if (request.ReportId is not null)
                throw new InvalidOperationException(
                    "This reporting request already has a report attached.");

            // 2. Fetch the radiologist
            var radiologist = await _radiologistRepository.GetByIdAsync(radiologistId);
            if (radiologist is null)
                throw new UnauthorizedAccessException("Radiologist not found.");

            // 3. Generate the PDF
            var pdfBytes = GeneratePdf(dto, request, radiologist);
            var storageReference = await SavePdfAsync(pdfBytes, dto.ReportingRequestId);

            // 4. Create the report entity
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ClinicalHistory = dto.ClinicalHistory,
                Technique = dto.Technique,
                Findings = dto.Findings,
                Impression = dto.Impression,
                Recommendation = dto.Recommendation,
                Status = ReportStatusEnum.Draft,
                Author = radiologist,
                StorageReference = storageReference
            };

            await _reportRepository.InsertAsync(report);
            await _reportRepository.CommitAsync(Guid.Empty);

            // 5. Link the report to the reporting request
            request.Report = report;
            request.ReportId = report.Id;
            request.Status = ReportingRequestStatusEnum.Completed;
            request.CompletionTime = DateTime.Now;

            await _requestRepository.UpdateAsync(request);
            await _requestRepository.CommitAsync(Guid.Empty);

            _logger.LogInformation(
                "Report {ReportId} created and linked to request {RequestId}",
                report.Id, dto.ReportingRequestId);

            return MapToResponseDto(report, dto.ReportingRequestId);
        }

        public async Task<ReportResponseDto> GetReportByIdAsync(Guid reportId, Guid radiologistId)
        {
            _logger.LogInformation("Fetching report {ReportId}", reportId);

            var report = await _reportRepository.GetByIdNestedSearchAsync(reportId, maxLevel: 2);

            if (report is null)
                throw new KeyNotFoundException($"Report with Id '{reportId}' was not found.");

            if (report.Author.Id != radiologistId)
                throw new UnauthorizedAccessException("You are not authorized to view this report.");

            // Find the linked request to include the request Id
            var requests = await _requestRepository.FindNestedSearchAsync(
                filter: r => r.ReportId == reportId,
                maxLevel: 1
            );
            var linkedRequest = requests.FirstOrDefault();

            return MapToResponseDto(report, linkedRequest?.Id);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetAllReportsByRadiologistAsync(Guid radiologistId)
        {
            _logger.LogInformation("Fetching all reports for radiologist {RadiologistId}", radiologistId);

            var reports = await _reportRepository.FindNestedSearchAsync(
                filter: r => r.Author.Id == radiologistId,
                maxLevel: 2
            );

            // Fetch all requests that have reports to map request Ids
            var requests = await _requestRepository.FindNestedSearchAsync(
                filter: r => r.ReportId != null && r.AssignedRadiologist.Id == radiologistId,
                maxLevel: 1
            );

            var requestByReportId = requests
                .Where(r => r.ReportId.HasValue)
                .ToDictionary(r => r.ReportId!.Value, r => r.Id);

            return reports.Select(report =>
            {
                requestByReportId.TryGetValue(report.Id, out var requestId);
                return MapToResponseDto(report, requestId == Guid.Empty ? null : requestId);
            });
        }

        public async Task<byte[]> GetReportPdfAsync(Guid reportId, Guid callerId)
        {
            _logger.LogInformation("Fetching PDF for report {ReportId} (caller {CallerId})", reportId, callerId);

            var report = await _reportRepository.GetByIdNestedSearchAsync(reportId, maxLevel: 2);

            if (report is null)
                throw new KeyNotFoundException($"Report with Id '{reportId}' was not found.");

            var isSystem = SystemIdentities.IsSystem(callerId);

            if (!isSystem && report.Author.Id != callerId)
                throw new UnauthorizedAccessException("You are not authorized to access this report.");

            if (!File.Exists(report.StorageReference))
                throw new FileNotFoundException(
                    $"PDF file not found at path: {report.StorageReference}");

            return await File.ReadAllBytesAsync(report.StorageReference);
        }



        // ─────────────────────────────────────────────────────────────────────
        // PDF Generation (QuestPDF)
        // ─────────────────────────────────────────────────────────────────────

        private static byte[] GeneratePdf(
            CreateReportDto dto,
            ReportingRequest request,
            Radiologist radiologist)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // ── Header ────────────────────────────────────────────
                    page.Header().Column(col =>
                    {
                        col.Item().Text("RADIOLOGY REPORT")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);

                        col.Item().PaddingTop(5).Text($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // ── Content ───────────────────────────────────────────
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Patient Info
                        col.Item().PaddingBottom(10).Column(section =>
                        {
                            section.Item().Text("PATIENT INFORMATION")
                                .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                            section.Item().Text($"Name: {request.Image?.Patient?.Name ?? "N/A"}");
                            section.Item().Text($"Date of Birth: {request.Image?.Patient?.DateOfBirth:yyyy-MM-dd}");
                            section.Item().Text($"Gender: {request.Image?.Patient?.Gender}");
                            section.Item().Text($"Phone: {request.Image?.Patient?.PhoneNumber ?? "N/A"}");
                        });

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        // Imaging Info
                        col.Item().PaddingVertical(10).Column(section =>
                        {
                            section.Item().Text("IMAGING DETAILS")
                                .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                            section.Item().Text($"Modality: {request.Image?.ImageModality}");
                            section.Item().Text($"Priority: {request.Priority}");
                            section.Item().Text($"Emergency: {(request.IsEmergency ? "Yes" : "No")}");

                            if (request.IsEmergency && !string.IsNullOrEmpty(request.EmergencyJustification))
                                section.Item().Text($"Justification: {request.EmergencyJustification}");
                        });

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        // Report Sections
                        AddReportSection(col, "CLINICAL HISTORY", dto.ClinicalHistory);
                        AddReportSection(col, "TECHNIQUE", dto.Technique);
                        AddReportSection(col, "FINDINGS", dto.Findings);
                        AddReportSection(col, "IMPRESSION", dto.Impression);
                        AddReportSection(col, "RECOMMENDATION", dto.Recommendation);
                    });

                    // ── Footer ────────────────────────────────────────────
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text($"Reported by: Dr. {radiologist.Name}")
                                    .FontSize(10).Bold();
                                left.Item().Text($"Speciality: {radiologist.Speciality}")
                                    .FontSize(9);
                                left.Item().Text($"Email: {radiologist.Email}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text($"Report ID: {Guid.NewGuid()}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                                right.Item().AlignRight().Text(text =>
                                {
                                    text.Span("Page ").FontSize(8);
                                    text.CurrentPageNumber().FontSize(8);
                                    text.Span(" of ").FontSize(8);
                                    text.TotalPages().FontSize(8);
                                });
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void AddReportSection(ColumnDescriptor col, string title, string content)
        {
            col.Item().PaddingVertical(8).Column(section =>
            {
                section.Item().Text(title)
                    .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                section.Item().PaddingTop(3).Text(content);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PDF Storage
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<string> SavePdfAsync(byte[] pdfBytes, Guid requestId)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            Directory.CreateDirectory(directory);

            var fileName = $"Report_{requestId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(directory, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return filePath;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mapper
        // ─────────────────────────────────────────────────────────────────────

        private static ReportResponseDto MapToResponseDto(Report report, Guid? reportingRequestId)
        {
            return new ReportResponseDto
            {
                Id = report.Id,
                ReportingRequestId = reportingRequestId ?? Guid.Empty,
                ClinicalHistory = report.ClinicalHistory,
                Technique = report.Technique,
                Findings = report.Findings,
                Impression = report.Impression,
                Recommendation = report.Recommendation,
                Status = report.Status,
                AuthorId = report.Author.Id,
                AuthorName = report.Author.Name,
                StorageReference = report.StorageReference
            };
        }
    }
}