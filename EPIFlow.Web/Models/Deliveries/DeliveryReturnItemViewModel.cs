using System;
using System.ComponentModel.DataAnnotations;

namespace EPIFlow.Web.Models.Deliveries;

public class DeliveryReturnItemViewModel
{
    public Guid DeliveryItemId { get; set; }
    public string EpiDescription { get; set; } = string.Empty;
    public int QuantityDelivered { get; set; }
    public int? ReturnedQuantity { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    [Display(Name = "Quantidade para devolução")]
    [Range(0, int.MaxValue, ErrorMessage = "Informe uma quantidade válida.")]
    public int QuantityToReturn { get; set; }
}
