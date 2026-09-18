using API_asemp.Contextos;
using API_asemp.Models.Tablero;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace API_asemp.Servicios
{
    public class TableroFiscalService : ITableroFiscalService
    {
        private readonly IConfiguration _config;
        private readonly myDbContext _db;

        private readonly XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
        private readonly XNamespace pago20 = "http://www.sat.gob.mx/Pagos20";

        // NUEVO: namespace para TimbreFiscalDigital (para leer UUID).
        private readonly XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

        public TableroFiscalService(IConfiguration config, myDbContext db)
        {
            _config = config;
            _db = db;
        }

        // ============================================================
        // MÉTODO ORIGINAL → TABLERO RESUMIDO (SIN DETALLE)
        // ============================================================

        public TableroIvaDTO GenerarTablero(int clienteId, int anio, int mes)
        {
            var cliente = _db.clientes
                .Include(c => c.certificados_sat)
                .FirstOrDefault(c => c.id == clienteId);

            if (cliente == null)
                throw new Exception("Cliente no encontrado.");

            string? rfc = cliente.certificados_sat
                .Where(c => c.estatus == true && !string.IsNullOrWhiteSpace(c.rfc))
                .OrderByDescending(c => c.fecha_registro)
                .Select(c => c.rfc)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(rfc))
                throw new Exception("El cliente no tiene RFC válido.");

            var dto = new TableroIvaDTO
            {
                ClienteId = clienteId,
                Rfc = rfc!,
                Anio = anio,
                Mes = mes
            };

            string basePath = _config["SatStoragePath"];
            string mesNombre = ObtenerNombreMes(mes);

            string rutaBase = Path.Combine(basePath, "Clientes", $"{clienteId}_{rfc}", anio.ToString(), mesNombre);
            string rutaEmitidos = Path.Combine(rutaBase, "emitidos");
            string rutaRecibidos = Path.Combine(rutaBase, "recibidos");

            if (Directory.Exists(rutaEmitidos))
                ProcesarEmitidos(rutaEmitidos, dto);

            if (Directory.Exists(rutaRecibidos))
                ProcesarRecibidos(rutaRecibidos, dto);

            return dto;
        }

        // ============================================================
        // NUEVO MÉTODO → TABLERO CON DETALLE POR XML
        // No modifica el tablero actual, es solo para exportar/reportes.
        // ============================================================

        public TableroIvaConDetalleDTO GenerarTableroConDetalle(int clienteId, int anio, int mes)
        {
            // 1) Calculamos el mismo resumen que ya usas en el tablero.
            //    Para NO duplicar lógica, reutilizamos GenerarTablero.
            var resumen = GenerarTablero(clienteId, anio, mes);

            // 2) Creamos la lista donde vamos a guardar cada XML procesado.
            var detalleXml = new List<IvaXmlDetalleDTO>();

            // 3) Volvemos a recorrer los XML, pero usando métodos especiales
            //    que llenan la lista de detalle SIN tocar el resumen calculado.
            string basePath = _config["SatStoragePath"];
            string mesNombre = ObtenerNombreMes(mes);

            string rutaBase = Path.Combine(basePath, "Clientes", $"{clienteId}_{resumen.Rfc}", anio.ToString(), mesNombre);
            string rutaEmitidos = Path.Combine(rutaBase, "emitidos");
            string rutaRecibidos = Path.Combine(rutaBase, "recibidos");

            if (Directory.Exists(rutaEmitidos))
                ProcesarEmitidosConDetalle(rutaEmitidos, detalleXml);

            if (Directory.Exists(rutaRecibidos))
                ProcesarRecibidosConDetalle(rutaRecibidos, detalleXml);

            // 4) Devolvemos resumen + detalle.
            return new TableroIvaConDetalleDTO
            {
                Resumen = resumen,
                DetalleXml = detalleXml
            };
        }

        private string ObtenerNombreMes(int mes) => new[]
        {
            "Enero","Febrero","Marzo","Abril","Mayo","Junio",
            "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"
        }[mes - 1];

        // ============================================================
        // EMITIDOS → IVA CAUSADO (como en el sistema externo)
        // ============================================================

        private void ProcesarEmitidos(string ruta, TableroIvaDTO dto)
        {
            foreach (var file in Directory.EnumerateFiles(ruta, "*.xml", SearchOption.AllDirectories))
            {
                var xml = XDocument.Load(file);
                var comp = xml.Root;
                if (comp == null) continue;

                string tipo = comp.Attribute("TipoDeComprobante")?.Value ?? "";
                if (tipo == "N") continue; // nóminas fuera

                // COMPLEMENTOS DE PAGO EMITIDOS → IVA CAUSADO PPD DEL MES
                if (tipo == "P")
                {
                    dto.IvaCausado.PPD += ExtraerIVAPago(xml);
                    // si quisieras también IVA retenido en REP, aquí se sumaría a Retenido
                    continue;
                }

                // FACTURAS I / E → solo PUE al mes, PPD se reconoce cuando hay REP
                bool esNotaCredito = tipo == "E";

                decimal ivaTrasladado = ExtraerIVATrasladado(xml);
                decimal ivaRetenido = ExtraerIVARetenido(xml);

                string metodo = comp.Attribute("MetodoPago")?.Value ?? "";
                int signo = esNotaCredito ? -1 : 1;

                // Sólo PUE va directo a "Contado"
                if (metodo == "PUE")
                {
                    dto.IvaCausado.PUE += signo * ivaTrasladado;
                    dto.IvaCausado.Retenido += signo * ivaRetenido;
                }
                // Si es PPD, NO se registra aquí (solo en el REP)
            }
        }

        // ============================================================
        // RECIBIDOS → IVA ACREDITABLE (como en el sistema externo)
        // ============================================================

        private void ProcesarRecibidos(string ruta, TableroIvaDTO dto)
        {
            foreach (var file in Directory.EnumerateFiles(ruta, "*.xml", SearchOption.AllDirectories))
            {
                var xml = XDocument.Load(file);
                var comp = xml.Root;
                if (comp == null) continue;

                string tipo = comp.Attribute("TipoDeComprobante")?.Value ?? "";
                if (tipo == "N") continue;

                // COMPLEMENTOS DE PAGO RECIBIDOS → IVA ACREDITABLE PPD DEL MES
                if (tipo == "P")
                {
                    dto.IvaAcreditable.PPD += ExtraerIVAPago(xml);
                    continue;
                }

                bool esNotaCredito = tipo == "E";

                decimal ivaTrasladado = ExtraerIVATrasladado(xml);
                decimal ivaRetenido = ExtraerIVARetenido(xml);

                string metodo = comp.Attribute("MetodoPago")?.Value ?? "";
                int signo = esNotaCredito ? -1 : 1;

                if (metodo == "PUE")
                {
                    dto.IvaAcreditable.PUE += signo * ivaTrasladado;
                    dto.IvaAcreditable.Retenido += signo * ivaRetenido;
                }
                // PPD solo en REP
            }
        }

        // ============================================================
        // NUEVO: EMITIDOS CON DETALLE (NO MODIFICA EL RESUMEN)
        // Recorre los XML de emitidos y solo llena la lista de detalle.
        // ============================================================

        private void ProcesarEmitidosConDetalle(string ruta, List<IvaXmlDetalleDTO> detalleXml)
        {
            foreach (var file in Directory.EnumerateFiles(ruta, "*.xml", SearchOption.AllDirectories))
            {
                var xml = XDocument.Load(file);
                var comp = xml.Root;
                if (comp == null) continue;

                string tipo = comp.Attribute("TipoDeComprobante")?.Value ?? "";
                string metodo = comp.Attribute("MetodoPago")?.Value ?? "";
                bool esNotaCredito = tipo == "E";
                int signo = esNotaCredito ? -1 : 1;

                // Crear la base del registro de detalle (datos generales del CFDI).
                var detalle = CrearDetalleBase(xml, file, "Emitido", tipo, metodo);

                if (tipo == "N")
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "Nómina";
                    detalleXml.Add(detalle);
                    continue;
                }

                if (tipo == "P")
                {
                    decimal ivaPago = ExtraerIVAPago(xml);
                    detalle.IvaTrasladado = ivaPago;
                    detalle.IncluidoEnCalculo = ivaPago != 0;
                    // Si quieres, aquí puedes poner MotivoExclusion cuando sea 0.
                    detalleXml.Add(detalle);
                    continue;
                }

                decimal ivaTrasladado = ExtraerIVATrasladado(xml);
                decimal ivaRetenido = ExtraerIVARetenido(xml);

                detalle.IvaTrasladado = signo * ivaTrasladado;
                detalle.IvaRetenido = signo * ivaRetenido;

                if (metodo == "PUE")
                {
                    detalle.IncluidoEnCalculo = true;
                }
                else if (metodo == "PPD")
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "PPD (se considera en REP)";
                }
                else
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "Método de pago no considerado";
                }

                detalleXml.Add(detalle);
            }
        }

        // ============================================================
        // NUEVO: RECIBIDOS CON DETALLE (NO MODIFICA EL RESUMEN)
        // ============================================================

        private void ProcesarRecibidosConDetalle(string ruta, List<IvaXmlDetalleDTO> detalleXml)
        {
            foreach (var file in Directory.EnumerateFiles(ruta, "*.xml", SearchOption.AllDirectories))
            {
                var xml = XDocument.Load(file);
                var comp = xml.Root;
                if (comp == null) continue;

                string tipo = comp.Attribute("TipoDeComprobante")?.Value ?? "";
                string metodo = comp.Attribute("MetodoPago")?.Value ?? "";
                bool esNotaCredito = tipo == "E";
                int signo = esNotaCredito ? -1 : 1;

                var detalle = CrearDetalleBase(xml, file, "Recibido", tipo, metodo);

                if (tipo == "N")
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "Nómina";
                    detalleXml.Add(detalle);
                    continue;
                }

                if (tipo == "P")
                {
                    decimal ivaPago = ExtraerIVAPago(xml);
                    detalle.IvaTrasladado = ivaPago;
                    detalle.IncluidoEnCalculo = ivaPago != 0;
                    detalleXml.Add(detalle);
                    continue;
                }

                decimal ivaTrasladado = ExtraerIVATrasladado(xml);
                decimal ivaRetenido = ExtraerIVARetenido(xml);

                detalle.IvaTrasladado = signo * ivaTrasladado;
                detalle.IvaRetenido = signo * ivaRetenido;

                if (metodo == "PUE")
                {
                    detalle.IncluidoEnCalculo = true;
                }
                else if (metodo == "PPD")
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "PPD (se considera en REP)";
                }
                else
                {
                    detalle.IncluidoEnCalculo = false;
                    detalle.MotivoExclusion = "Método de pago no considerado";
                }

                detalleXml.Add(detalle);
            }
        }

        // ============================================================
        // NUEVO: helper para armar la base de IvaXmlDetalleDTO
        // Lee datos generales: RFCs, UUID, fecha, etc.
        // ============================================================

        private IvaXmlDetalleDTO CrearDetalleBase(
            XDocument xml,
            string rutaArchivo,
            string tipoMovimiento,
            string tipoCfdi,
            string metodoPago)
        {
            var comp = xml.Root!;

            string nombreXml = Path.GetFileName(rutaArchivo);
            string rfcEmisor = comp.Element(cfdi + "Emisor")?.Attribute("Rfc")?.Value ?? "";
            string rfcReceptor = comp.Element(cfdi + "Receptor")?.Attribute("Rfc")?.Value ?? "";

            DateTime? fecha = null;
            string? fechaStr = comp.Attribute("Fecha")?.Value;
            if (DateTime.TryParse(fechaStr, out var f))
                fecha = f;

            string uuid = comp
                .Element(cfdi + "Complemento")?
                .Element(tfd + "TimbreFiscalDigital")?
                .Attribute("UUID")?.Value ?? string.Empty;

            return new IvaXmlDetalleDTO
            {
                TipoMovimiento = tipoMovimiento,
                RutaArchivo = rutaArchivo,
                NombreXml = nombreXml,
                TipoCfdi = tipoCfdi,
                MetodoPago = metodoPago,
                RfcEmisor = rfcEmisor,
                RfcReceptor = rfcReceptor,
                Fecha = fecha,
                Uuid = uuid,
                IncluidoEnCalculo = false,  // se ajusta después
                MotivoExclusion = string.Empty
            };
        }

        // ============================================================
        // IVA TRASLADADO FACTURA (sin doble suma)
        // ============================================================

        private decimal ExtraerIVATrasladado(XDocument xml)
        {
            decimal totalIVA = 0m;
            var comp = xml.Root;
            if (comp == null) return 0m;

            // 1) IVA GLOBAL EN COMPROBANTE
            var globalTraslados = comp
                .Element(cfdi + "Impuestos")?
                .Element(cfdi + "Traslados")?
                .Elements(cfdi + "Traslado");

            bool tieneIvaGlobal = false;

            if (globalTraslados != null)
            {
                foreach (var t in globalTraslados)
                {
                    if ((string)t.Attribute("Impuesto") == "002" &&
                        (string)t.Attribute("TipoFactor") == "Tasa")
                    {
                        totalIVA += ParseDecimal(t.Attribute("Importe")?.Value);
                        tieneIvaGlobal = true;
                    }
                }
            }

            // 2) Si NO hay IVA global, sumar por concepto (evita doble conteo)
            if (!tieneIvaGlobal)
            {
                var trasladosConceptos =
                    xml.Descendants(cfdi + "Concepto")
                       .Elements(cfdi + "Impuestos")
                       .Elements(cfdi + "Traslados")
                       .Elements(cfdi + "Traslado");

                foreach (var t in trasladosConceptos)
                {
                    if ((string)t.Attribute("Impuesto") == "002" &&
                        (string)t.Attribute("TipoFactor") == "Tasa")
                    {
                        totalIVA += ParseDecimal(t.Attribute("Importe")?.Value);
                    }
                }
            }

            return totalIVA;
        }

        // ============================================================
        // IVA RETENIDO FACTURA (002)
        // ============================================================

        private decimal ExtraerIVARetenido(XDocument xml)
        {
            decimal totalRetenido = 0m;

            var retenciones = xml
                .Descendants(cfdi + "Retencion")
                .Where(r => (string)r.Attribute("Impuesto") == "002");

            foreach (var r in retenciones)
            {
                totalRetenido += ParseDecimal(r.Attribute("Importe")?.Value);
            }

            return totalRetenido;
        }

        // ============================================================
        // IVA DE COMPLEMENTO DE PAGO (Pagos 2.0) → siempre PPD
        // ============================================================

        private decimal ExtraerIVAPago(XDocument xml)
        {
            decimal total = 0m;

            var traslados = xml.Descendants(pago20 + "TrasladoP");

            foreach (var t in traslados)
            {
                // En Pagos 2.0 normalmente viene Importep
                string importe =
                    t.Attribute("ImporteP")?.Value ??
                    t.Attribute("Importe")?.Value ??
                    "0";

                total += ParseDecimal(importe);
            }

            return total;
        }

        // ============================================================
        // PARSEO SEGURO
        // ============================================================

        private decimal ParseDecimal(string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return 0m;

            decimal.TryParse(
                val,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result);

            return result;
        }
    }
}
