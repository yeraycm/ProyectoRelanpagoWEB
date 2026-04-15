public class AspiranteDTO
{
    public int IdAspirante { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string? NombreCompleto { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public string CarreraInteres { get; set; }
    public string FechaCita { get; set; }
    public string HoraInicio { get; set; }
    public string HoraFin { get; set; }
    // Esta es la que faltaba:
    public bool EstaMatriculado { get; set; }
}