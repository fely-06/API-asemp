using System.Net;
using System.Net.Mail;

namespace API_asemp.Servicios
{
    // ======================================================
    // 🔹 Contrato para el envío de correos.
    //     Permite cambiar de proveedor (SMTP, SendGrid, etc.)
    //     sin tocar el código que lo consume.
    // ======================================================
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
    }

    // ======================================================
    // 🔹 Implementación vía SMTP usando la configuración
    //     de la sección "Smtp" en appsettings.json
    // ======================================================
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                Console.WriteLine("⚠️ No se envió correo: destinatario vacío.");
                return;
            }

            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var user = _config["Smtp:User"];
            var password = _config["Smtp:Password"];
            var enableSslStr = _config["Smtp:EnableSsl"];
            var fromEmail = _config["Smtp:From"];
            var fromName = _config["Smtp:FromName"] ?? "ASEMP";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            {
                Console.WriteLine("⚠️ SMTP no está configurado (Smtp:Host / Smtp:User). Se omite el envío de correo.");
                return;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool enableSsl = !bool.TryParse(enableSslStr, out var ssl) || ssl; // true por defecto

            using var mensaje = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(fromEmail) ? user! : fromEmail, fromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            using var cliente = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = enableSsl
            };

            try
            {
                await cliente.SendMailAsync(mensaje);
                Console.WriteLine($"✅ Correo enviado correctamente a {destinatario}");
            }
            catch (Exception ex)
            {
                // No se relanza la excepción: un fallo de correo no debe
                // tumbar el flujo de negocio (ej. creación de usuario).
                Console.WriteLine($"❌ Error enviando correo a {destinatario}: {ex.Message}");
            }
        }
    }
}
