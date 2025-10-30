using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Reports.DTOs;
using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EPIFlow.Application.Reports.Services;

public class ReportService : IReportService
{
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<EpiDelivery> _deliveryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReportService(
        IRepository<InventoryItem> inventoryRepository,
        IRepository<EpiDelivery> deliveryRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _inventoryRepository = inventoryRepository;
        _deliveryRepository = deliveryRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<StockReportItemDto>> GetStockReportAsync(CancellationToken cancellationToken = default)
    {
        return await _inventoryRepository.AsQueryable()
            .Include(item => item.EpiType)
            .AsNoTracking()
            .Select(item => new StockReportItemDto(
                item.EpiType!.Code,
                item.EpiType.Description,
                item.EpiType.Category.ToString(),
                item.QuantityAvailable,
                item.MinimumQuantity,
                item.QuantityAvailable < item.MinimumQuantity))
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryReportItemDto>> GetDeliveryReportByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _deliveryRepository.AsQueryable()
            .Where(delivery => delivery.EmployeeId == employeeId)
            .Include(delivery => delivery.Employee)
            .Include(delivery => delivery.Items)
                .ThenInclude(item => item.EpiType)
            .AsNoTracking()
            .SelectMany(delivery => delivery.Items
                .Where(item => !item.IsDeleted)
                .Select(item => new DeliveryReportItemDto(
                    delivery.DeliveryDate,
                    delivery.Employee!.Name,
                    item.EpiType!.Code,
                    item.EpiType.Description,
                    item.Quantity,
                    item.ValidUntil,
                    delivery.ResponsibleUserId.ToString())))
            .OrderBy(reportItem => reportItem.DeliveryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpiredEpiReportItemDto>> GetExpiredEpiReportAsync(DateTime referenceDate, int thresholdInDays, CancellationToken cancellationToken = default)
    {
        var thresholdDate = referenceDate.AddDays(thresholdInDays);

        return await _deliveryRepository.AsQueryable()
            .Include(delivery => delivery.Employee)
            .Include(delivery => delivery.Items)
                .ThenInclude(item => item.EpiType)
            .AsNoTracking()
            .SelectMany(delivery => delivery.Items
                .Where(item => !item.IsDeleted && item.ValidUntil <= thresholdDate && (!item.ReturnedQuantity.HasValue || item.ReturnedQuantity < item.Quantity))
                .Select(item => new ExpiredEpiReportItemDto(
                    delivery.Employee!.Name,
                    item.EpiType!.Code,
                    item.EpiType.Description,
                    item.ValidUntil,
                    item.Quantity - (item.ReturnedQuantity ?? 0),
                    (referenceDate.Date - item.ValidUntil.Date).Days)))
            .OrderBy(reportItem => reportItem.ValidUntil)
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]> GenerateStockReportPdfAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetStockReportAsync(cancellationToken);
        return GeneratePdf("Relatório de Estoque", table =>
        {
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Código");
                header.Cell().Element(CellStyle).Text("Descrição");
                header.Cell().Element(CellStyle).Text("Categoria");
                header.Cell().Element(CellStyle).AlignRight().Text("Quantidade");
                header.Cell().Element(CellStyle).AlignRight().Text("Mínimo");
                header.Cell().Element(CellStyle).Text("Status");
            });

            foreach (var item in data)
            {
                table.Cell().Element(CellStyle).Text(item.Code);
                table.Cell().Element(CellStyle).Text(item.Description);
                table.Cell().Element(CellStyle).Text(item.Category);
                table.Cell().Element(CellStyle).AlignRight().Text(item.QuantityAvailable.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text(item.MinimumQuantity.ToString());
                table.Cell().Element(CellStyle).Text(item.IsBelowMinimum ? "Atenção" : "OK");
            }
        });
    }

    public async Task<byte[]> GenerateDeliveryReportPdfAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var data = await GetDeliveryReportByEmployeeAsync(employeeId, cancellationToken);
        return GeneratePdf("Relatório de Entregas", table =>
        {
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Data");
                header.Cell().Element(CellStyle).Text("Colaborador");
                header.Cell().Element(CellStyle).Text("Código");
                header.Cell().Element(CellStyle).Text("Descrição");
                header.Cell().Element(CellStyle).AlignRight().Text("Qtd");
                header.Cell().Element(CellStyle).Text("Validade");
            });

            foreach (var item in data)
            {
                table.Cell().Element(CellStyle).Text(item.DeliveryDate.ToString("dd/MM/yyyy"));
                table.Cell().Element(CellStyle).Text(item.EmployeeName);
                table.Cell().Element(CellStyle).Text(item.EpiCode);
                table.Cell().Element(CellStyle).Text(item.EpiDescription);
                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(CellStyle).Text(item.ValidUntil.ToString("dd/MM/yyyy"));
            }
        });
    }

    public async Task<byte[]> GenerateExpiredEpiReportPdfAsync(DateTime referenceDate, int thresholdInDays, CancellationToken cancellationToken = default)
    {
        var data = await GetExpiredEpiReportAsync(referenceDate, thresholdInDays, cancellationToken);
        return GeneratePdf("EPIs Vencidos ou Próximos do Vencimento", table =>
        {
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Colaborador");
                header.Cell().Element(CellStyle).Text("Código");
                header.Cell().Element(CellStyle).Text("Descrição");
                header.Cell().Element(CellStyle).AlignRight().Text("Qtd");
                header.Cell().Element(CellStyle).Text("Validade");
                header.Cell().Element(CellStyle).Text("Dias");
            });

            foreach (var item in data)
            {
                table.Cell().Element(CellStyle).Text(item.EmployeeName);
                table.Cell().Element(CellStyle).Text(item.EpiCode);
                table.Cell().Element(CellStyle).Text(item.EpiDescription);
                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(CellStyle).Text(item.ValidUntil.ToString("dd/MM/yyyy"));
                table.Cell().Element(CellStyle).Text(item.DaysOverdue.ToString());
            }
        });
    }

    private static IContainer CellStyle(IContainer container) => container
        .BorderBottom(1)
        .BorderColor(Colors.Grey.Lighten2)
        .PaddingVertical(5)
        .PaddingHorizontal(3);

    private byte[] GeneratePdf(string title, Action<TableDescriptor> tableBuilder)
    {
        var generatedAt = _dateTimeProvider.UtcNow.ToLocalTime();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("EPIFlow").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(title).FontSize(16).FontColor(Colors.Blue.Darken3);
                    column.Item().Text($"Gerado em: {generatedAt:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tableBuilder(table);
                });

                page.Footer()
                    .AlignRight()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
