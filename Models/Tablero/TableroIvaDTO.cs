namespace API_asemp.Models.Tablero
{
    public class TableroIvaDTO
    {
        public int ClienteId { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Mes { get; set; }

        public IvaDetalleDTO IvaCausado { get; set; } = new();
        public IvaDetalleDTO IvaAcreditable { get; set; } = new();

        // IVA a pagar = IVA trasladado en ventas - IVA acreditable en compras
        public decimal IvaAPagar =>
            IvaCausado.TotalTrasladado - IvaAcreditable.TotalTrasladado;
    }
}
