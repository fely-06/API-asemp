using API_asemp.Models.Cobros;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace API_asemp.Servicios
{
    public class ReportesCobrosArchivoService
    {
        private readonly string _logoPath = "wwwroot/assets/images/Logo-de-la Empresa.png";

        //// ============================================================
        ////                       EXCEL
        //// ============================================================

        public byte[] GenerarExcel(List<CobroPendienteDTO> datos)
        {
            var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Reporte Cobros");

            // ================================
            //   LOGO + TÍTULO PRINCIPAL
            // ================================

            string logoPath = "wwwroot/assets/images/Logo-de-la Empresa.png";

            if (File.Exists(logoPath))
            {
                var img = ws.AddPicture(logoPath)
                    .MoveTo(ws.Cell("A1"))
                    .Scale(0.35); // Ajusta tamaño
            }

            ws.Cell("C1").Value = "REPORTE DE COBROS";
            ws.Cell("C1").Style.Font.Bold = true;
            ws.Cell("C1").Style.Font.FontSize = 20;
            ws.Cell("C1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Combinar celdas del título
            ws.Range("C1:F1").Merge();

            // Espacio debajo del título
            int startRow = 4;

            // ================================
            //          ENCABEZADOS
            // ================================

            string[] headers = {
        "Cliente", "RFC", "Mes", "Año",
        "Total", "Estado", "Folio", "Fecha Pago"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(startRow, i + 1).Value = headers[i];
                ws.Cell(startRow, i + 1).Style.Font.Bold = true;
                ws.Cell(startRow, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#7A003C"); // Guinda ASEMP
                ws.Cell(startRow, i + 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(startRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(startRow, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // ================================
            //          DATOS
            // ================================
            int row = startRow + 1;

            foreach (var d in datos)
            {
                ws.Cell(row, 1).Value = d.Cliente;
                ws.Cell(row, 2).Value = d.RFC;
                ws.Cell(row, 3).Value = d.Mes;
                ws.Cell(row, 4).Value = d.Anio;

                ws.Cell(row, 5).Value = d.Total;
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                ws.Cell(row, 6).Value = d.EstadoPago;
                ws.Cell(row, 7).Value = d.Folio;
                ws.Cell(row, 8).Value = d.FechaPago?.ToString("dd/MM/yyyy") ?? "";

                // Bordes a cada fila
                for (int col = 1; col <= 8; col++)
                {
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            // ================================
            //   TOTAL GENERAL ABAJO
            // ================================
            decimal totalGeneral = datos.Sum(x => x.Total);

            ws.Cell(row + 1, 4).Value = "TOTAL GENERAL:";
            ws.Cell(row + 1, 4).Style.Font.Bold = true;
            ws.Cell(row + 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(row + 1, 5).Value = totalGeneral;
            ws.Cell(row + 1, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row + 1, 5).Style.Font.Bold = true;
            ws.Cell(row + 1, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2E5E5");

            // ================================
            //   AJUSTE DE COLUMNAS
            // ================================
            ws.Columns().AdjustToContents();

            // ================================
            //   EXPORTAR
            // ================================
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }


        //public byte[] GenerarExcel(List<CobroPendienteDTO> datos)
        //{
        //    using var wb = new XLWorkbook();
        //    var ws = wb.AddWorksheet("Reporte");

        //    ws.Cell(1, 1).Value = "Cliente";
        //    ws.Cell(1, 2).Value = "RFC";
        //    ws.Cell(1, 3).Value = "Mes";
        //    ws.Cell(1, 4).Value = "Año";
        //    ws.Cell(1, 5).Value = "Total";
        //    ws.Cell(1, 6).Value = "Estado";
        //    ws.Cell(1, 7).Value = "Folio";
        //    ws.Cell(1, 8).Value = "Fecha Pago";

        //    ws.Range("A1:H1").Style.Font.Bold = true;

        //    int row = 2;
        //    foreach (var d in datos)
        //    {
        //        ws.Cell(row, 1).Value = d.Cliente;
        //        ws.Cell(row, 2).Value = d.RFC;
        //        ws.Cell(row, 3).Value = d.Mes;
        //        ws.Cell(row, 4).Value = d.Anio;
        //        ws.Cell(row, 5).Value = d.Total;
        //        ws.Cell(row, 6).Value = d.EstadoPago;
        //        ws.Cell(row, 7).Value = d.Folio;
        //        ws.Cell(row, 8).Value = d.FechaPago?.ToString("yyyy-MM-dd") ?? "";
        //        row++;
        //    }

        //    ws.Columns().AdjustToContents();

        //    using var stream = new MemoryStream();
        //    wb.SaveAs(stream);
        //    return stream.ToArray();
        //}

        // ============================================================
        //                 PDF COMPLETO CON QUESTPDF
        // ============================================================
        public byte[] GenerarPdfCompleto(List<CobroPendienteDTO> datos, string titulo)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor("#333"));

                    page.Header().Element(Encabezado);
                    page.Content().Element(c => Contenido(c, datos, titulo));
                    page.Footer().AlignCenter().Text($"ASEMP — {DateTime.Now:dd/MM/yyyy}");
                });
            });

            return document.GeneratePdf();
        }

        // ENCABEZADO
        private void Encabezado(IContainer container)
        {
            container.Row(row =>
            {
                if (File.Exists(_logoPath))
                    row.ConstantItem(80).Image(_logoPath);

                row.RelativeItem().PaddingLeft(10).Column(col =>
                {
                    col.Item().Text("ASEMP — Reporte de Cobros")
                        .FontSize(20)
                        .Bold()
                        .FontColor("#7A003C");

                    col.Item().Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy}");
                });
            });
        }

        // CONTENIDO GENERAL
        private void Contenido(IContainer container, List<CobroPendienteDTO> datos, string titulo)
        {
            container.Column(col =>
            {
                //col.Item().PaddingBottom(10).Text(titulo).FontSize(16).Bold();


                col.Item().Element(c => ResumenGeneral(c, datos));

                var grupos = datos.GroupBy(x => x.Cliente);

                foreach (var grupo in grupos)
                {
                    col.Item().PaddingTop(15)
                        .Element(c => SeccionCliente(c, grupo.Key, grupo.ToList()));
                }
            });
        }

        // RESUMEN
        private void ResumenGeneral(IContainer container, List<CobroPendienteDTO> datos)
        {
            decimal tPend = datos.Where(x => x.EstadoPago == "PENDIENTE").Sum(x => x.Total);
            decimal tPag = datos.Where(x => x.EstadoPago == "PAGADO").Sum(x => x.Total);

            int clientesDeuda = datos
                .Where(x => x.EstadoPago == "PENDIENTE")
                .Select(x => x.ClienteId)
                .Distinct().Count();

            container.Border(1).Padding(10).Background("#F5F5F5").Column(col =>
            {
                col.Item().Text($"Clientes con deuda: {clientesDeuda}").Bold();
                col.Item().Text($"Total pendiente: {tPend:C2}");
                col.Item().Text($"Total pagado: {tPag:C2}");
                col.Item().Text($"Total registros: {datos.Count}");
            });
        }

        // SECCIÓN POR CLIENTE
        private void SeccionCliente(IContainer container, string cliente, List<CobroPendienteDTO> lista)
        {
            container.Column(col =>
            {
                col.Item().Text(cliente)
                    .FontSize(15)
                    .Bold()
                    .FontColor("#7A003C");

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(80);
                        c.ConstantColumn(50);
                        c.ConstantColumn(60);
                        c.ConstantColumn(80);
                        c.ConstantColumn(60);
                        c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Mes");
                        h.Cell().Element(HeaderCell).Text("Año");
                        h.Cell().Element(HeaderCell).Text("Total");
                        h.Cell().Element(HeaderCell).Text("Estado");
                        h.Cell().Element(HeaderCell).Text("Folio");
                        h.Cell().Element(HeaderCell).Text("Fecha Pago");

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#A00050")
                             .Padding(5)
                             .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold());
                    });

                    foreach (var d in lista)
                    {
                        table.Cell().Element(Cell).Text(d.Mes);
                        table.Cell().Element(Cell).Text(d.Anio.ToString());
                        table.Cell().Element(Cell).Text(d.Total.ToString("C2"));
                        table.Cell().Element(Cell).Text(d.EstadoPago ?? "");
                        table.Cell().Element(Cell).Text(d.Folio);
                        table.Cell().Element(Cell).Text(d.FechaPago?.ToString("dd/MM/yyyy") ?? "");

                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).Padding(4);
                    }
                });

                // TOTALES DEL CLIENTE
                decimal totalCliente = lista.Sum(x => x.Total);
                decimal totalPend = lista.Where(x => x.EstadoPago == "PENDIENTE").Sum(x => x.Total);

                col.Item().AlignRight().PaddingTop(5)
                    .Text($"Total del cliente: {totalCliente:C2}").Bold();

                col.Item().AlignRight()
                    .Text($"Pendiente del cliente: {totalPend:C2}");
            });
        }
    }
}
