using Newtonsoft.Json;

namespace API_asemp.Models
{
    public class Estatus<T>
    {
        public bool IsSuccess;
        public T Result;
        public string? Mensaje;

        public Estatus(bool _issuccess, T _resul, string _mensaje)
        {
            IsSuccess = _issuccess;
            Result = _resul;
            Mensaje = _mensaje;
        }

        public static Estatus<T> OK(T _resul, string _mensaje)
        {
            return new Estatus<T>(true, _resul, _mensaje);
        }

        public static Estatus<T> OK(string _mensaje)
        {
            return new Estatus<T>(true, default(T), _mensaje);
        }

        public string toJson()
        {
            return JsonConvert.SerializeObject(new { success = IsSuccess, mensaje = Mensaje, result = Result });
        }

        public static Estatus<T> Error(string _mensaje)
        {
            return new Estatus<T>(false, default(T), _mensaje);
        }

        // ===== Alias de compatibilidad =====
        public bool Ok => IsSuccess; // ahora puedes usar .Ok o .IsSuccess
    }
}
