using System;

namespace EPIFlow.Application.Reports.DTOs;

public record ExpiredEpiReportItemDto(
    string EmployeeName,
    string EpiCode,
    string EpiDescription,
    DateTime ValidUntil,
    int Quantity,
    int DaysOverdue);
