using Microsoft.AspNetCore.Mvc;
using SistemaMatricula.Data;
using SistemaMatricula.Models;
using System.ComponentModel.DataAnnotations;

namespace SistemaMatricula.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AspirantesController : ControllerBase
    {
        private readonly MatriculaRepository _repo;

        public AspirantesController(IConfiguration config)
        {
            _repo = new MatriculaRepository(config);
        }

        // 1. Obtener aspirantes con filtros
        [HttpGet]
        public IActionResult GetAspirantes([FromQuery] string email = "", [FromQuery] string estado = "Todos")
        {
            var aspirantes = _repo.ObtenerAspirantes(email, estado);
            return Ok(aspirantes);
        }

        // 2. Ver listado de citas asignadas
        [HttpGet("citas")]
        public IActionResult GetCitas()
        {
            return Ok(_repo.ObtenerCitas());
        }

        // 3. Crear nuevo aspirante con validación de Email duplicado
        [HttpPost]
        public IActionResult CrearAspirante([FromBody] AspiranteDTO asp)
        {
            // Validación de correo ya registrado (Evita el error de SQL Unique Constraint)
            if (_repo.ExisteAspirantePorEmail(asp.Email))
            {
                return BadRequest(new { mensaje = "Este correo electrónico ya está registrado en el sistema." });
            }

            try
            {
                int id = _repo.InsertarAspirante(asp);
                return Ok(new { mensaje = "Aspirante creado exitosamente", idAspirante = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al procesar el registro.", detalle = ex.Message });
            }
        }

        // 4. Agendar Cita con validaciones de existencia
        [HttpPost("agendar")]
        public async Task<IActionResult> AgendarCita([FromBody] CitaCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validar si el aspirante ya tiene una cita
                var yaTieneCita = _repo.ExisteCita(dto.IdAspirante);

                if (yaTieneCita)
                {
                    return BadRequest(new { mensaje = "Este aspirante ya tiene una cita registrada." });
                }

                bool ok = _repo.GuardarCita(
                    dto.IdAspirante,
                    TimeSpan.Parse(dto.HoraInicio),
                    TimeSpan.Parse(dto.HoraFin)
                );

                if (ok) return Ok(new { mensaje = "Cita agendada correctamente" });

                return StatusCode(500, new { error = "No se pudo guardar la cita en la base de datos." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error de formato en las horas o error interno.", detalle = ex.Message });
            }
        }

        // 5. Actualizar la carrera elegida
        [HttpPut("{id}/carrera")]
        public IActionResult CambiarCarrera(int id, [FromBody] CambioCarreraRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.NuevaCarrera))
                return BadRequest(new { error = "La carrera es requerida." });

            bool exito = _repo.CambiarCarrera(id, req.NuevaCarrera);
            if (exito) return Ok(new { mensaje = "Carrera actualizada correctamente." });

            return StatusCode(500, new { error = "Error interno al actualizar la base de datos." });
        }

        // 6. Actualizar estado de matrícula
        [HttpPut("{id}/matricula")]
        public IActionResult ActualizarMatricula(int id, [FromBody] bool estado)
        {
            bool exito = _repo.ActualizarMatricula(id, estado);
            if (exito) return Ok(new { mensaje = "Estado de matrícula actualizado." });

            return StatusCode(500, new { error = "No se pudo actualizar el estado." });
        }

        // 7. Obtener lista de carreras
        [HttpGet("carreras")]
        public IActionResult GetCarreras()
        {
            return Ok(_repo.ObtenerCarreras());
        }

        // 8. Detalle de cita por ID de aspirante
        [HttpGet("{id}/cita-detalle")]
        public IActionResult GetCitaDetalle(int id)
        {
            var cita = _repo.ObtenerCitaPorId(id);
            if (cita == null) return NotFound(new { mensaje = "El aspirante no tiene una cita asignada." });

            return Ok(cita);
        }

        // 9. Validar choque de horarios
        [HttpPost("validar-horario")]
        public IActionResult ValidarHorario([FromBody] CitaDTO cita)
        {
            bool ocupado = _repo.ExisteChoqueHorario(
                TimeSpan.Parse(cita.HoraInicio),
                TimeSpan.Parse(cita.HoraFin)
            );

            return Ok(new { ocupado });
        }
    }

    // --- CLASES DE APOYO ---

    public class CambioCarreraRequest
    {
        public string NuevaCarrera { get; set; }
    }

    public class CitaCreateDTO
    {
        [Required(ErrorMessage = "El ID del aspirante es obligatorio")]
        public int IdAspirante { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public string HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        public string HoraFin { get; set; }
    }
}