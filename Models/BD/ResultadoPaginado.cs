namespace API_asemp.Models
{
    public class ResultadoPaginado<T>
    {
        public int total { get; set; }
        public int pagina { get; set; }
        public int tamano { get; set; }
        public int paginas { get; set; }
        public List<T> datos { get; set; } = new();
    }
}
