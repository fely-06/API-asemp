using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Herramientas.Validaciones
{
    public class Ensure
    {
        /// <summary>
        /// Valida si el objeto es un valor nulo si es asi salta una excepcion
        /// </summary>
        public static void ValidarNulo(object objeto, string mensaje = "El valor es nulo")
        {
            if (objeto is null)
            {
                throw new Exception(mensaje);
            }
        }

        public static void ValidarEnteroPositivo(int numero, string mensaje)
        {
            if (numero <= 0)
            {
                throw new Exception(mensaje);
            }
        }
        public static void ValidarEnteroPositivo(long numero, string mensaje)
        {
            if (numero <= 0)
            {
                throw new Exception(mensaje);
            }
        }
        public static void ValidarLongitud(string cadena, int limite, string mensaje)
        {
            if (cadena.Length > limite)
            {
                throw new Exception(mensaje);
            }
        }
        public static void ValidarVacio(string valor, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new Exception(mensaje);
            }           
        }
        public static void ValidarBooleano(bool valor, string mensaje)
        {
            if (!valor)
            {
                throw new Exception(mensaje);
            }
        }

        public static void ValidarLista<T>(List<T> lista, string mensaje)
        {
            if (lista == null || lista.Count == 0)
            {
                throw new Exception(mensaje);
            }
        }

        public static void ValidarFecha(DateTime? fecha, string mensaje)
        {
            if (fecha == null || fecha == default(DateTime))
            {
                throw new Exception(mensaje);
            }
        }
        public static void ValidarCorreos(List<string> correos)
        {
            ValidarLista(correos, "Debes agregar almenos un correo");
            foreach (var item in correos)
            {
                ValidarCorreo(item);
            }
        }
        public static void ValidarCorreo(string correo)
        {
            ValidarVacio(correo,"El correo no puede estar vacio");
            if (!Regex.IsMatch(correo, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"))
            {
                throw new Exception($"El correo {correo} no es valido");
            }
        }
        public static void ValidarGuid(Guid guid, string mensajeError)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentException(mensajeError);
            }
        }
        public static void ValidarContrasena(string contrasena)
        {
            ValidarVacio(contrasena, "La contraseña no puede estar vacia");
            if (contrasena.Length < 8)
            {
                throw new Exception("La contraseña no puede tener menos de 8 caracteres");
            }
        }
    }
}
