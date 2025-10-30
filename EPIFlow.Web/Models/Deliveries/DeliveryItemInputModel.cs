using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryItemInputModel
{
    [Required]
    public Guid EpiTypeId { get; set; }

    [Display(Name = "Quantidade")]
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma quantidade válida.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Validade")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Informe a data de validade.")]
    public DateTime ValidUntil { get; set; } = DateTime.Today.AddMonths(6);
}
