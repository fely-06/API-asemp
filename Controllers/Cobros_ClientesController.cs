using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Authorization; // ← necesario para [Authorize]
using Microsoft.AspNetCore.Mvc;
using API_asemp.Models;


namespace API_asemp.Controllers
{
    // Controlador para manejar los cobros de los clientes.
    // Ruta base: /cobros
   

    [ApiController]
    [Route("cobros")]
    [Authorize] // ← requiere token JWT válido para acceder
    public class Cobros_ClientesController : ControllerBase
    {
        private readonly Cobros_Clientes _cobros;
        private readonly myDbContext _db;

        // Se recibe la clase Cobros_Clientes desde la capa de datos
        public Cobros_ClientesController(Cobros_Clientes cobrosService, myDbContext db)
        {
            _cobros = cobrosService;
            _db = db;
        }

        // ===============================================================
        // ========== MÉTODOS DISPONIBLES EN LA API ======================
        // ===============================================================

        /// <summary>
        /// Devuelve todos los cobros registrados.
        /// </summary>
        /// 

        //   [HttpGet("lista")]
        //   public IActionResult GetLista(
        //[FromQuery] int? anio,
        //[FromQuery] int? mes,
        //[FromQuery] int? clienteId,
        //[FromQuery] bool? todos,
        //[FromQuery] bool? pendientes,
        //[FromQuery] bool? pagados,
        //[FromQuery] bool? atrasados)
        //   {
        //       try
        //       {
        //           int anioActual = DateTime.Now.Year;
        //           int mesActual = DateTime.Now.Month;

        //           int anioFiltro = anio ?? anioActual;
        //           int mesFiltro = mes ?? mesActual;

        //           // ===============================================
        //           // ✔ LISTA COMPLETA (sin filtros)
        //           // ===============================================
        //           if (todos == true)
        //           {
        //               // No hacemos nada especial
        //               // Solo no aplicamos otros filtros secundarios

        //               //var listaCompleta = _cobros.GetQueryable()
        //               //    .OrderByDescending(c => c.id)
        //               //    .ToList();

        //               //return Ok(listaCompleta);
        //           }

        //           // ===============================================
        //           // ✔ CONSULTA BASE
        //           // ===============================================
        //           var query = _cobros.GetQueryable()
        //               .Where(c => c.anio == anioFiltro);

        //           // filtro por mes SI lo mandaron
        //           if (mes != null)
        //               query = query.Where(c => c.mes == mesFiltro);
        //           else
        //               query = query.Where(c => c.mes == mesActual);

        //           // filtro por cliente
        //           if (clienteId != null && clienteId > 0)
        //               query = query.Where(c => c.cliente_id == clienteId);

        //           // ===============================================
        //           // ✔ FILTRO: SOLO PENDIENTES
        //           // ===============================================
        //           if (pendientes == true)
        //           {
        //               query = query.Where(c => c.estado_pago != "PAGADO");
        //           }

        //           // ===============================================
        //           // ✔ FILTRO: SOLO PAGADOS
        //           // ===============================================
        //           if (pagados == true)
        //           {
        //               query = query.Where(c => c.estado_pago == "PAGADO");
        //           }

        //           // ===============================================
        //           // ✔ FILTRO: SOLO ATRASADOS
        //           // (meses anteriores al actual y NO pagados)
        //           // ===============================================
        //           if (atrasados == true)
        //           {
        //               query = query.Where(c =>
        //                   c.mes < mesActual &&
        //                   c.estado_pago != "PAGADO"
        //               );
        //           }

        //           var lista = query
        //               .OrderByDescending(c => c.id)
        //               .ToList();

        //           return Ok(lista);
        //       }
        //       catch (Exception e)
        //       {
        //           return BadRequest(e.Message);
        //       }
        //   }

        //[HttpGet("lista")]
        //public IActionResult GetLista()
        //{
        //    try
        //    {
        //        var lista = _cobros.GetLista();
        //        return Ok(lista);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}




        //

        [HttpGet("lista")]
        public IActionResult GetLista([FromQuery] CobrosFiltro filtro)
        {
            try
            {
                var rol = User.FindFirst("role")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                var clienteIdClaim = User.FindFirst("cliente_id")?.Value;

                if (rol == "Cliente" && int.TryParse(clienteIdClaim, out int clienteId))
                {
                    // 🔒 Forzar cliente desde JWT
                    filtro.cliente_id = clienteId;
                }

                var resultado = _cobros.Buscar(filtro);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        //[HttpGet("lista")]
        //public IActionResult GetLista([FromQuery] CobrosFiltro filtro)
        //{
        //    try
        //    {
        //        var resultado = _cobros.Buscar(filtro);
        //        return Ok(resultado);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}


        //generar cobro
        [HttpPost("generar-mes")]
        public IActionResult GenerarMes()
        {
            try
            {
                _cobros.GenerateCobrosDelMesActual();
                return Ok("Cobros del mes generados correctamente.");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




        /// <summary>
        /// Busca un cobro por su ID.
        /// </summary>
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _cobros.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Registra un nuevo cobro.
        /// </summary>
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Cobro_Cliente cobro)
        {
            try
            {
                Ensure.ValidarNulo(cobro, "El objeto cobro no puede ser nulo.");

                var resultado = _cobros.Registrar(cobro);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Edita un cobro existente.
        /// </summary>
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Cobro_Cliente cobro)
        {
            try
            {
                Ensure.ValidarNulo(cobro, "El objeto cobro no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                cobro.id = id;

                var resultado = _cobros.Actualizar(cobro);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Elimina un cobro por su ID.
        /// </summary>
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _cobros.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        //pagar
        [HttpPut("pagar/{id}")]
        public IActionResult Pagar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("ID inválido.");

                var resultado = _cobros.MarcarComoPagado(id);

                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }







        // ================================================
        //   GENERAR COBROS PARA TODOS LOS CLIENTES
        // ================================================
        [HttpPost("generar-anio/{anio}")]
        public IActionResult GenerarAnio(int anio)
        {
            try
            {
                var listaClientes = _db.clientes
                    .Where(c => c.estatus == true)
                    .Select(c => c.id)
                    .ToList();

                foreach (var id in listaClientes)
                    _cobros.GenerateCobrosAnuales(id, anio);

                return Ok("Cobros generados correctamente.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Estado de cuenta anual de un cliente (HTML).
        /// </summary>
        [HttpGet("estado-cuenta/{clienteId:int}/{anio:int}")]
        public IActionResult EstadoCuenta(int clienteId, int anio)
        {
            try
            {
                if (clienteId <= 0)
                    return BadRequest("El clienteId no es válido.");

                if (anio <= 0)
                    return BadRequest("El año no es válido.");

                var html = _cobros.GenerarEstadoCuentaHtml(clienteId, anio);
                return Content(html, "text/html; charset=utf-8");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        /// <summary>
        /// Genera y descarga el estado de cuenta en PDF.
        /// </summary>
        [HttpGet("estado-cuenta-pdf/{clienteId:int}/{anio:int}")]
        public IActionResult EstadoCuentaPdf(int clienteId, int anio)
        {
            try
            {
                if (clienteId <= 0)
                    return BadRequest("El clienteId no es válido.");

                if (anio <= 0)
                    return BadRequest("El año no es válido.");

                var ruta = _cobros.GenerarEstadoCuentaPdf(clienteId, anio);

                var bytes = System.IO.File.ReadAllBytes(ruta);
                var fileName = $"estado_cuenta_{anio}.pdf";

                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




        //filtros
        [HttpGet("filtrar")]
        public IActionResult Filtrar([FromQuery] int? anio, [FromQuery] int? mes, [FromQuery] int? clienteId)
        {
            try
            {
                int anioActual = DateTime.Now.Year;
                int mesActual = DateTime.Now.Month;

                // valores por defecto
                int anioFiltro = anio ?? anioActual;
                int mesFiltro = mes ?? mesActual;

                var query = _cobros.GetQueryable(); // lo creamos abajo

                // filtrar por año
                query = query.Where(c => c.anio == anioFiltro);

                // filtrar por mes (solo si mandan mes en query)
                if (mes != null)
                    query = query.Where(c => c.mes == mesFiltro);

                // filtrar por cliente
                if (clienteId != null && clienteId > 0)
                    query = query.Where(c => c.cliente_id == clienteId);

                var lista = query
                    .OrderByDescending(c => c.id)
                    .ToList();

                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        // ===============================================================
        // CONFIRMAR PAGO Y GENERAR RECIBO PDF
        // ===============================================================
     
        [HttpPost("confirmar-pago")]
        public IActionResult ConfirmarPago([FromForm] ConfirmarPagoDTO body)
        {
            if (body.cobro_id <= 0)
                return BadRequest("ID de cobro inválido.");

            var resultado = _cobros.ConfirmarPagoYGenerarRecibo(body.cobro_id);

            if (!resultado.IsSuccess)
                return BadRequest(resultado.Mensaje);

            return Ok(resultado.toJson());
        }




        // ===============================================================
        // VER RECIBO PDF DE UN COBRO
        // ===============================================================
        [HttpGet("recibo/{id:int}")]
        public IActionResult VerRecibo(int id, [FromServices] IConfiguration config)
        {
            var cobro = _cobros.Get(id);

            if (string.IsNullOrWhiteSpace(cobro.comprobante_pdf))
                return NotFound("Este cobro no tiene recibo generado.");

            string baseSat = config["SatStoragePath"]!;

            // En BD está algo como: Clientes/25_RFC/2025/enero/recibo_pago_2025_enero.pdf
            string relative = cobro.comprobante_pdf.Replace("/", Path.DirectorySeparatorChar.ToString());
            string fullPath = Path.Combine(baseSat, relative);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("El archivo del recibo no existe en el servidor.");

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return File(bytes, "application/pdf", fileName);
        }



        //
        // ===============================================================
        // ¿EL CLIENTE TIENE PAGOS PENDIENTES?
        // ===============================================================
        [Authorize(Roles = "Cliente")]
        [HttpGet("cliente/{clienteId}/pendientes")]
        public IActionResult TienePagosPendientes(int clienteId)
        {
            try
            {
                if (clienteId <= 0)
                    return BadRequest("El clienteId no es válido.");

                // Busca PENDIENTES o NO PAGADOS
                bool tienePendientes = _db.cobros_clientes
                    .Any(c => c.cliente_id == clienteId && c.estado_pago != "PAGADO");

                return Ok(new { pendiente = tienePendientes });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




    }
}
