using Microsoft.AspNetCore.Mvc;
using SistemaMatricula.Data;
using SistemaMatricula.Models;

namespace SistemaMatricula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DirectorController : ControllerBase
    {
        private readonly MatriculaRepository _repository;

        public DirectorController(MatriculaRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("matriculados/{idDirector}")]
        public IActionResult ObtenerMatriculados(int idDirector)
        {
            var lista = _repository.ObtenerMatriculadosPorDirector(idDirector);
            return Ok(lista);
        }

        [HttpPost("enviar-correo")]
        public IActionResult EnviarCorreoMasivo([FromBody] CorreoMasivoDTO dto)
        {
            _repository.GuardarCorreoMasivo(dto.IdDirector, dto.Asunto, dto.Mensaje);
            return Ok(new { mensaje = "Correos enviados correctamente." });
        }
    }
}