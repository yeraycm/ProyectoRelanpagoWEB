using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaMatricula.Models;

namespace SistemaMatricula.Data
{
    public class MatriculaRepository
    {
        private readonly string _connectionString;

        public MatriculaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Dentro de tu clase MatriculaRepository
        public List<AspiranteDTO> ObtenerAspirantes(string email, string estadoMatricula)
        {
            var lista = new List<AspiranteDTO>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Consulta base que une Aspirantes con su gestión
                string query = @"
                    SELECT a.id_aspirante, 
                           a.nombre, 
                           a.apellido,
                           a.nombre + ' ' + a.apellido AS NombreCompleto, 
                           a.email, 
                           a.carrera_interes, 
                           ISNULL(m.indicar_matricula, 0) AS esta_matriculado
                    FROM aspirantes a
                    LEFT JOIN modulo_gestion_aspirante m ON a.id_aspirante = m.id_aspirante
                    WHERE 1=1 ";

                // Filtro: Buscar por email
                if (!string.IsNullOrEmpty(email)) query += " AND a.email LIKE @Email ";

                // Filtros: Ver matriculados o no matriculados
                if (estadoMatricula == "Matriculados") query += " AND m.indicar_matricula = 1 ";
                if (estadoMatricula == "NoMatriculados") query += " AND (m.indicar_matricula = 0 OR m.indicar_matricula IS NULL) ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (!string.IsNullOrEmpty(email)) cmd.Parameters.AddWithValue("@Email", "%" + email + "%");
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new AspiranteDTO
                            {
                                IdAspirante = Convert.ToInt32(reader["id_aspirante"]),
                                Nombre = reader["nombre"].ToString(),
                                Apellido = reader["apellido"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Email = reader["email"].ToString(),
                                CarreraInteres = reader["carrera_interes"].ToString(),
                                EstaMatriculado = Convert.ToBoolean(reader["esta_matriculado"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public bool ActualizarMatricula(int id, bool estado)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"IF EXISTS (SELECT 1 FROM modulo_gestion_aspirante WHERE id_aspirante = @Id)
                                UPDATE modulo_gestion_aspirante SET indicar_matricula = @Estado WHERE id_aspirante = @Id
                                ELSE
                                INSERT INTO modulo_gestion_aspirante (id_aspirante, indicar_matricula) VALUES (@Id, @Estado)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Estado", estado ? 1 : 0);
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool CambiarCarrera(int id, string nuevaCarrera)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "UPDATE aspirantes SET carrera_interes = @Carrera WHERE id_aspirante = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Carrera", nuevaCarrera);
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<CitaDTO> ObtenerCitas()
        {
            var lista = new List<CitaDTO>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.nombre + ' ' + a.apellido AS NombreCompleto, a.carrera_interes, 
                               m.asignacion_horas_inicio, m.asignacion_horas_fin
                               FROM aspirantes a
                               INNER JOIN modulo_gestion_aspirante m ON a.id_aspirante = m.id_aspirante
                               WHERE m.asignacion_horas_inicio IS NOT NULL";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new CitaDTO
                            {
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["carrera_interes"].ToString(),
                                HoraInicio = reader["asignacion_horas_inicio"].ToString(),
                                HoraFin = reader["asignacion_horas_fin"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }
        public bool AsignarFechasCita(int id, DateTime inicio, DateTime fin)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
            UPDATE modulo_gestion_aspirante 
            SET asignacion_horas_inicio = @Inicio, 
                asignacion_horas_fin = @Fin 
            WHERE id_aspirante = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Inicio", inicio);
                    cmd.Parameters.AddWithValue("@Fin", fin);
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

    

        public int InsertarAspirante(AspiranteDTO asp)
        {
                int nuevoId = 0;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"
            INSERT INTO ProyectoRelampagoJCE.aspirantes
            (nombre, apellido, email, telefono, carrera_interes)
            OUTPUT INSERTED.id_aspirante
            VALUES (@nombre, @apellido, @email, @telefono, @carrera)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@nombre", asp.Nombre);
                        cmd.Parameters.AddWithValue("@apellido", asp.Apellido);
                        cmd.Parameters.AddWithValue("@email", asp.Email);
                        cmd.Parameters.AddWithValue("@telefono", asp.Telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@carrera", asp.CarreraInteres);

                        con.Open();
                        nuevoId = (int)cmd.ExecuteScalar();
                    }
                }

                return nuevoId;
        }

        public List<string> ObtenerCarreras()
        {
        var lista = new List<string>();

        using (SqlConnection con = new SqlConnection(_connectionString))
        {
            string query = "SELECT DISTINCT carrera_interes FROM ProyectoRelampagoJCE.aspirantes";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    if (reader["carrera_interes"] != DBNull.Value)
                        lista.Add(reader["carrera_interes"].ToString());
                }
            }
        }

        return lista;
        }

        public bool GuardarCita(int id, TimeSpan inicio, TimeSpan fin)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                IF EXISTS (SELECT 1 FROM modulo_gestion_aspirante WHERE id_aspirante = @Id)
                UPDATE modulo_gestion_aspirante 
                SET asignacion_horas_inicio = @Inicio,
                    asignacion_horas_fin = @Fin
                WHERE id_aspirante = @Id
                ELSE
                INSERT INTO modulo_gestion_aspirante (id_aspirante, asignacion_horas_inicio, asignacion_horas_fin)
                VALUES (@Id, @Inicio, @Fin)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Inicio", inicio);
                    cmd.Parameters.AddWithValue("@Fin", fin);

                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool ExisteChoqueHorario(TimeSpan inicio, TimeSpan fin)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                SELECT COUNT(*) FROM modulo_gestion_aspirante
                WHERE (@Inicio < asignacion_horas_fin AND @Fin > asignacion_horas_inicio)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Inicio", inicio);
                    cmd.Parameters.AddWithValue("@Fin", fin);

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();

                    return count > 0;
                }
            }
        }
    }
}
