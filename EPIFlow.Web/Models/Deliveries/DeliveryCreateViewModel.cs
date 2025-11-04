using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryCreateViewModel
{
    [Display(Name = "Colaborador")]
    [Required(ErrorMessage = "Selecione o colaborador.")]
    public Guid? EmployeeId { get; set; }

    [Display(Name = "Data da entrega")]
    [DataType(DataType.Date)]
    public DateTime DeliveryDate { get; set; } = DateTime.Today;

    [Display(Name = "Número da entrega")]
    [MaxLength(50)]
    public string? DeliveryNumber { get; set; }

    [Display(Name = "Observações")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    public Guid? BiometricValidationTaskId { get; set; }

    public bool IsBiometricValidated { get; set; }

    public List<DeliveryItemInputModel> Items { get; set; } = new() { new DeliveryItemInputModel() };

    public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> EpiTypes { get; set; } = new List<SelectListItem>();
}
