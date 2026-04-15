namespace SistemaMatricula.Models
{
    public class AspiranteDTO
    {
        public int IdAspirante { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string CarreraInteres { get; set; }
        public bool EstaMatriculado { get; set; }
    }

    // Clase auxiliar para recibir datos de cambio de carrera
    public class CambioCarreraRequest
    {
        public string NuevaCarrera { get; set; }
    }
}