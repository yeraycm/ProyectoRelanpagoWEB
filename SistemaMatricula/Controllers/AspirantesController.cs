using Microsoft.AspNetCore.Mvc;
using SistemaMatricula.Data;
using SistemaMatricula.Models;

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

        // Obtener aspirantes con filtros de búsqueda por email y estado de matrícula
        [HttpGet]
        public IActionResult GetAspirantes([FromQuery] string email = "", [FromQuery] string estado = "Todos")
        {
            var aspirantes = _repo.ObtenerAspirantes(email, estado);
            return Ok(aspirantes);
        }

        // Ver listado de citas asignadas
        [HttpGet("citas")]
        public IActionResult GetCitas()
        {
            return Ok(_repo.ObtenerCitas());
        }

        // Actualizar la carrera elegida por el aspirante
        [HttpPut("{id}/carrera")]
        public IActionResult CambiarCarrera(int id, [FromBody] CambioCarreraRequest req)
        {
            // Verificamos que el objeto no venga nulo
            if (req == null || string.IsNullOrEmpty(req.NuevaCarrera))
                return BadRequest(new { error = "La carrera es requerida." });

            bool exito = _repo.CambiarCarrera(id, req.NuevaCarrera);
            if (exito) return Ok(new { mensaje = "Carrera actualizada correctamente." });

            return StatusCode(500, new { error = "Error interno al actualizar la base de datos." });
        }

        // Endpoint para marcar si el estudiante matriculó o no
        [HttpPut("{id}/matricula")]
        public IActionResult ActualizarMatricula(int id, [FromBody] bool estado)
        {
            bool exito = _repo.ActualizarMatricula(id, estado);
            if (exito) return Ok(new { mensaje = "Estado de matrícula actualizado." });

            return StatusCode(500, new { error = "No se pudo actualizar el estado." });
        }
    }

    // Clase de apoyo para recibir los datos del cambio de carrera
    public class CambioCarreraRequest
    {
        public string NuevaCarrera { get; set; }
    }
}