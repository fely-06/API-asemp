using API_asemp.Contextos;
using API_asemp.Models;
using API_asemp.Models.BD;
using Herramientas.Validaciones;
using Microsoft.EntityFrameworkCore;

namespace API_asemp.Datos
{
    public class Clientes
    {
        private readonly myDbContext _db;

        public Clientes(myDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // ========== MÉTODOS DE CONSULTA ======================
        // =====================================================

        public Cliente Get(int id)
        {
            var registro = _db.clientes.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este cliente");
            return registro!;
        }

        public List<Cliente> GetLista()
        {
            return _db.clientes
                .OrderByDescending(x => x.id)
                .ToList();
        }

        public Cliente GetClienteCertificado(int id)
        {
            var registro = _db.clientes
                .Include(c => c.certificados_sat) // 👈 incluir los certificados
                .FirstOrDefault(x => x.id == id);

            Ensure.ValidarNulo(registro, "No se encontró este cliente");
            return registro!;
        }


        public List<object> GetListaConCertificados()
        {
            var lista = _db.clientes
                .Select(c => new
                {
                    c.id,
                    c.razon_social,
                    c.telefono,
                    c.correo_electronico,
                    c.direccion,
                    c.honorarios_subtotal,
                    c.estatus,

                    // 🔥 CAMPOS QUE TE FALTAN PARA COBROS
                    c.usa_retenciones,
                    c.porc_isr_ret,
                    c.porc_iva_ret,

                    certificado = c.certificados_sat
                        .OrderByDescending(x => x.fecha_registro)
                        .Select(x => new
                        {
                            x.rfc,
                            x.fecha_vigencia_inicio,
                            x.fecha_vigencia_fin,
                            x.archivo_cer,
                            x.archivo_key,
                            x.archivo_pfx
                        })
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.id)
                .ToList<object>();

            return lista;
        }

        // =====================================================
        // 🔹 Método auxiliar para confirmar cambios en la BD
        // =====================================================
        public void GuardarCambios()
        {
            _db.SaveChanges();
        }



        // =====================================================
        // ========== MÉTODOS CRUD =============================
        // =====================================================

        public Cliente Registrar(Cliente cliente)
        {
            Ensure.ValidarVacio(cliente.razon_social!, "La razón social no puede estar vacía");
            Ensure.ValidarVacio(cliente.correo_electronico!, "El correo no puede estar vacío");

            // Validar duplicado solo por correo
            if (_db.clientes.Any(c => c.correo_electronico!.ToLower() == cliente.correo_electronico!.ToLower()))
                throw new Exception("Ya existe un cliente con este correo electrónico.");

            cliente.fecha_registro = DateTime.UtcNow;
            cliente.estatus = true;

            // Guardar retenciones
            cliente.usa_retenciones = cliente.usa_retenciones;
            cliente.porc_isr_ret = cliente.porc_isr_ret;
            cliente.porc_iva_ret = cliente.porc_iva_ret;

            _db.clientes.Add(cliente);
            _db.SaveChanges();   // ← Aquí EF asigna el ID

            return cliente;      // ← SE REGRESA CON SU ID real
        }


        //public Estatus<bool> Registrar(Cliente cliente)
        //{
        //    Ensure.ValidarVacio(cliente.razon_social!, "La razón social no puede estar vacía");
        //    Ensure.ValidarVacio(cliente.correo_electronico!, "El correo no puede estar vacío");

        //    // Validar duplicado solo por correo, ya que RFC ya no existe aquí
        //    if (_db.clientes.Any(c => c.correo_electronico!.ToLower() == cliente.correo_electronico!.ToLower()))
        //        return Estatus<bool>.Error("Ya existe un cliente con este correo electrónico.");

        //    cliente.fecha_registro = DateTime.UtcNow;
        //    cliente.estatus = true;

        //    _db.clientes.Add(cliente);
        //    _db.SaveChanges();

        //    return Estatus<bool>.OK("Cliente registrado correctamente");
        //}


        public Estatus<bool> Actualizar(Cliente cliente)
        {
            Ensure.ValidarNulo(cliente, "El objeto cliente no puede ser nulo");

            var registro = Get(cliente.id);

            // Validar duplicado de correo
            if (_db.clientes.Any(c => c.id != cliente.id && c.correo_electronico!.ToLower() == cliente.correo_electronico!.ToLower()))
                return Estatus<bool>.Error("Ya existe otro cliente con este correo electrónico.");

            registro.razon_social = cliente.razon_social;
            registro.telefono = cliente.telefono;
            registro.correo_electronico = cliente.correo_electronico;
            registro.direccion = cliente.direccion;
            registro.honorarios_subtotal = cliente.honorarios_subtotal;
            registro.estatus = cliente.estatus;

            // ================================
            // 🔹 NUEVOS CAMPOS A ACTUALIZAR
            // ================================
            registro.usa_retenciones = cliente.usa_retenciones;
            registro.porc_isr_ret = cliente.porc_isr_ret;
            registro.porc_iva_ret = cliente.porc_iva_ret;

            _db.SaveChanges();
            return Estatus<bool>.OK("Cliente actualizado correctamente");
        }

        //public Estatus<bool> Actualizar(Cliente cliente)
        //{
        //    Ensure.ValidarNulo(cliente, "El objeto cliente no puede ser nulo");

        //    var registro = Get(cliente.id);

        //    // Validar duplicado de correo
        //    if (_db.clientes.Any(c => c.id != cliente.id && c.correo_electronico!.ToLower() == cliente.correo_electronico!.ToLower()))
        //        return Estatus<bool>.Error("Ya existe otro cliente con este correo electrónico.");

        //    registro.razon_social = cliente.razon_social;
        //    registro.telefono = cliente.telefono;
        //    registro.correo_electronico = cliente.correo_electronico;
        //    registro.direccion = cliente.direccion;
        //    registro.honorarios_subtotal = cliente.honorarios_subtotal;
        //    registro.estatus = cliente.estatus;

        //    _db.SaveChanges();
        //    return Estatus<bool>.OK("Cliente actualizado correctamente");
        //}

        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _db.clientes.Remove(registro);
            _db.SaveChanges();
            return Estatus<bool>.OK("Cliente eliminado correctamente");
        }
    }
}
