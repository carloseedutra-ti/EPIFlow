using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Application.Tenants.DTOs;

public class TenantPaymentCreateDto
{
    [Required]
    public Guid TenantId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "Informe um valor válido.")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
