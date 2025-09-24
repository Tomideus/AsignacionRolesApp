using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AsignacionRolesApp
{
    public class AppConfig
    {
        public string ConnectionString { get; set; }
        public int UpdateIntervalSeconds { get; set; }
        public Dictionary<int, int> RolesRequeridos { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception("ConnectionString no configurada");

            if (RolesRequeridos == null || !RolesRequeridos.Any())
                throw new Exception("Roles requeridos no configurados");
        }

        public static AppConfig Load()
        {
            try
            {
                // Obtener ruta del ejecutable
                var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var builder = new ConfigurationBuilder()
                    .SetBasePath(exePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var config = builder.Build();

                var appConfig = new AppConfig
                {
                    ConnectionString = config.GetConnectionString("DensoDB"),
                    UpdateIntervalSeconds = int.Parse(config["AppSettings:UpdateIntervalSeconds"]),
                    RolesRequeridos = config.GetSection("AppSettings:RolesRequeridos")
                        .GetChildren()
                        .ToDictionary(
                            x => int.Parse(x.Key),
                            x => int.Parse(x.Value))
                };

                appConfig.Validate();
                return appConfig;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar configuración: {ex.Message}", ex);
            }
        }
    }
}