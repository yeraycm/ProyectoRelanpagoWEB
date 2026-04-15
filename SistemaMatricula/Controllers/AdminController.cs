using Microsoft.AspNetCore.Mvc;

using SistemaMatricula.Data; // Ajusta al nombre de tu namespace
using SistemaMatricula.Models;

namespace SistemaMatricula.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly MatriculaRepository _repo;

        public AdminController(IConfiguration config)
        {
            _repo = new MatriculaRepository(config);
        }
// Tarea: Mostrar lista de citas asignadas [cite: 9]
        [HttpGet("citas")]
        public IActionResult VerCitas()
        {
            var citas = _repo.ObtenerCitas();
            return Ok(citas);
        }

// Tarea: Función para marcar como matriculado o no [cite: 5, 6]
        [HttpPut("actualizar-estado/{id}")]
        public IActionResult ActualizarEstado(int id, [FromBody] bool matriculado)
        {
            var exito = _repo.ActualizarMatricula(id, matriculado);
            if (exito) return Ok(new { mensaje = "Estado actualizado correctamente" });
            return BadRequest("No se pudo actualizar el estado.");
        }
    }
}