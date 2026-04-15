namespace SistemaMatricula.Models
{
    public class AspiranteDTO
    {
        public int IdAspirante { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; } //mias

        public string NombreCompleto { get; set; } // tuya

        public string Email { get; set; }
        public string Telefono { get; set; }

        public string CarreraInteres { get; set; }

        public bool EstaMatriculado { get; set; }
    }

    // Clase auxiliar para recibir datos de cambio de carrera
    public class CambioCarreraRequest
    {
        public string NuevaCarrera { get; set; }
    }
}
