using QuestPDF.Fluent;
using Route = MeGui.Models.Route;
using Checkpoint = MeGui.Models.Checkpoint;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MeGui.Services;

public class PdfService
{
    public byte[] GenerateRoutePdf(Route route, List<Checkpoint> checkpoints)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Text("MêGui - Guia de Estações de SP")
                        .FontSize(22).Bold().FontColor(Colors.Blue.Darken3);

                    col.Item().PaddingTop(5).Text($"Rota: {route.OriginStation} → {route.DestinationStation}")
                        .FontSize(16).SemiBold().FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(15);

                    foreach (var checkpoint in checkpoints.OrderBy(c => c.Order))
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(inner =>
                        {
                            inner.Item().Text($"📍 Checkpoint {checkpoint.Order}")
                                .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                            inner.Item().PaddingTop(5).Text(checkpoint.Instruction)
                                .FontSize(11).LineHeight(1.4f);

                            inner.Item().PaddingTop(5).Text($"Imagem: {checkpoint.ImageUrl}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("MêGui © 2025 — Gerado automaticamente | Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
