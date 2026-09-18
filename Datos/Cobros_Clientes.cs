using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;


//
using System.Globalization;
using System.Text;


//pdf
using DinkToPdf;
using DinkToPdf.Contracts;

//pdf
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API_asemp.Datos
{
    public class Cobros_Clientes
    {
        private readonly myDbContext _db;

        private readonly IConfiguration _config;

        private readonly IWebHostEnvironment _env;



        public Cobros_Clientes(myDbContext db, IConfiguration config, IWebHostEnvironment env)
        {
            _db = db;
            _config = config;
            QuestPDF.Settings.License = LicenseType.Community;
            _env = env;
        }

        // ================================
        //   OBTENER UN COBRO POR ID
        // ================================
        public Cobro_Cliente Get(int id)
        {
            var registro = _db.cobros_clientes.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este registro de cobro");
            return registro!;
        }

        // ================================
        //   LISTA COMPLETA DE COBROS
        // ================================
        public List<Cobro_Cliente> GetLista()
        {
            return _db.cobros_clientes
                .OrderByDescending(x => x.id)
                .ToList();
        }



        //

        public List<CobroClienteDTO> Buscar(CobrosFiltro f)
        {
            var query =
                from c in _db.cobros_clientes
                join cli in _db.clientes on c.cliente_id equals cli.id
                select new { c, cli };

            // ============================
            // FILTRO POR TEXTO (solo cliente)
            // ============================
            if (!string.IsNullOrWhiteSpace(f.texto))
            {
                var t = f.texto.Trim().ToLower();

                query = query.Where(x =>
                    (x.cli.razon_social ?? "").ToLower().Contains(t)
                );
            }

            // ============================
            // FILTRO POR CLIENTE
            // ============================
            if (f.cliente_id.HasValue)
                query = query.Where(x => x.c.cliente_id == f.cliente_id);

            // ============================
            // FILTRO POR MES
            // ============================
            if (f.mes.HasValue)
                query = query.Where(x => x.c.mes == f.mes.Value);

            // ============================
            // FILTRO POR AÑO
            // ============================
            if (f.anio.HasValue)
                query = query.Where(x => x.c.anio == f.anio.Value);

            // ============================
            // PENDIENTES
            // ============================
            if (f.pendientes == true)
                query = query.Where(x => x.c.estado_pago != "PAGADO");

            // ============================
            // PAGADOS
            // ============================
            if (f.pagados == true)
                query = query.Where(x => x.c.estado_pago == "PAGADO");

            // ============================
            // ATRASADOS (mes menor al actual)
            // ============================
            if (f.atrasados == true)
            {
                int mesActual = DateTime.Now.Month;
                query = query.Where(x =>
                    x.c.mes < mesActual &&
                    x.c.estado_pago != "PAGADO"
                );
            }

            // ============================
            // ARMAR DTO FINAL
            // ============================
            return query
                .OrderByDescending(x => x.c.id)
                .Select(x => new CobroClienteDTO
                {
                    id = x.c.id,
                    folio = x.c.folio,                    // ← AGREGAR

                    cliente_id = x.c.cliente_id,
                    cliente = x.cli.razon_social,

                    subtotal = x.c.subtotal,
                    iva = x.c.iva_monto,
                    isr_ret = x.c.isr_ret,
                    iva_ret = x.c.iva_ret,
                    total = x.c.total,

                    mes = x.c.mes,
                    anio = x.c.anio,
                    estado_pago = x.c.estado_pago,

                    comprobante_pdf = x.c.comprobante_pdf   // ← NUEVO

                })
                .ToList();
        }




        //
        // ================================
        //   REGISTRAR NUEVO COBRO
        // ================================
        public Estatus<bool> Registrar(Cobro_Cliente cobro)
        {
            Ensure.ValidarEnteroPositivo(cobro.cliente_id, "El cliente_id no es válido");
            Ensure.ValidarVacio(cobro.descripcion!, "La descripción no puede estar vacía");
            Ensure.ValidarEnteroPositivo(cobro.anio, "El año no es válido");
            Ensure.ValidarEnteroPositivo(cobro.mes, "El mes no es válido");

            // 1) Obtener cliente
            var cliente = _db.clientes.FirstOrDefault(c => c.id == cobro.cliente_id);
            Ensure.ValidarNulo(cliente, "No se encontró el cliente para este cobro.");

            // 2) Generar folio
            int siguienteFolio = (_db.cobros_clientes.Max(x => (int?)x.folio) ?? 0) + 1;
            cobro.folio = siguienteFolio;

            // 3) Calcular IVA normal
            cobro.iva_porcentaje = 16;
            cobro.iva_monto = cobro.subtotal * 0.16m;

            // 4) Calcular retenciones automáticamente
            if (cliente.usa_retenciones)
            {
                cobro.isr_ret = cobro.subtotal * cliente.porc_isr_ret;        // ej 10%
                cobro.iva_ret = cobro.subtotal * cliente.porc_iva_ret;       // ej 0.106667
            }
            else
            {
                cobro.isr_ret = 0;
                cobro.iva_ret = 0;
            }

            // 5) Total final
            cobro.total = cobro.subtotal + cobro.iva_monto - cobro.isr_ret - cobro.iva_ret;

            // 6) Fechas y estado
            cobro.fecha_emision = DateTime.UtcNow;
            cobro.estado_pago = "PENDIENTE";

            _db.cobros_clientes.Add(cobro);
            _db.SaveChanges();

            return Estatus<bool>.OK("Cobro registrado correctamente");
        }


        // ================================
        //   ACTUALIZAR COBRO EXISTENTE
        // ================================

        public Estatus<bool> Actualizar(Cobro_Cliente cobro)
        {
            Ensure.ValidarNulo(cobro, "El objeto cobro no puede ser nulo");

            var registro = Get(cobro.id);

            // =========================
            // CAMPOS NORMALES
            // =========================
            registro.cliente_id = cobro.cliente_id;
            registro.subtotal = cobro.subtotal;
            registro.descripcion = cobro.descripcion;
            registro.iva_porcentaje = cobro.iva_porcentaje;

            // recalcular IVA
            registro.iva_monto = registro.subtotal * (registro.iva_porcentaje / 100m);

            // retenciones
            registro.isr_ret = cobro.isr_ret;
            registro.iva_ret = cobro.iva_ret;

            // total
            registro.total = registro.subtotal
                           + registro.iva_monto
                           - registro.isr_ret
                           - registro.iva_ret;

            registro.mes = cobro.mes;
            registro.anio = cobro.anio;

            // =========================
            // MANEJO ESPECIAL DEL ESTADO
            // =========================

            var estadoActual = registro.estado_pago ?? "PENDIENTE";
            var estadoNuevo = cobro.estado_pago ?? "PENDIENTE";

            // 1) Si estaba PAGADO y lo cambian a PENDIENTE → revertir pago
            if (estadoActual == "PAGADO" && estadoNuevo == "PENDIENTE")
            {
                registro.estado_pago = "PENDIENTE";
                registro.fecha_pago = null;
                registro.comprobante_pdf = null;   // opcional: así ya no se podrá abrir el recibo
            }
            // 2) No permitir marcar PAGADO desde el formulario normal
            else if (estadoActual != "PAGADO" && estadoNuevo == "PAGADO")
            {
                // Forzar a seguir pendiente; el pago se hace solo con ConfirmarPagoYGenerarRecibo
                return Estatus<bool>.Error("Para marcar como pagado use el botón 'Registrar pago'.");
            }
            // 3) Cualquier otro caso (por ejemplo seguir como PENDIENTE)
            else
            {
                registro.estado_pago = estadoNuevo;
                registro.fecha_pago = cobro.fecha_pago;
            }

            _db.SaveChanges();
            return Estatus<bool>.OK("Cobro actualizado correctamente");
        }

        //public Estatus<bool> Actualizar(Cobro_Cliente cobro)
        //{
        //    Ensure.ValidarNulo(cobro, "El objeto cobro no puede ser nulo");

        //    var registro = Get(cobro.id);

        //    registro.cliente_id = cobro.cliente_id;
        //    registro.subtotal = cobro.subtotal;
        //    registro.descripcion = cobro.descripcion;
        //    registro.iva_porcentaje = cobro.iva_porcentaje;

        //    // NUEVO: recalcular IVA y retenciones
        //    registro.iva_monto = registro.subtotal * (registro.iva_porcentaje / 100);

        //    // NUEVO: copiar montos de retenciones desde el DTO
        //    registro.isr_ret = cobro.isr_ret;
        //    registro.iva_ret = cobro.iva_ret;

        //    // NUEVO: total correcto con retenciones
        //    registro.total = registro.subtotal
        //                   + registro.iva_monto
        //                   - registro.isr_ret
        //                   - registro.iva_ret;

        //    registro.mes = cobro.mes;
        //    registro.anio = cobro.anio;
        //    registro.estado_pago = cobro.estado_pago;
        //    registro.fecha_emision = cobro.fecha_emision;
        //    registro.fecha_pago = cobro.fecha_pago;
        //    registro.comprobante_pdf = cobro.comprobante_pdf;

        //    _db.SaveChanges();
        //    return Estatus<bool>.OK("Cobro actualizado correctamente");
        //}


        // ================================
        //   ELIMINAR COBRO
        // ================================
        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.cobros_clientes.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Cobro eliminado correctamente");
        }

        //logica de pagos
        public Estatus<bool> MarcarComoPagado(int id)
        {
            var registro = Get(id);

            if (registro.estado_pago == "PAGADO")
                return Estatus<bool>.Error("Este cobro ya está pagado.");

            registro.estado_pago = "PAGADO";
            registro.fecha_pago = DateTime.UtcNow;

            _db.SaveChanges();
            return Estatus<bool>.OK("Cobro marcado como pagado.");
        }




        // ================================================
        //   GENERAR COBROS ANUALES PARA UN CLIENTE
        // ================================================
        public void GenerateCobrosAnuales(int clienteId, int anio, bool generarSoloMesesFuturos = false)
        {
            var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
            Ensure.ValidarNulo(cliente, "No se encontró el cliente.");

            // ⛔ Si el cliente está inactivo, NO generamos cobros
            if (!cliente.estatus)
                return;

            // Si generarSoloMesesFuturos == true empieza desde el mes actual,
            // si no, desde Enero.
            int mesInicio = generarSoloMesesFuturos
                ? DateTime.Now.Month
                : 1;


            Console.WriteLine($"===> Generando cobros anuales para cliente {clienteId}, año {anio}");


            for (int mes = mesInicio; mes <= 12; mes++)
            {
                // evita cobros duplicados
                bool existe = _db.cobros_clientes.Any(x =>
                    x.cliente_id == clienteId &&
                    x.anio == anio &&
                    x.mes == mes


                );

                if (existe) continue;

                var subtotal = cliente.honorarios_subtotal;
                var ivaMonto = subtotal * 0.16m;

                var isr = cliente.usa_retenciones ? subtotal * (cliente.porc_isr_ret / 100m) : 0;
                var ivaRet = cliente.usa_retenciones ? subtotal * (cliente.porc_iva_ret / 100m) : 0;


                var total = subtotal + ivaMonto - isr - ivaRet;

                int folio = (_db.cobros_clientes.Max(x => (int?)x.folio) ?? 0) + 1;

                var cobro = new Cobro_Cliente
                {
                    cliente_id = clienteId,
                    descripcion = $"Honorarios Profesionales {mes}/{anio}",
                    subtotal = subtotal,
                    iva_porcentaje = 16,
                    iva_monto = ivaMonto,
                    isr_ret = isr,
                    iva_ret = ivaRet,
                    total = total,
                    mes = mes,
                    anio = anio,
                    estado_pago = "PENDIENTE",
                    fecha_emision = DateTime.UtcNow,
                    folio = folio
                };

                Console.WriteLine($"--> Mes {mes}, existe = {existe}");

                _db.cobros_clientes.Add(cobro);
            }

            _db.SaveChanges();
        }


        ///
        public void GenerateCobrosDelMesActual()
        {
            int anio = DateTime.Now.Year;
            int mes = DateTime.Now.Month;

            var clientes = _db.clientes.Where(c => c.estatus).ToList();

            foreach (var c in clientes)
                GenerateCobroMensual(c.id, anio, mes);
        }

        public void GenerateCobroMensual(int clienteId, int anio, int mes)
        {
            var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
            Ensure.ValidarNulo(cliente, "No se encontró el cliente.");

            // Evitar duplicado
            bool existe = _db.cobros_clientes.Any(x =>
                x.cliente_id == clienteId &&
                x.anio == anio &&
                x.mes == mes
            );

            if (existe) return;

            var subtotal = cliente.honorarios_subtotal;
            var ivaMonto = subtotal * 0.16m;

            var isr = cliente.usa_retenciones ? subtotal * (cliente.porc_isr_ret / 100m) : 0;
            var ivaRet = cliente.usa_retenciones ? subtotal * (cliente.porc_iva_ret / 100m) : 0;

            var total = subtotal + ivaMonto - isr - ivaRet;

            int folio = (_db.cobros_clientes.Max(x => (int?)x.folio) ?? 0) + 1;

            var cobro = new Cobro_Cliente
            {
                cliente_id = clienteId,
                descripcion = $"Honorarios Profesionales {mes}/{anio}",
                subtotal = subtotal,
                iva_porcentaje = 16,
                iva_monto = ivaMonto,
                isr_ret = isr,
                iva_ret = ivaRet,
                total = total,
                mes = mes,
                anio = anio,
                estado_pago = "PENDIENTE",
                fecha_emision = DateTime.UtcNow,
                folio = folio
            };

            _db.cobros_clientes.Add(cobro);
            _db.SaveChanges();
        }



        ///




        public string GetEstadoCliente(int clienteId, int anio)
        {
            var cobros = _db.cobros_clientes
                .Where(c => c.cliente_id == clienteId && c.anio == anio)
                .ToList();

            int mesActual = DateTime.Now.Month;

            bool debeMesActual = cobros.Any(c =>
                c.mes == mesActual &&
                c.estado_pago != "PAGADO"
            );

            int atrasados = cobros.Count(c =>
                c.mes < mesActual &&
                c.estado_pago != "PAGADO"
            );

            if (atrasados > 0) return $"Vas atrasado {atrasados} meses";
            if (debeMesActual) return $"Debes {mesActual}";

            return "Estás al corriente";
        }


        //filtros
        public IQueryable<Cobro_Cliente> GetQueryable()
        {
            return _db.cobros_clientes.AsQueryable();
        }



        //confirmar pago
        public Estatus<string> ConfirmarPagoYGenerarRecibo(int cobroId)
        {
            var cobro = Get(cobroId);

            if (cobro.estado_pago == "PAGADO")
                return Estatus<string>.Error("Este cobro ya está pagado.");

            cobro.estado_pago = "PAGADO";
            cobro.fecha_pago = DateTime.UtcNow;


            // 1) Generar PDF del recibo
            var rutaPdf = GenerarReciboPagoPdf(cobro);

            // 2) Guardar ruta
            cobro.comprobante_pdf = rutaPdf;

            _db.SaveChanges();

            return Estatus<string>.OK(rutaPdf);
        }
        public string GenerarReciboPagoPdf(Cobro_Cliente cobro)
        {
            var cliente = _db.clientes.FirstOrDefault(c => c.id == cobro.cliente_id);
            Ensure.ValidarNulo(cliente, "No se encontró el cliente.");

            var certificado = _db.certificados_sat.FirstOrDefault(x => x.cliente_id == cobro.cliente_id);
            Ensure.ValidarNulo(certificado, "No se encontró el certificado SAT del cliente.");

            string rfc = certificado.rfc!;
            string razonCliente = cliente.razon_social!;

            var cultura = new CultureInfo("es-MX");
            string fechaTexto = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", cultura);
            string nombreMesActual = cultura.DateTimeFormat.GetMonthName(cobro.mes);

            string baseSat = _config["SatStoragePath"]!;   // C:\API_asemp_data\SAT

            // === rutas ===
            string relativeDir = Path.Combine("Clientes", $"{cliente.id}_{rfc}", cobro.anio.ToString(), nombreMesActual);
            string fileName = $"recibo_pago_{cobro.anio}_{nombreMesActual}.pdf";
            string relativePath = Path.Combine(relativeDir, fileName);

            string fullPath = Path.Combine(baseSat, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // === datos por mes ===
            var cobrosAnio = _db.cobros_clientes
                .Where(x => x.cliente_id == cobro.cliente_id && x.anio == cobro.anio)
                .ToList();

            var porMes = cobrosAnio
                .GroupBy(x => x.mes)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.id).First());

            string[] nombresMeses =
            {
        "Enero","Febrero","Marzo","Abril","Mayo","Junio",
        "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"
    };

            decimal totalPagado = 0m;

            // Paleta combinada
            const string Accent = "#1F497D";   // azul fuerte
            const string AccentSoft = "#E3ECF9";   // azul claro
            const string MonthHighlight = "#FFF7D6";
            const string BorderGray = "#DDDDDD";

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));

                    // ================== ENCABEZADO (sin imagen) ==================
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text("ASESORÍA EMPRESARIAL")
                              .Bold().FontSize(16).FontColor(Accent);
                        header.Item().AlignCenter().Text("CP Edna Gpe. Martínez R");
                        header.Item().AlignCenter().Text("Priv. Villa Guadalupe No. 02");
                        header.Item().AlignCenter().Text("Col Granja, Nogales, Sonora");
                        header.Item().AlignCenter().Text("Tel. (631) 314-68-12");


                        header.Item().PaddingTop(8)
                              .AlignCenter()
                              .Text(t => t.Span("RECIBO DE PAGO")
                                           .Bold()
                                           .FontSize(13)
                                           .FontColor(Accent));

                        header.Item().PaddingTop(4)
                              .LineHorizontal(0.8f)
                              .LineColor(BorderGray);
                    });

                    // ================== CUERPO ==================
                    page.Content().PaddingTop(10).Column(main =>
                    {
                        // Tarjeta principal CENTRADA
                        main.Item()
                            .AlignCenter()                 // centra el bloque
                            .Element(card =>
                            {
                                card.Width(520)            // ancho fijo de la tarjeta
                                    .Border(0.8f)
                                    .BorderColor(BorderGray)
                                    .Padding(12)
                                    .Column(col =>
                                    {
                                        col.Spacing(5);

                                        // ---- datos generales ----
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                            {
                                                t.Span("Cliente: ").Bold();
                                                t.Span(razonCliente);
                                            });

                                            row.ConstantItem(170).AlignRight().Text(t =>
                                            {
                                                t.Span("Folio: ").Bold();
                                                t.Span($"{cobro.folio:00000}");
                                            });
                                        });

                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                            {
                                                t.Span("RFC: ").Bold();
                                                t.Span(rfc);
                                            });

                                            row.ConstantItem(170).AlignRight().Text(t =>
                                            {
                                                t.Span("Fecha de pago: ").Bold();
                                                t.Span(fechaTexto);
                                            });
                                        });

                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(t =>
                                            {
                                                t.Span("Concepto: ").Bold();
                                                t.Span(cobro.descripcion ??
                                                       $"Honorarios Profesionales {nombreMesActual} {cobro.anio}");
                                            });

                                            row.ConstantItem(170).AlignRight().Text(t =>
                                            {
                                                t.Span("Periodo: ").Bold();
                                                t.Span($"{nombreMesActual} {cobro.anio}");
                                            });
                                        });

                                        col.Item().PaddingTop(6)
                                           .LineHorizontal(0.7f)
                                           .LineColor(BorderGray);

                                        // ---- Título estado de cuenta ----
                                        col.Item().PaddingTop(6).Text(t =>
                                        {
                                            t.Span("Estado de cuenta del año ").Bold();
                                            t.Span(cobro.anio.ToString()).Bold();
                                        });

                                        // ===== tabla de meses (estilo estado de cuenta) =====
                                        col.Item().PaddingTop(4).Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(5);  // descripción
                                                columns.RelativeColumn(2);  // “línea”
                                                columns.ConstantColumn(70); // importe
                                                columns.ConstantColumn(60); // estado
                                            });

                                            IContainer HeaderCell(IContainer c) =>
                                                c.Background(AccentSoft)
                                                 .PaddingVertical(3)
                                                 .PaddingHorizontal(4)
                                                 .Border(0.5f)
                                                 .BorderColor(BorderGray);

                                            IContainer RowCell(IContainer c, bool esActual) =>
                                                c.Background(esActual ? MonthHighlight : "#FFFFFF")
                                                 .PaddingVertical(2)
                                                 .PaddingHorizontal(4)
                                                 .BorderBottom(0.4f)
                                                 .BorderColor(BorderGray);

                                            // Encabezado
                                            table.Header(h =>
                                            {
                                                h.Cell().Element(HeaderCell).Text("OTROS").Bold();
                                                h.Cell().Element(HeaderCell).Text(string.Empty);
                                                h.Cell().Element(HeaderCell).AlignRight().Text("Importe").Bold();
                                                h.Cell().Element(HeaderCell).AlignLeft().Text("Estado").Bold();
                                            });

                                            // Filas de meses
                                            for (int i = 1; i <= 12; i++)
                                            {
                                                string nombreMes = nombresMeses[i - 1];
                                                Cobro_Cliente? cm = porMes.ContainsKey(i) ? porMes[i] : null;

                                                decimal totalMes = cm?.total ?? 0m;
                                                string estado = "-";

                                                if (cm != null && !string.IsNullOrWhiteSpace(cm.estado_pago))
                                                    estado = cm.estado_pago;

                                                if (estado.ToUpper().Contains("PAGAD"))
                                                    totalPagado += totalMes;

                                                string totalTexto = totalMes > 0 ? totalMes.ToString("N2", cultura) : "-";
                                                bool esMesActual = (i == cobro.mes);

                                                table.Cell().Element(c => RowCell(c, esMesActual))
                                                            .Text(t =>
                                                            {
                                                                var span = t.Span($"Honorarios Profesionales {nombreMes} {cobro.anio}");
                                                                if (esMesActual) span.Bold().FontColor(Accent);
                                                            });

                                                table.Cell().Element(c => RowCell(c, esMesActual))
                                                            .AlignCenter()
                                                            .Text("__________");

                                                table.Cell().Element(c => RowCell(c, esMesActual))
                                                            .AlignRight()
                                                            .Text(t =>
                                                            {
                                                                var span = t.Span(totalTexto);
                                                                if (esMesActual) span.Bold().FontColor(Accent);
                                                            });

                                                table.Cell().Element(c => RowCell(c, esMesActual))
                                                            .AlignLeft()
                                                            .Text(t =>
                                                            {
                                                                var span = t.Span(estado);
                                                                if (esMesActual) span.Bold().FontColor(Accent);
                                                            });
                                            }
                                        });

                                        // ---- TOTAL anual ----
                                        col.Item().PaddingTop(6).Row(row =>
                                        {
                                            row.RelativeItem().Background(AccentSoft)
                                               .PaddingVertical(3)
                                               .PaddingHorizontal(6)
                                               .Text("T O T A L").Bold();

                                            row.ConstantItem(110)
                                               .Background(AccentSoft)
                                               .PaddingVertical(3)
                                               .PaddingHorizontal(6)
                                               .AlignRight()
                                               .Text(totalPagado.ToString("N2", cultura))
                                               .Bold();
                                        });

                                        // ===== Detalle de importes =====
                                        col.Item().PaddingTop(10).Text("Detalle de importes").Bold();

                                        col.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem().Text("Subtotal:");
                                            row.ConstantItem(110).AlignRight()
                                               .Text(cobro.subtotal.ToString("C", cultura));
                                        });

                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text($"IVA ({cobro.iva_porcentaje} %):");
                                            row.ConstantItem(110).AlignRight()
                                               .Text(cobro.iva_monto.ToString("C", cultura));
                                        });

                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Retención ISR:");
                                            row.ConstantItem(110).AlignRight()
                                               .Text(cobro.isr_ret.ToString("C", cultura));
                                        });

                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Retención IVA:");
                                            row.ConstantItem(110).AlignRight()
                                               .Text(cobro.iva_ret.ToString("C", cultura));
                                        });

                                        col.Item().PaddingTop(4)
                                           .LineHorizontal(0.7f)
                                           .LineColor(BorderGray);

                                        col.Item().PaddingTop(4).Element(e =>
                                        {
                                            e.Background(AccentSoft)
                                             .Border(0.7f)
                                             .BorderColor(Accent)
                                             .Padding(6)
                                             .Row(row =>
                                             {
                                                 row.RelativeItem().Text("TOTAL PAGADO").Bold();
                                                 row.ConstantItem(110).AlignRight()
                                                     .Text(cobro.total.ToString("C", cultura))
                                                     .Bold();
                                             });
                                        });
                                    });
                            });

                        // Pie de página
                        main.Item().PaddingTop(18)
                                   .AlignRight()
                                   .Text($"H. Nogales, Sonora, a {fechaTexto}");
                    });
                });
            })
            .GeneratePdf(fullPath);

            return relativePath;
        }


        // ================================
        //   ESTADO DE CUENTA (HTML)
        // ================================
        public string GenerarEstadoCuentaHtml(int clienteId, int anio)
        {
            // 1) Traer cliente
            var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
            Ensure.ValidarNulo(cliente, "No se encontró el cliente para el estado de cuenta.");

            // 2) Traer cobros del año
            var cobrosAnio = _db.cobros_clientes
                .Where(x => x.cliente_id == clienteId && x.anio == anio)
                .ToList();

            // Diccionario por mes
            var porMes = cobrosAnio
                .GroupBy(x => x.mes)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.id).First());

            // Nombres de meses
            string[] nombresMeses = new[]
            {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

            var sb = new StringBuilder();
            var cultura = new CultureInfo("es-MX");

            // ============================
            // CSS + estructura base
            // ============================
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"es\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>Estado de cuenta {anio}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
        body {
            font-family: Arial, sans-serif;
            margin: 40px;
            color: #000;
        }
        .header {
            text-align: right;
            font-family: Georgia, serif;
            font-size: 14px;
            line-height: 18px;
        }
        .titulo-principal {
            text-align: center;
            font-family: Georgia, serif;
            font-size: 20px;
            font-weight: bold;
            margin-top: 30px;
            margin-bottom: 5px;
        }
        .divider {
            border-bottom: 2px solid black;
            margin: 0 auto 20px auto;
            width: 60%;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            font-size: 14px;
        }
        td {
            padding: 3px 0;
        }
        .linea {
            border-bottom: 1px solid #555;
            width: 100%;
            display: inline-block;
            margin-bottom: -3px;
        }
        .col-total {
            width: 90px;
            text-align: right;
        }
        .col-estado {
            width: 80px;
        }
        .section-label {
            font-weight: bold;
            margin-top: 15px;
        }
        .total-label {
            font-weight: bold;
            font-size: 16px;
            padding-top: 10px;
        }
        .total-monto {
            font-weight: bold;
            font-size: 16px;
            border-top: 2px solid black;
            border-bottom: 2px solid black;
            width: 150px;
            text-align: right;
            padding: 5px 0;
        }
        .footer {
            margin-top: 40px;
            font-size: 14px;
        }
        .obs {
            margin-top: 30px;
            font-weight: bold;
            text-decoration: underline;
        }
        @media print {
            body { margin: 20mm; }
        }
    ");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // ============================
            // Encabezado
            // ============================
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<strong>ASESORÍA EMPRESARIAL</strong><br>");
            sb.AppendLine("CP Edna Gpe. Martínez R<br>");
            sb.AppendLine("Priv. Villa Guadalupe No. 02<br>");
            sb.AppendLine("Col Granja &nbsp;&nbsp;&nbsp;&nbsp; Nogales, Sonora<br>");
            sb.AppendLine("Tel. (631) 314-68-12");
            sb.AppendLine("</div>");

            sb.AppendLine("<br><br>");

            sb.AppendLine($"<div><strong>EMPRESA:</strong> {cliente.razon_social}</div>");
            sb.AppendLine($"<div><strong>AÑO:</strong> {anio}</div>");

            sb.AppendLine("<div class=\"titulo-principal\">ESTADO DE CUENTA</div>");
            sb.AppendLine("<div class=\"divider\"></div>");

            sb.AppendLine("<br>");

            decimal totalPagado = 0m;

            sb.AppendLine("<table>");
            sb.AppendLine("<tbody>");

            for (int i = 1; i <= 12; i++)
            {
                string nombreMes = nombresMeses[i - 1];
                Cobro_Cliente? cobroMes = porMes.ContainsKey(i) ? porMes[i] : null;

                string descripcion = cobroMes?.descripcion ?? $"Honorarios Profesionales {nombreMes} {anio}";
                decimal total = cobroMes?.total ?? 0m;

                string estado = "Pendiente";
                if (cobroMes != null)
                {
                    // Si no tiene estado o está vacío → Pendiente
                    estado = string.IsNullOrWhiteSpace(cobroMes.estado_pago)
                        ? "Pendiente"
                        : cobroMes.estado_pago;

                    if (estado.ToUpper().Contains("PAGAD"))
                    {
                        totalPagado += total;
                    }
                }

                string totalTexto = total > 0
                    ? total.ToString("N2", cultura)
                    : "-";

                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td>{descripcion}</td>");
                sb.AppendLine("  <td><span class=\"linea\"></span></td>");
                sb.AppendLine($"  <td class=\"col-total\">{totalTexto}</td>");
                sb.AppendLine($"  <td class=\"col-estado\">{estado}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            sb.AppendLine("<br><br>");

            // ============================
            // TOTAL
            // ============================
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("  <td class=\"total-label\">TOTAL</td>");
            sb.AppendLine("  <td></td>");
            sb.AppendLine($"  <td class=\"total-monto\">{totalPagado.ToString("N2", cultura)}</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<br><br>");

            // ============================
            // Pie de página
            // ============================
            var hoy = DateTime.Now;
            string fechaTexto = hoy.ToString("dd 'de' MMMM 'de' yyyy", cultura);

            sb.AppendLine("<div class=\"footer\">");
            sb.AppendLine($"H. Nogales, Sonora, a {fechaTexto}<br><br>");
            sb.AppendLine("<div class=\"obs\">OBSERVACIONES:</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }


        public string GenerarEstadoCuentaPdf(int clienteId, int anio)
        {
            // 1) Obtener HTML
            string html = GenerarEstadoCuentaHtml(clienteId, anio);

            // 2) Definir ruta física donde se guardará
            string basePath = Path.Combine("wwwroot", "comprobantes", anio.ToString(), $"cliente_{clienteId}");
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            string fileName = $"estado_cuenta_{anio}.pdf";
            string fullPath = Path.Combine(basePath, fileName);

            // 3) Configurar conversión
            var converter = new SynchronizedConverter(new PdfTools());

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 15, Right = 15 },
                    Out = fullPath
                },
                Objects =
        {
            new ObjectSettings
            {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8", EnableJavascript = true, LoadImages = true }
            }
        }
            };

            // 4) Generar PDF
            converter.Convert(doc);

            // 5) Guardar ruta relativa en BD
            string rutaWeb = $"/comprobantes/{anio}/cliente_{clienteId}/{fileName}";

            var cliente = _db.clientes.FirstOrDefault(x => x.id == clienteId);
            if (cliente != null)
            {
                // Si quieres registrar la ruta en alguna propiedad del cliente, aquí
                // cliente.ruta_estado_cuenta = rutaWeb;
                // _db.SaveChanges();
            }

            return fullPath;
        }


    }
}
