namespace API_asemp.Models.DTO
{
    public class CertificadoArchivosDTO
    {
        public IFormFile? cer { get; set; }
        public IFormFile? key { get; set; }
        public string? contrasena { get; set; }
    }
}
