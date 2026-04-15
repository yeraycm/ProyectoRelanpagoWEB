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

        // --- MÉTODO 1: OBTENER LISTADO ---
        public List<AspiranteDTO> ObtenerAspirantes(string email, string estadoMatricula)
        {
            var lista = new List<AspiranteDTO>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT a.id_aspirante, a.nombre, a.apellido,
                           a.nombre + ' ' + a.apellido AS NombreCompleto, 
                           a.email, a.carrera_interes, 
                           ISNULL(m.indicar_matricula, 0) AS esta_matriculado
                    FROM aspirantes a
                    LEFT JOIN modulo_gestion_aspirante m ON a.id_aspirante = m.id_aspirante
                    WHERE 1=1 ";

                if (!string.IsNullOrEmpty(email)) query += " AND a.email LIKE @Email ";
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

        // --- MÉTODO 2: INSERTAR (EL QUE DABA ERROR) ---
        public int InsertarAspirante(AspiranteDTO asp)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        string sqlAspirante = @"INSERT INTO aspirantes (nombre, apellido, email, telefono, carrera_interes) 
                                                VALUES (@nom, @ape, @email, @tel, @carrera);
                                                SELECT SCOPE_IDENTITY();";
                        SqlCommand cmd1 = new SqlCommand(sqlAspirante, conn, trans);
                        cmd1.Parameters.AddWithValue("@nom", asp.Nombre);
                        cmd1.Parameters.AddWithValue("@ape", asp.Apellido);
                        cmd1.Parameters.AddWithValue("@email", asp.Email);
                        cmd1.Parameters.AddWithValue("@tel", asp.Telefono ?? (object)DBNull.Value);
                        cmd1.Parameters.AddWithValue("@carrera", asp.CarreraInteres);

                        int idGenerado = Convert.ToInt32(cmd1.ExecuteScalar());

                        // Parseo de fechas corregido
                        DateTime inicio = DateTime.Parse($"{asp.FechaCita} {asp.HoraInicio}");
                        DateTime fin = DateTime.Parse($"{asp.FechaCita} {asp.HoraFin}");

                        string sqlModulo = @"INSERT INTO modulo_gestion_aspirante 
                                            (id_aspirante, asignacion_horas_inicio, asignacion_horas_fin, indicar_matricula) 
                                            VALUES (@id, @inicio, @fin, 0);";
                        SqlCommand cmd2 = new SqlCommand(sqlModulo, conn, trans);
                        cmd2.Parameters.AddWithValue("@id", idGenerado);
                        cmd2.Parameters.AddWithValue("@inicio", inicio);
                        cmd2.Parameters.AddWithValue("@fin", fin);

                        cmd2.ExecuteNonQuery();
                        trans.Commit();
                        return idGenerado;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        throw new Exception("Error en SQL: " + ex.Message);
                    }
                }
            }
        }

        // --- MÉTODO 3: GESTIÓN DE MATRÍCULA ---
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

        // --- MÉTODOS QUE FALTABAN (LOS QUE RECUPERAMOS) ---
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

        public bool GuardarCita(int id, TimeSpan inicio, TimeSpan fin)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"IF EXISTS (SELECT 1 FROM modulo_gestion_aspirante WHERE id_aspirante = @Id)
                                UPDATE modulo_gestion_aspirante SET asignacion_horas_inicio = @Inicio, asignacion_horas_fin = @Fin WHERE id_aspirante = @Id
                                ELSE
                                INSERT INTO modulo_gestion_aspirante (id_aspirante, asignacion_horas_inicio, asignacion_horas_fin) VALUES (@Id, @Inicio, @Fin)";
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
                string query = "SELECT COUNT(*) FROM modulo_gestion_aspirante WHERE (@Inicio < asignacion_horas_fin AND @Fin > asignacion_horas_inicio)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Inicio", inicio);
                    cmd.Parameters.AddWithValue("@Fin", fin);
                    con.Open();
                    return (int)cmd.ExecuteScalar() > 0;
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

        public List<string> ObtenerCarreras()
        {
            var lista = new List<string>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT DISTINCT carrera_interes FROM aspirantes";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["carrera_interes"] != DBNull.Value)
                                lista.Add(reader["carrera_interes"].ToString());
                        }
                    }
                }
            }
            return lista;
        }
        public CitaDTO ObtenerCitaPorId(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.nombre + ' ' + a.apellido AS NombreCompleto, 
                               a.carrera_interes, 
                               m.asignacion_horas_inicio, 
                               m.asignacion_horas_fin
                        FROM aspirantes a
                        INNER JOIN modulo_gestion_aspirante m ON a.id_aspirante = m.id_aspirante
                        WHERE a.id_aspirante = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CitaDTO
                            {
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["carrera_interes"].ToString(),
                                // Convertimos a string para el DTO
                                HoraInicio = reader["asignacion_horas_inicio"].ToString(),
                                HoraFin = reader["asignacion_horas_fin"].ToString()
                            };
                        }
                    }
                }
            }
            return null; // Si no tiene cita asignada
        }
        public bool ExisteCita(int idAspirante)
        {
            // Usamos 'using' para asegurar que la conexión se cierre sola
            using (var con = new SqlConnection(_connectionString))
            {
                // Consulta SQL para contar si ya existe ese ID en la tabla de citas
                string sql = "SELECT COUNT(1) FROM Citas WHERE IdAspirante = @id";

                // Ejecutamos la consulta
                // Si usas Dapper:
                // int conteo = con.ExecuteScalar<int>(sql, new { id = idAspirante });

                // Si usas ADO.NET puro:
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", idAspirante);
                    con.Open();
                    int conteo = (int)cmd.ExecuteScalar();
                    return conteo > 0;
                }
            }
        }
        public bool ExisteAspirantePorEmail(string email)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                // Buscamos si el email ya existe en la tabla aspirantes
                string sql = "SELECT COUNT(1) FROM aspirantes WHERE email = @email";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    con.Open();
                    int existe = (int)cmd.ExecuteScalar();
                    return existe > 0;
                }
            }
        }

        public List<object> ObtenerMatriculadosPorDirector(int idDirector)
        {
            var lista = new List<object>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
        SELECT 
            a.id_aspirante,
            a.nombre,
            a.apellido,
            a.email,
            a.carrera_interes
        FROM aspirantes a
        INNER JOIN modulo_gestion_aspirante m
            ON a.id_aspirante = m.id_aspirante
        INNER JOIN directores_carrera d
            ON d.id_director = @idDirector
        WHERE a.carrera_interes = d.carrera
          AND ISNULL(m.indicar_matricula, 0) = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@idDirector", idDirector);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new
                {
                    id_aspirante = Convert.ToInt32(reader["id_aspirante"]),
                    nombre = reader["nombre"]?.ToString(),
                    apellido = reader["apellido"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    carrera_interes = reader["carrera_interes"]?.ToString()
                });
            }

            return lista;
        }

        public void GuardarCorreoMasivo(int idDirector, string asunto, string mensaje)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var queryIds = @"
        SELECT a.id_aspirante
        FROM aspirantes a
        INNER JOIN modulo_gestion_aspirante m
            ON a.id_aspirante = m.id_aspirante
        INNER JOIN directores_carrera d
            ON d.id_director = @idDirector
        WHERE a.carrera_interes = d.carrera
          AND ISNULL(m.indicar_matricula, 0) = 1";

            var ids = new List<int>();

            using (var command = new SqlCommand(queryIds, connection))
            {
                command.Parameters.AddWithValue("@idDirector", idDirector);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ids.Add(Convert.ToInt32(reader["id_aspirante"]));
                }
            }

            foreach (var idAspirante in ids)
            {
                var insert = @"
            INSERT INTO correos_electronicos
            (id_aspirante, id_director, asunto, contenido)
            VALUES (@idAspirante, @idDirector, @asunto, @contenido)";

                using var insertCommand = new SqlCommand(insert, connection);
                insertCommand.Parameters.AddWithValue("@idAspirante", idAspirante);
                insertCommand.Parameters.AddWithValue("@idDirector", idDirector);
                insertCommand.Parameters.AddWithValue("@asunto", asunto);
                insertCommand.Parameters.AddWithValue("@contenido", mensaje);

                insertCommand.ExecuteNonQuery();
            }
        }
    }

}