using System;

namespace EPIFlow.Application.Reports.DTOs;

public record DeliveryReportItemDto(
    DateTime DeliveryDate,
    string EmployeeName,
    string EpiCode,
    string EpiDescription,
    int Quantity,
    DateTime ValidUntil,
    string DeliveredBy);
