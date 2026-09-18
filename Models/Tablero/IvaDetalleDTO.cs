namespace API_asemp.Models.Tablero
{
    public class IvaDetalleDTO
    {
        public decimal PUE { get; set; }
        public decimal PPD { get; set; }
        public decimal Retenido { get; set; }

        // Total que usa el frontend
        public decimal Total => PUE + PPD;

        // Si quieres mantener el otro nombre también:
        public decimal TotalTrasladado => PUE + PPD;
    }

}
