using System;
using System.ComponentModel.DataAnnotations;
using EPIFlow.Domain.Common;

namespace EPIFlow.Domain.Entities;

public class TenantPayment : AuditableEntity
{
    public Tenant? Tenant { get; set; }

    [Range(typeof(decimal), "0.0", "999999999999.99")]
    public decimal Amount { get; set; }

    public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
