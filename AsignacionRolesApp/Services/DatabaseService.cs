using AsignacionRolesApp.Models;
using Npgsql; // <-- CAMBIO 1: Usar Npgsql en lugar de Microsoft.Data.SqlClient

namespace AsignacionRolesApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Persona> ObtenerPersonasPresentes()
        {
            var personas = new List<Persona>();

            // <-- CAMBIO 2: NpgsqlConnection en lugar de SqlConnection
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var query = @"
                    SELECT u.user_id, u.first_name, u.last_name, 
                           jp.name_job_position, u.role_id,
                           u.profile_image_url
                    FROM assistance a
                    JOIN users u ON a.user_id = u.user_id
                    JOIN jobPosition jp ON u.job_position_id = jp.job_position_id
                    WHERE a.is_present = 1 
                    AND u.role_id != 6";

                // <-- CAMBIO 3: NpgsqlCommand en lugar de SqlCommand
                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        personas.Add(new Persona
                        {
                            UserId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            Puesto = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            RolReal = reader.GetInt32(4),
                            ProfileImageUrl = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return personas;
        }

        public List<Asignacion> ObtenerAsignacionesActivas()
        {
            var asignaciones = new List<Asignacion>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var query = @"
                    SELECT user_id, assigned_role_id
                    FROM role_assignments
                    WHERE is_active = 1";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        asignaciones.Add(new Asignacion
                        {
                            UserId = reader.GetInt32(0),
                            RolAsignado = reader.GetInt32(1)
                        });
                    }
                }
            }
            return asignaciones;
        }

        public void GuardarAsignaciones(List<Asignacion> asignaciones)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // <-- CAMBIO 4: CURRENT_TIMESTAMP en lugar de GETDATE()
                        var updateCmd = new NpgsqlCommand(
                            "UPDATE role_assignments SET is_active = 0, assignment_end = CURRENT_TIMESTAMP WHERE is_active = 1",
                            conn, transaction);
                        updateCmd.ExecuteNonQuery();

                        foreach (var asignacion in asignaciones)
                        {
                            var insertCmd = new NpgsqlCommand(
                                "INSERT INTO role_assignments (user_id, assigned_role_id, assignment_start, is_active) " +
                                "VALUES (@userId, @rolId, CURRENT_TIMESTAMP, 1)",
                                conn, transaction);

                            // Npgsql maneja los parámetros con @ perfectamente, igual que SqlClient
                            insertCmd.Parameters.AddWithValue("@userId", asignacion.UserId);
                            insertCmd.Parameters.AddWithValue("@rolId", asignacion.RolAsignado);
                            insertCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}