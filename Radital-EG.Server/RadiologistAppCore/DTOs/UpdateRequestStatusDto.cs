using Domain;

namespace RadiologistAppCore.DTOs
{
    public class UpdateRequestStatusDto
    {
        public required ReportingRequestStatusEnum Status { get; set; }
    }
}