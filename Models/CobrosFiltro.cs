namespace API_asemp.Models;

public class CobrosFiltro
{
    public string? texto { get; set; }
    public int? mes { get; set; }
    public int? anio { get; set; }
    public int? cliente_id { get; set; }

    public bool? pendientes { get; set; }
    public bool? pagados { get; set; }
    public bool? atrasados { get; set; }
}
