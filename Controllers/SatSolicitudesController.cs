//using API_asemp.Contextos;
//using API_asemp.Datos;
//using API_asemp.Servicios;
//using API_asemp.Models.BD;
//using Microsoft.AspNetCore.Mvc;

//namespace API_asemp.Controllers
//{
//    [ApiController]
//    [Route("api/sat/solicitudes")]
//    public class SatSolicitudesController : ControllerBase
//    {
//        private readonly SolicitudesSAT _repo;
//        private readonly SatService _satService;

//        public SatSolicitudesController(myDbContext db, SatService satService)
//        {
//            _repo = new SolicitudesSAT(db);
//            _satService = satService;
//        }

//        [HttpPost]
//        public async Task<IActionResult> CrearSolicitud([FromBody] SolicitudSAT solicitud)
//        {
//            try
//            {

//                // 1. Registrar en BD
//                var resultadoBD = _repo.Registrar(solicitud);
//                if (!resultadoBD.Ok)
//                    return BadRequest(resultadoBD.Mensaje);

//                // 2. Simular comunicación con SAT
//                var resultadoSAT = await _satService.EnviarSolicitudAsync(solicitud, solicitud.certificado!);

//                // 3. Actualizar solicitud con los resultados del SAT
//                solicitud.estado_solicitud = resultadoSAT.EstadoSolicitud;
//                solicitud.codigo_estado = resultadoSAT.CodigoEstado;
//                solicitud.mensaje = resultadoSAT.Mensaje;
//                solicitud.token = resultadoSAT.Token;
//                solicitud.fecha_ultima_verificacion = DateTime.UtcNow;

//                _repo.Actualizar(solicitud);

//                return Ok(new
//                {
//                    solicitud.id,
//                    solicitud.estado_solicitud,
//                    solicitud.codigo_estado,
//                    solicitud.mensaje
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { error = ex.Message });
//            }
//        }



//        [HttpPost("{id}/verificar")]
//        public async Task<IActionResult> VerificarSolicitud(int id)
//        {
//            try
//            {
//                // 1. Buscar la solicitud en BD
//                var solicitud = _repo.Get(id);
//                if (solicitud == null)
//                    return NotFound("No se encontró la solicitud en la base de datos.");

//                // 2. Obtener el certificado asociado
//                var certificado = solicitud.certificado;
//                if (certificado == null)
//                    return BadRequest("No se encontró el certificado asociado a la solicitud.");

//                // 3. Llamar al servicio SAT para verificar estado
//                var resultadoSAT = await _satService.VerificarSolicitudAsync(solicitud, certificado);

//                // 4. Guardar registro en la tabla verificaciones_sat
//                var verificacion = new VerificacionesSAT
//                {
//                    SolicitudId = solicitud.id,
//                    CodigoEstado = resultadoSAT.CodigoEstado,
//                    EstadoSolicitud = (short)resultadoSAT.EstadoSolicitud,
//                    NumeroCfdis = resultadoSAT.NumeroCfdis,
//                    Mensaje = resultadoSAT.Mensaje,
//                    FechaVerificacion = DateTime.UtcNow
//                };

//                // puedes crear un repo VerificacionesSAT o guardar directo:
//                _repo.DbContext.verificaciones_sat.Add(verificacion);

//                await _repo.DbContext.SaveChangesAsync();

//                // 5. Actualizar solicitud con estado más reciente
//                solicitud.estado_solicitud = resultadoSAT.EstadoSolicitud;
//                solicitud.mensaje = resultadoSAT.Mensaje;
//                solicitud.fecha_ultima_verificacion = DateTime.UtcNow;
//                _repo.Actualizar(solicitud);

//                // 6. Responder al front
//                return Ok(new
//                {
//                    solicitud.id,
//                    solicitud.estado_solicitud,
//                    solicitud.codigo_estado,
//                    solicitud.mensaje,
//                    resultadoSAT.NumeroCfdis
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { error = ex.Message });
//            }
//        }



//    }
//}