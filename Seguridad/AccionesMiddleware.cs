public class AccionesMiddleware
{
    private readonly RequestDelegate _next;

    public AccionesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint != null)
        {
            var attr = endpoint.Metadata.GetMetadata<RequireAccionAttribute>();

            if (attr != null)
            {
                var permisos = context.User.FindAll("permiso")
                                           .Select(p => p.Value)
                                           .ToList();

                if (!permisos.Contains(attr.Clave))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("No autorizado.");
                    return;
                }
            }
        }

        await _next(context);
        }
}
