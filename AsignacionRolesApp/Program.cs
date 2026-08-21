using AsignacionRolesApp.Models;
using AsignacionRolesApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace AsignacionRolesApp
{
    class Program
    {
        private static DatabaseService _dbService;
        private static AssignmentService _assignmentService;
        private static System.Timers.Timer _timer;
        private static int _updateInterval;

        // Pro-Tip: Variables para evitar ejecuciones superpuestas del timer
        private static bool _isRunning = false;
        private static readonly object _lock = new object();

        static void Main()
        {
            try
            {
                var config = AppConfig.Load();
                _updateInterval = config.UpdateIntervalSeconds * 1000;

                _dbService = new DatabaseService(config.ConnectionString);
                _assignmentService = new AssignmentService(config.RolesRequeridos);

                Console.WriteLine("============================================");
                Console.WriteLine("  Sistema de Asignación de Roles - Denso  ");
                Console.WriteLine("============================================");
                Console.WriteLine($"- Intervalo: {config.UpdateIntervalSeconds} segundos");
                Console.WriteLine($"- Roles requeridos: {string.Join(", ", config.RolesRequeridos.Select(kv => $"{GetNombreRol(kv.Key)}:{kv.Value}"))}");
                Console.WriteLine("--------------------------------------------\n");

                // Ejecutar inmediatamente al inicio
                EjecutarAsignacionSegura();

                // Configurar timer
                _timer = new System.Timers.Timer(_updateInterval);
                _timer.Elapsed += (s, e) => EjecutarAsignacionSegura();
                _timer.AutoReset = true;
                _timer.Start();

                Console.WriteLine("Sistema en ejecución. Presiona:");
                Console.WriteLine("- [R] Para ejecutar asignación manualmente");
                Console.WriteLine("- [ESC] Para salir");
                Console.WriteLine("--------------------------------------------");

                while (true)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape) break;
                    if (key.Key == ConsoleKey.R) EjecutarAsignacionSegura();
                }

                _timer.Stop();
                Console.WriteLine("\nAplicación finalizada correctamente.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR CRÍTICO: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Presiona cualquier tecla para salir...");
                Console.ReadKey();
            }
        }

        // Método envuelto con bloqueo para concurrencia
        private static void EjecutarAsignacionSegura()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss}: [AVISO] Ejecución anterior aún en proceso. Omitiendo...");
                    return;
                }
                _isRunning = true;
            }

            try
            {
                EjecutarAsignacion();
            }
            finally
            {
                lock (_lock) { _isRunning = false; }
            }
        }

        private static void EjecutarAsignacion()
        {
            var startTime = DateTime.Now;
            Console.WriteLine($"{startTime:HH:mm:ss}: Iniciando asignación...");

            // 1. Obtener personas presentes
            var presentes = _dbService.ObtenerPersonasPresentes();
            Console.WriteLine($"- Personas presentes y capacitadas: {presentes.Count}");

            // 1.5 NUEVO: Obtener asignaciones actuales para mantener continuidad
            var asignacionesActuales = _dbService.ObtenerAsignacionesActivas();
            Console.WriteLine($"- Asignaciones previas activas: {asignacionesActuales.Count}");

            // 2. Realizar asignaciones (Ahora le pasamos las actuales)
            var asignaciones = _assignmentService.AsignarRoles(presentes, asignacionesActuales);

            // 3. Guardar en BD (Esto cerrará las viejas y abrirá las nuevas, incluyendo las retenidas)
            _dbService.GuardarAsignaciones(asignaciones);

            // 4. Exportar a JSON para el Frontend (TV)
            ExportarAJson(asignaciones);

            // 5. Mostrar resultados en consola
            MostrarResultados(asignaciones);

            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"- Proceso completado en {elapsed.TotalSeconds:0.00} segundos");
            Console.WriteLine("--------------------------------------------");
        }

        // Método para generar el JSON que leerá el Frontend - ¡NUEVO!
        private static void ExportarAJson(List<Asignacion> asignaciones)
        {
            try
            {
                // Estructura optimizada para que el Frontend la lea fácilmente
                var resultado = new
                {
                    UltimaActualizacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Roles = asignaciones
                        .GroupBy(a => a.RolAsignado)
                        .ToDictionary(
                            g => GetNombreRol(g.Key),
                            g => g.Select(a => new {
                                a.UserId,
                                a.NombreCompleto,
                                a.Puesto,
                                a.ProfileImageUrl
                            }).ToList()
                        ),
                    // RF_05: Teléfonos de emergencia
                    TelefonosEmergencia = new[]
                    {
                        new { Servicio = "Guardia Interna", Numero = "Int. 100" },
                        new { Servicio = "Bomberos", Numero = "100" },
                        new { Servicio = "SAME", Numero = "107" }
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(resultado, options);
                var path = Path.Combine(AppContext.BaseDirectory, "roles_actuales.json");

                File.WriteAllText(path, json);
                Console.WriteLine($"- JSON actualizado para Frontend: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"- [ADVERTENCIA] No se pudo generar el JSON: {ex.Message}");
            }
        }

        private static void MostrarResultados(List<Asignacion> asignaciones)
        {
            Console.WriteLine("\n=== ASIGNACIÓN ACTUAL ===");
            Console.WriteLine("| Rol   | Nombre Completo      | Puesto            |");
            Console.WriteLine("|-------|----------------------|-------------------|");

            foreach (var a in asignaciones.OrderBy(a => a.RolAsignado))
            {
                Console.WriteLine($"| {GetNombreRol(a.RolAsignado),-5} | {a.NombreCompleto,-20} | {Truncate(a.Puesto, 15),-15} |");
            }
            Console.WriteLine("=============================================");
            Console.WriteLine($"Total asignados: {asignaciones.Count}");
        }

        private static string GetNombreRol(int rolId)
        {
            return rolId switch
            {
                1 => "JEFE BRIGADA",
                2 => "BRIGADISTA",
                3 => "EVACUADOR",
                4 => "ACTUACIÓN",
                5 => "SySO",
                _ => "ROL DESCONOCIDO"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}
-...--.-.-.