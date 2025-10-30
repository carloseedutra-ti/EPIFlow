using System;
using EPIFlow.Application.Common.Interfaces;

namespace EPIFlow.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
