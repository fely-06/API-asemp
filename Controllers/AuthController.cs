using API_asemp.Contextos;
using API_asemp.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API_asemp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly myDbContext _db;
        private readonly string _jwtKey;
        private readonly int _jwtHours;

        public AuthController(myDbContext db, IConfiguration cfg)
        {
            _db = db;
            _jwtKey = cfg["Jwt:Key"]!;
            _jwtHours = int.Parse(cfg["Jwt:ExpiresHours"] ?? "8");
        }

        // ============================================================
        // ======================= LOGIN ==============================
        // ============================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.usuario) || string.IsNullOrWhiteSpace(dto.contrasena))
                return BadRequest("Usuario y contraseña requeridos.");

            var user = await _db.usuarios
                .Include(u => u.rol)
                .FirstOrDefaultAsync(u =>
                    u.usuario == dto.usuario || u.correo == dto.usuario);

            if (user is null || !PasswordHelper.Verify(dto.contrasena, user.contrasena ?? ""))
                return Unauthorized("Credenciales inválidas.");

            var cliente = await _db.clientes
                .FirstOrDefaultAsync(c => c.usuario_id == user.id);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim("id", user.id.ToString()),
                new Claim(ClaimTypes.Name, user.usuario ?? ""),
                new Claim(ClaimTypes.Role, user.rol?.nombre ?? "")
            };

            if (user.rol?.nombre == "Cliente" && cliente != null)
            {
                claims.Add(new Claim("cliente_id", cliente.id.ToString()));
            }

            var permisos = await _db.roles_acciones
                .Where(r => r.rol_id == user.rol_id)
                .Select(r => r.accion!.nombre!)
                .ToListAsync();

            foreach (var clave in permisos)
                claims.Add(new Claim("permiso", clave));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtHours),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = user.usuario,
                rol = user.rol?.nombre,
                permisos
            });
        }

        // ============================================================
        // ======================= PERFIL =============================
        // ============================================================
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim))
                return Unauthorized();

            int userId = int.Parse(idClaim);

            var user = await _db.usuarios
                .Include(u => u.rol)
                .FirstOrDefaultAsync(u => u.id == userId);

            if (user is null)
                return Unauthorized();

            var permisos = await _db.roles_acciones
                .Where(r => r.rol_id == user.rol_id)
                .Select(r => r.accion!.nombre!)
                .ToListAsync();

            // 🔹 NUEVO: cliente asociado (solo si existe)
            var cliente = await _db.clientes
                .Where(c => c.usuario_id == user.id)
                .Select(c => new
                {
                    c.id,
                    c.razon_social,
                    rfc = c.certificados_sat
                        .Select(cs => cs.rfc)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                id = user.id,
                nombres = user.nombres,
                apellido_paterno = user.apellido_paterno,
                apellido_materno = user.apellido_materno,
                usuario = user.usuario,
                rol = user.rol?.nombre,
                permisos,

                // 👇 ESTE ES EL CAMBIO CLAVE
                cliente
            });
        }
    }

    public record LoginDTO(string usuario, string contrasena);
}

