namespace API_asemp.Models.DTO
{
    // Cuerpo opcional para /certificadossat/enviar-correo/{id}
    // Si "correo" viene vacío, se usa el correo registrado del cliente.
    public class EnviarCertificadoCorreoDTO
    {
        public string? correo { get; set; }
    }
}
