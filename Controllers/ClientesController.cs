using API_asemp.Contextos;
using API_asemp.Datos;
using API_asemp.Models;
using API_asemp.Models.BD;
using API_asemp.Servicios;
using API_asemp.Utilerias;
using Herramientas.Validaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API_asemp.Controllers
{
    // ============================================================
    // CONTROLADOR: CLIENTES
    // ------------------------------------------------------------
    // Maneja CRUD de clientes y sus certificados SAT
    // Ruta base: /clientes
    // ============================================================
    [RequireAccion("clientes.ver")]
    [ApiController]
    [Route("clientes")]
    [Authorize] // ← exige JWT válido
    public class ClientesController : ControllerBase
    {
        private readonly Clientes _clientes;
        private readonly Cobros_Clientes _cobros;

        // en la clase ClientesController:
        private readonly myDbContext _db;


        // ------------------------------------------------------------
        // Constructor: se inyecta el servicio de datos "Clientes"
        // ------------------------------------------------------------
        public ClientesController(Clientes clientesService, Cobros_Clientes cobrosService, myDbContext db)
        {
            _clientes = clientesService;
            _cobros = cobrosService;
            _db = db;
        }

        // ============================================================
        // MÉTODOS DE CONSULTA
        // ============================================================

        /// <summary>
        /// Devuelve todos los clientes registrados (sin certificados).
        /// </summary>
        [RequireAccion("clientes.ver")]
        [HttpGet("lista")]
        public IActionResult GetLista()
        {
            try
            {
                var lista = _clientes.GetLista();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Devuelve todos los clientes con su RFC y vigencias del certificado.
        /// </summary>
        [RequireAccion("clientes.ver")]
        [HttpGet("lista-certificados")]
        [AllowAnonymous] // ← solo durante pruebas
        public IActionResult GetListaConCertificados()
        {
            try
            {
                var lista = _clientes.GetListaConCertificados();
                return Ok(lista);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca un cliente simple por su ID (sin incluir certificados).
        /// </summary>
        [RequireAccion("clientes.ver")]
        [HttpGet("buscar/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _clientes.Get(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Busca un cliente con sus certificados SAT incluidos.
        /// </summary>
        [RequireAccion("clientes.ver")]
        [HttpGet("buscar-con-detalle/{id}")]
        public IActionResult GetByIdClienteCertificado(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _clientes.GetClienteCertificado(id);
                return Ok(resultado);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // ============================================================
        // MÉTODOS CRUD
        // ============================================================

        /// <summary>
        /// Registra un nuevo cliente (sin archivos).
        /// </summary>
        [RequireAccion("clientes.crear")]
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] Cliente cliente)
        {
            try
            {
                Ensure.ValidarNulo(cliente, "El objeto cliente no puede ser nulo.");

                // Registrar y obtener el cliente con ID real
                var clienteCreado = _clientes.Registrar(cliente);

                // =============================
                // GENERAR COBROS AUTOMÁTICOS
                // =============================
                int anioActual = DateTime.Now.Year;
                _cobros.GenerateCobrosAnuales(clienteCreado.id, anioActual, true);

                return Ok(new
                {
                    mensaje = "Cliente registrado correctamente",
                    cliente = clienteCreado
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }




        ///// <summary>
        ///// Registra un cliente junto con sus certificados (.cer, .key, .pfx)
        ///// </summary>
        //[HttpPost("registrar-completo")]
        //[AllowAnonymous]
        //[RequestSizeLimit(20_000_000)]
        //public async Task<IActionResult> RegistrarClienteConCertificado(
        //    [FromServices] CertificadosService certificadosService,
        //    [FromForm] ClienteCertDto dto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new
        //        {
        //            mensaje = "Datos inválidos",
        //            errores = ModelState
        //        });
        //    }

        //    if (dto == null)
        //        return BadRequest(new { mensaje = "No se recibieron los datos del cliente." });

        //    // 1) Si el cliente existe, leer estatus anterior
        //    bool estabaActivo = false;
        //    if (dto.id > 0)
        //    {
        //        var clienteAntes = _db.clientes.FirstOrDefault(c => c.id == dto.id);
        //        if (clienteAntes != null)
        //            estabaActivo = clienteAntes.estatus;
        //    }

        //    // 2) Ejecutar el servicio (crea o actualiza)
        //    var resultado = await certificadosService.RegistrarClienteYCertificado(dto);

        //    if (!resultado.Ok)
        //        return BadRequest(new { mensaje = resultado.Mensaje });

        //    // 3) Leer cliente desde la BD YA ACTUALIZADO
        //    dynamic datos = resultado.Datos;
        //    int clienteId = datos.cliente_id;

        //    var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
        //    if (cliente == null)
        //        return BadRequest(new { mensaje = "Error inesperado: cliente no encontrado tras registrar." });

        //    bool nuevoEstatus = cliente.estatus;

        //    int anioActual = DateTime.Now.Year;
        //    int mesActual = DateTime.Now.Month;

        //    // 4) APLICAR LÓGICA DE COBROS
        //    if (dto.id == 0)
        //    {
        //        // CLIENTE NUEVO
        //        if (nuevoEstatus)
        //            _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
        //    }
        //    else
        //    {
        //        // CLIENTE EDITADO
        //        if (estabaActivo && !nuevoEstatus)
        //        {
        //            // ACTIVO → INACTIVO = borrar cobros futuros no pagados
        //            var futuros = _db.cobros_clientes
        //                .Where(c => c.cliente_id == clienteId &&
        //                            c.anio == anioActual &&
        //                            c.mes > mesActual &&
        //                            c.estado_pago != "PAGADO")
        //                .ToList();

        //            if (futuros.Any())
        //            {
        //                _db.cobros_clientes.RemoveRange(futuros);
        //                _db.SaveChanges();
        //            }
        //        }
        //        else if (!estabaActivo && nuevoEstatus)
        //        {
        //            // INACTIVO → ACTIVO = generar cobros faltantes
        //            _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
        //        }
        //    }

        //    // 5) Respuesta
        //    return Ok(new
        //    {
        //        mensaje = dto.id == 0
        //            ? "Cliente registrado correctamente"
        //            : "Cliente actualizado correctamente",
        //        resultado.Datos
        //    });
        //}
        [HttpPost("registrar-completo")]
        [AllowAnonymous]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> RegistrarClienteConCertificado(
    [FromServices] CertificadosService certificadosService,
    [FromForm] ClienteCertDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    mensaje = "Datos inválidos",
                    errores = ModelState
                });
            }

            if (dto == null)
                return BadRequest(new { mensaje = "No se recibieron los datos del cliente." });

            // =====================================================================
            // 1️⃣  INICIAR TRANSACCIÓN
            // =====================================================================
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // 2️⃣  Si el cliente existe, guardar estatus previo
                bool estabaActivo = false;

                 if (dto.id > 0)
                {
                    var clienteAntes = _db.clientes.FirstOrDefault(c => c.id == dto.id);
                    if (clienteAntes != null)
                        estabaActivo = clienteAntes.estatus;
                }

                // 3️⃣  Registrar cliente + certificados (TU SERVICIO)
                var resultado = await certificadosService.RegistrarClienteYCertificado(dto);

                if (!resultado.Ok)
                {
                    await tx.RollbackAsync();   // ❗ REVERSA TODO
                    return BadRequest(new { mensaje = resultado.Mensaje });
                }

                dynamic datos = resultado.Datos;
                int clienteId = datos.cliente_id;

                var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
                if (cliente == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mensaje = "Error inesperado: cliente no encontrado tras registrar." });
                }

                bool nuevoEstatus = cliente.estatus;

                int anioActual = DateTime.Now.Year;
                int mesActual = DateTime.Now.Month;

                // =====================================================================
                // 4️⃣  LÓGICA DE COBROS (DENTRO DE LA TRANSACCIÓN)
                // =====================================================================

                if (dto.id == 0)
                {
                    // CLIENTE NUEVO
                    if (nuevoEstatus)
                        _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
                }
                else
                {
                    // CLIENTE EDITADO
                    if (estabaActivo && !nuevoEstatus)
                    {
                        // ACTIVO → INACTIVO = borrar cobros futuros no pagados
                        var futuros = _db.cobros_clientes
                            .Where(c => c.cliente_id == clienteId &&
                                        c.anio == anioActual &&
                                        c.mes > mesActual &&
                                        c.estado_pago != "PAGADO")
                            .ToList();

                        if (futuros.Any())
                        {
                            _db.cobros_clientes.RemoveRange(futuros);
                        }
                    }
                    else if (!estabaActivo && nuevoEstatus)
                    {
                        // INACTIVO → ACTIVO = generar cobros faltantes
                        _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
                    }
                }

                // Guardar cambios y confirmar operación
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // =====================================================================
                // 5️⃣  RESPUESTA OK
                // =====================================================================
                return Ok(new
                {
                    mensaje = dto.id == 0
                        ? "Cliente registrado correctamente"
                        : "Cliente actualizado correctamente",
                    resultado.Datos
                });
            }
            catch (Exception ex)
            {
                // ❗ Cae aquí si falla cualquier cosa: certificado, firma, cobros, etc.
                await tx.RollbackAsync();
                return BadRequest(new { mensaje = "Error interno durante el registro", detalle = ex.Message });
            }
        }



        //    [HttpPost("registrar-completo")]
        //    [AllowAnonymous]
        //    [RequestSizeLimit(20_000_000)]
        //    public async Task<IActionResult> RegistrarClienteConCertificado(
        //[FromServices] CertificadosService certificadosService,
        //[FromForm] ClienteCertDto dto)
        //    {
        //        if (dto == null)
        //            return BadRequest("No se recibieron los datos del cliente.");

        //        // ===========================================
        //        // 1) Si el cliente existe, leer estatus anterior
        //        // ===========================================
        //        bool estabaActivo = false;
        //        if (dto.id > 0)
        //        {
        //            var clienteAntes = _db.clientes.FirstOrDefault(c => c.id == dto.id);
        //            if (clienteAntes != null)
        //                estabaActivo = clienteAntes.estatus;
        //        }

        //        // ===========================================
        //        // 2) Ejecutar el servicio (crea o actualiza)
        //        // ===========================================
        //        var resultado = await certificadosService.RegistrarClienteYCertificado(dto);

        //        if (!resultado.Ok)
        //            return BadRequest(new { message = resultado.Mensaje });

        //        // ===========================================
        //        // 3) Leer cliente desde la BD YA ACTUALIZADO
        //        // ===========================================
        //        dynamic datos = resultado.Datos;
        //        int clienteId = datos.cliente_id;

        //        var cliente = _db.clientes.FirstOrDefault(c => c.id == clienteId);
        //        if (!resultado.Ok)
        //        {
        //            return BadRequest(resultado.Mensaje);  // string plano
        //        }
        //        if (cliente == null)
        //        return BadRequest("Error inesperado: cliente no encontrado tras registrar.");

        //        bool nuevoEstatus = cliente.estatus;

        //        int anioActual = DateTime.Now.Year;
        //        int mesActual = DateTime.Now.Month;

        //        // ===========================================
        //        // 4) APLICAR LÓGICA DE COBROS
        //        // ===========================================

        //        if (dto.id == 0)
        //        {
        //            // CLIENTE NUEVO
        //            if (nuevoEstatus)
        //                _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
        //        }
        //        else
        //        {
        //            // CLIENTE EDITADO
        //            if (estabaActivo && !nuevoEstatus)
        //            {
        //                // ACTIVO → INACTIVO = borrar cobros futuros no pagados
        //                var futuros = _db.cobros_clientes
        //                    .Where(c => c.cliente_id == clienteId &&
        //                                c.anio == anioActual &&
        //                                c.mes > mesActual &&
        //                                c.estado_pago != "PAGADO")
        //                    .ToList();

        //                if (futuros.Any())
        //                {
        //                    _db.cobros_clientes.RemoveRange(futuros);
        //                    _db.SaveChanges();
        //                }
        //            }
        //            else if (!estabaActivo && nuevoEstatus)
        //            {
        //                // INACTIVO → ACTIVO = generar cobros faltantes
        //                _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);
        //            }
        //        }

        //        // ===========================================
        //        // 5) Respuesta
        //        // ===========================================
        //        return Ok(new
        //        {
        //            mensaje = dto.id == 0
        //                ? "Cliente registrado correctamente"
        //                : "Cliente actualizado correctamente",
        //            resultado.Datos
        //        });
        //    }


        //    [HttpPost("registrar-completo")]
        //    [AllowAnonymous]
        //    [RequestSizeLimit(20_000_000)]
        //    public async Task<IActionResult> RegistrarClienteConCertificado(
        //[FromServices] CertificadosService certificadosService,
        //[FromForm] ClienteCertDto dto)
        //    {
        //        if (dto == null)
        //            return BadRequest("No se recibieron los datos del cliente ni los archivos del certificado.");

        //        var resultado = await certificadosService.RegistrarClienteYCertificado(dto);

        //        if (!resultado.Ok)
        //            return BadRequest(new { message = resultado.Mensaje });

        //        // ============ EXTRAER CLIENTE ID ================
        //        dynamic datos = resultado.Datos;
        //        int clienteId = datos.cliente_id;   // ← AQUÍ ES EL FIX

        //        // ============ GENERAR COBROS AUTOMÁTICOS ============
        //        int anioActual = DateTime.Now.Year;
        //        _cobros.GenerateCobrosAnuales(clienteId, anioActual, true);

        //        return Ok(new
        //        {
        //            mensaje = "Cliente y certificado registrados correctamente",
        //            resultado.Datos
        //        });
        //    }

        //[RequireAccion("clientes.crear")]
        //[HttpPost("registrar-completo")]
        //[AllowAnonymous] // ← solo mientras pruebas
        //[RequestSizeLimit(20_000_000)]
        //public async Task<IActionResult> RegistrarClienteConCertificado(
        //    [FromServices] CertificadosService certificadosService,
        //    [FromForm] ClienteCertDto dto)
        //{
        //    if (dto == null)
        //        return BadRequest("No se recibieron los datos del cliente ni los archivos del certificado.");

        //    var resultado = await certificadosService.RegistrarClienteYCertificado(dto);

        //    if (!resultado.Ok)
        //        return BadRequest(new { message = resultado.Mensaje });

        //    return Ok(resultado.Datos);
        //}

        /// <summary>
        /// Actualiza los datos de un cliente.
        /// </summary>
        [RequireAccion("clientes.editar")]
        [HttpPut("editar/{id}")]
        public IActionResult Editar(int id, [FromBody] Cliente cliente)
        {
            try
            {
                Ensure.ValidarNulo(cliente, "El objeto cliente no puede ser nulo.");
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                cliente.id = id;

                var resultado = _clientes.Actualizar(cliente);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [RequireAccion("clientes.editar")]
        [HttpPut("editar-completo/{id}")]
        [AllowAnonymous] // solo mientras pruebas
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> EditarClienteConCertificado(
    int id,
    [FromServices] CertificadosService certificadosService,
    [FromForm] ClienteCertDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID del cliente no es válido.");

                // 1️⃣ Buscar cliente existente
                var cliente = _clientes.GetClienteCertificado(id);
                if (cliente == null)
                    return NotFound("No se encontró el cliente.");

                // Guardamos estatus anterior
                bool estabaActivo = cliente.estatus;

                // 2️⃣ Actualizar datos generales
                cliente.razon_social = dto.razon_social ?? cliente.razon_social;
                cliente.telefono = dto.telefono ?? cliente.telefono;
                cliente.correo_electronico = dto.correo_electronico ?? cliente.correo_electronico;
                cliente.direccion = dto.direccion ?? cliente.direccion;
                cliente.honorarios_subtotal = dto.honorarios_subtotal != 0 ? dto.honorarios_subtotal : cliente.honorarios_subtotal;
                cliente.estatus = dto.estatus; // ← Aquí puede cambiar de activo ⇄ inactivo
                cliente.usuario_id = dto.usuario_id != 0 ? dto.usuario_id : cliente.usuario_id;

                // 3️⃣ Obtener o crear certificado
                var cert = cliente.certificados_sat.FirstOrDefault();
                if (cert == null)
                {
                    cert = new CertificadoSAT
                    {
                        cliente_id = cliente.id,
                        fecha_registro = DateTime.UtcNow,
                        estatus = true
                    };
                    cliente.certificados_sat.Add(cert);
                }

                // 4️⃣ Actualizar archivos si llegan nuevos
                if (dto.cer != null)
                {
                    var rutaCer = await certificadosService.GuardarArchivo(dto.cer, "cer", cliente.id, dto.rfc ?? cert.rfc ?? "SIN_RFC");
                    cert.archivo_cer = rutaCer;
                }

                if (dto.key != null)
                {
                    var rutaKey = await certificadosService.GuardarArchivo(dto.key, "key", cliente.id, dto.rfc ?? cert.rfc ?? "SIN_RFC");
                    cert.archivo_key = rutaKey;
                }

                // 5️⃣ Actualizar datos del certificado
                cert.rfc = dto.rfc ?? cert.rfc;
                cert.contrasena = dto.contrasena ?? cert.contrasena;
                cert.fecha_vigencia_inicio = dto.fecha_vigencia_inicio ?? cert.fecha_vigencia_inicio;
                cert.fecha_vigencia_fin = dto.fecha_vigencia_fin ?? cert.fecha_vigencia_fin;

                // Guardar cambios del cliente y certificado
                _clientes.GuardarCambios();


                // ============================================================
                // 🔥🔥 LÓGICA DE COBROS POR CAMBIO DE ESTATUS 🔥🔥
                // ============================================================

                int mesActual = DateTime.Now.Month;
                int anioActual = DateTime.Now.Year;

                // ⚠️ Cliente pasó de ACTIVO → INACTIVO
                if (estabaActivo && !cliente.estatus)
                {
                    var futuros = _db.cobros_clientes
                        .Where(c => c.cliente_id == cliente.id &&
                                    c.anio == anioActual &&
                                    c.mes > mesActual &&
                                    c.estado_pago != "PAGADO")
                        .ToList();

                    if (futuros.Any())
                    {
                        _db.cobros_clientes.RemoveRange(futuros);
                        _db.SaveChanges();
                    }
                }

                // ⚠️ Cliente pasó de INACTIVO → ACTIVO
                if (!estabaActivo && cliente.estatus)
                {
                    _cobros.GenerateCobrosAnuales(cliente.id, anioActual, true);
                }

                // ============================================================


                return Ok(new
                {
                    mensaje = "Cliente actualizado correctamente",
                    cliente.id,
                    cliente.razon_social,
                    cert.rfc
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al actualizar cliente", detalle = ex.Message });
            }
        }

        //    [RequireAccion("clientes.editar")]
        //    [HttpPut("editar-completo/{id}")]
        //    [AllowAnonymous] // solo mientras pruebas
        //    [RequestSizeLimit(20_000_000)]
        //    public async Task<IActionResult> EditarClienteConCertificado(
        //int id,
        //[FromServices] CertificadosService certificadosService,
        //[FromForm] ClienteCertDto dto)
        //    {
        //        try
        //        {
        //            if (id <= 0)
        //                return BadRequest("El ID del cliente no es válido.");

        //            // 1️⃣ Buscar cliente existente
        //            var cliente = _clientes.GetClienteCertificado(id);
        //            if (cliente == null)
        //                return NotFound("No se encontró el cliente.");

        //            // 2️⃣ Actualizar datos generales
        //            cliente.razon_social = dto.razon_social ?? cliente.razon_social;
        //            cliente.telefono = dto.telefono ?? cliente.telefono;
        //            cliente.correo_electronico = dto.correo_electronico ?? cliente.correo_electronico;
        //            cliente.direccion = dto.direccion ?? cliente.direccion;
        //            cliente.honorarios_subtotal = dto.honorarios_subtotal != 0 ? dto.honorarios_subtotal : cliente.honorarios_subtotal;
        //            cliente.estatus = dto.estatus;
        //            cliente.usuario_id = dto.usuario_id != 0 ? dto.usuario_id : cliente.usuario_id;

        //            // 3️⃣ Obtener o crear el certificado del cliente
        //            var cert = cliente.certificados_sat.FirstOrDefault();
        //            if (cert == null)
        //            {
        //                cert = new CertificadoSAT
        //                {
        //                    cliente_id = cliente.id,
        //                    fecha_registro = DateTime.UtcNow,
        //                    estatus = true
        //                };
        //                cliente.certificados_sat.Add(cert);
        //            }

        //            // 4️⃣ Solo actualizar archivos si llegan nuevos
        //            if (dto.cer != null)
        //            {
        //                var rutaCer = await certificadosService.GuardarArchivo(dto.cer, "cer", cliente.id, dto.rfc ?? cert.rfc ?? "SIN_RFC");
        //                cert.archivo_cer = rutaCer;
        //            }

        //            if (dto.key != null)
        //            {
        //                var rutaKey = await certificadosService.GuardarArchivo(dto.key, "key", cliente.id, dto.rfc ?? cert.rfc ?? "SIN_RFC");
        //                cert.archivo_key = rutaKey;
        //            }



        //            // 5️⃣ Actualizar otros datos del certificado
        //            cert.rfc = dto.rfc ?? cert.rfc;
        //            cert.contrasena = dto.contrasena ?? cert.contrasena;
        //            cert.fecha_vigencia_inicio = dto.fecha_vigencia_inicio ?? cert.fecha_vigencia_inicio;
        //            cert.fecha_vigencia_fin = dto.fecha_vigencia_fin ?? cert.fecha_vigencia_fin;

        //            // 6️⃣ Guardar cambios
        //            _clientes.GuardarCambios(); // o usa directamente _db.SaveChanges();

        //            return Ok(new
        //            {
        //                mensaje = "Cliente actualizado correctamente",
        //                cliente.id,
        //                cliente.razon_social,
        //                cert.rfc
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(new { mensaje = "Error al actualizar cliente", detalle = ex.Message });
        //        }
        //    }


        /// <summary>
        /// Elimina un cliente por su ID.
        /// </summary>
        [RequireAccion("clientes.eliminar")]
        [HttpDelete("eliminar/{id}")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("El ID no es válido.");

                var resultado = _clientes.Eliminar(id);
                if (!resultado.IsSuccess)
                    return BadRequest(resultado.Mensaje);

                return Ok(resultado.toJson());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
