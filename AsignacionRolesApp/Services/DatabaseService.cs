using Microsoft.Data.SqlClient;
using AsignacionRolesApp.Models;
using System.Collections.Generic;
using System;

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

            using (var conn = new SqlConnection(_connectionString))
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

                using (var cmd = new SqlCommand(query, conn))
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

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Solo traemos el ID de usuario y el rol que tiene asignado actualmente
                var query = @"
                    SELECT user_id, assigned_role_id
                    FROM role_assignments
                    WHERE is_active = 1";

                using (var cmd = new SqlCommand(query, conn))
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
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Iniciar transacción
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Desactivar asignaciones anteriores
                        var updateCmd = new SqlCommand(
                            "UPDATE role_assignments SET is_active = 0, assignment_end = GETDATE() WHERE is_active = 1",
                            conn, transaction);
                        updateCmd.ExecuteNonQuery();

                        // Insertar nuevas asignaciones
                        foreach (var asignacion in asignaciones)
                        {
                            var insertCmd = new SqlCommand(
                                "INSERT INTO role_assignments (user_id, assigned_role_id, assignment_start, is_active) " +
                                "VALUES (@userId, @rolId, GETDATE(), 1)",
                                conn, transaction);

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