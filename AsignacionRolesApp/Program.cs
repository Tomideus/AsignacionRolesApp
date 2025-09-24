using AsignacionRolesApp.Models;
using AsignacionRolesApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace AsignacionRolesApp
{
    class Program
    {
        private static DatabaseService _dbService;
        private static AssignmentService _assignmentService;
        private static System.Timers.Timer _timer;
        private static int _updateInterval;

        static void Main()
        {
            try
            {
                // Cargar configuración
                var config = AppConfig.Load();
                _updateInterval = config.UpdateIntervalSeconds * 1000;

                // Inicializar servicios
                _dbService = new DatabaseService(config.ConnectionString);
                _assignmentService = new AssignmentService(config.RolesRequeridos);

                Console.WriteLine("============================================");
                Console.WriteLine("  Sistema de Asignación de Roles - Denso  ");
                Console.WriteLine("============================================");
                Console.WriteLine($"Configuración cargada:");
                Console.WriteLine($"- Intervalo: {config.UpdateIntervalSeconds} segundos");
                Console.WriteLine($"- Roles requeridos: {string.Join(", ", config.RolesRequeridos.Select(kv => $"{kv.Key}:{kv.Value}"))}");
                Console.WriteLine("--------------------------------------------\n");

                // Ejecutar inmediatamente al inicio
                EjecutarAsignacion();

                // Configurar timer
                _timer = new System.Timers.Timer(_updateInterval);
                _timer.Elapsed += (s, e) => EjecutarAsignacion();
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
                    if (key.Key == ConsoleKey.R) EjecutarAsignacion();
                }

                _timer.Stop();
                Console.WriteLine("\nAplicación finalizada");
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

        private static void EjecutarAsignacion()
        {
            try
            {
                var startTime = DateTime.Now;
                Console.WriteLine($"{startTime:HH:mm:ss}: Iniciando asignación...");

                // 1. Obtener personas presentes
                var presentes = _dbService.ObtenerPersonasPresentes();
                Console.WriteLine($"- Personas presentes: {presentes.Count}");

                // 2. Realizar asignaciones
                var asignaciones = _assignmentService.AsignarRoles(presentes);

                // 3. Guardar en BD
                _dbService.GuardarAsignaciones(asignaciones);

                // 4. Mostrar resultados
                MostrarResultados(asignaciones);

                var elapsed = DateTime.Now - startTime;
                Console.WriteLine($"- Asignación completada en {elapsed.TotalSeconds:0.00} segundos");
                Console.WriteLine("--------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void MostrarResultados(List<Asignacion> asignaciones)
        {
            Console.WriteLine("\n=== ASIGNACIÓN ACTUAL ===");
            Console.WriteLine("| Rol  | Nombre Completo      | Puesto            |");
            Console.WriteLine("|------|----------------------|-------------------|");

            foreach (var a in asignaciones.OrderBy(a => a.RolAsignado))
            {
                Console.WriteLine($"| {GetNombreRol(a.RolAsignado),-4} | {a.NombreCompleto,-20} | {Truncate(a.Puesto, 15),-15} |");
            }
            Console.WriteLine("=============================================");
            Console.WriteLine($"Total asignados: {asignaciones.Count}");
        }

        private static string GetNombreRol(int rolId)
        {
            return rolId switch
            {
                1 => "JEFE",
                2 => "BRIG",
                3 => "EVAC",
                4 => "ACT",
                5 => "SySO",
                _ => "???"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}