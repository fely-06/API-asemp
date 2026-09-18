using API_asemp.Contextos;
using API_asemp.Models.BD;
using API_asemp.Models;
using Herramientas.Validaciones;

namespace API_asemp.Datos
{
    public class Departamentos
    {
        private readonly myDbContext _bd;
        public Departamentos(myDbContext bd)
        {
            _bd = bd;
        }
        public Departamento Get(int id)
        {
            var registro = _bd.departamentos.FirstOrDefault(x => x.id == id);
            Ensure.ValidarNulo(registro, "No se encontró este departamento");
            return registro;
        }

        public List<Departamento> GetLista()
        {
            return _bd.departamentos.Where(x => x.id > 0).OrderByDescending(x => x.id).ToList();
        }

        public Estatus<bool> Registrar(Departamento departamento)
        {
            Ensure.ValidarVacio(departamento.nombre, "El nombre del departamento no puede estar vacío");
            if (_bd.departamentos.Any(d => d.nombre.ToLower() == departamento.nombre.ToLower()))
            {
                return Estatus<bool>.Error("Ya existe un departamento con este nombre.");
            }
            _bd.departamentos.Add(departamento);
            _bd.SaveChanges();
            return Estatus<bool>.OK("Departamento guardado");
        }

        public Estatus<bool> Actualizar(Departamento departamento)
        {
            Ensure.ValidarVacio(departamento.nombre, "El nombre del departamento no puede estar vacío");
            if (_bd.departamentos.Any(d => d.id != departamento.id && d.nombre.ToLower() == departamento.nombre.ToLower()))
            {
                return Estatus<bool>.Error("Ya existe otro departamento con este nombre.");
            }
            var registro = Get(departamento.id);
            registro.nombre = departamento.nombre;
            _bd.SaveChanges();
            return Estatus<bool>.OK("Departamento actualizado");
        }

        public Estatus<bool> Eliminar(int id)
        {
            var registro = Get(id);
            _bd.departamentos.Remove(registro);
            _bd.SaveChanges();
            return Estatus<bool>.OK("Departamento eliminado");
        }
    }
}
