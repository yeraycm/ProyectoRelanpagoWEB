using System.ComponentModel.DataAnnotations;

namespace SistemaMatricula.Models
{
    public class CitaDTO
    {
        public string NombreCompleto { get; set; }
        public string Carrera { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }
    }
    public class CitaCreateDTO
    {
        [Required(ErrorMessage = "El ID del aspirante es obligatorio")]
        public int IdAspirante { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La carrera es obligatoria")]
        public string Carrera { get; set; }

        // Estas deben ser strings si desde el JS mandas "08:00"
        [Required]
        public string HoraInicio { get; set; }

        [Required]
        public string HoraFin { get; set; }

        // Si mandas la fecha por separado
        public string FechaCita { get; set; }
    }
}