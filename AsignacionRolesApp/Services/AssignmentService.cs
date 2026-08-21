using AsignacionRolesApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsignacionRolesApp.Services
{
    public class AssignmentService
    {
        private readonly Dictionary<int, int> _rolesRequeridos;
        private readonly Random _rand;

        public AssignmentService(Dictionary<int, int> rolesRequeridos)
        {
            _rolesRequeridos = rolesRequeridos;
            _rand = new Random();
        }

        // Ahora recibe las asignaciones actuales para mantener la continuidad
        public List<Asignacion> AsignarRoles(List<Persona> presentes, List<Asignacion> asignacionesActuales)
        {
            var asignacionesFinales = new List<Asignacion>();
            var asignadosIds = new HashSet<int>();

            // 1. RETENER: Mantener los roles de quienes siguen presentes
            var presentesIds = presentes.Select(p => p.UserId).ToHashSet();

            foreach (var actual in asignacionesActuales)
            {
                // Si el usuario que tenía un rol SIGUE en la planta, se lo retenemos
                if (presentesIds.Contains(actual.UserId))
                {
                    var persona = presentes.First(p => p.UserId == actual.UserId);
                    asignacionesFinales.Add(CrearAsignacion(persona, actual.RolAsignado));
                    asignadosIds.Add(actual.UserId);
                }
                // Si no está en presentesIds, significa que se fue de la planta. 
                // Su rol queda vacante y se llenará en el paso 3.
            }

            // 2. CALCULAR HUECOS: ¿Cuántos puestos faltan por cubrir en cada rol?
            var rolesFaltantes = new Dictionary<int, int>();
            foreach (var rol in _rolesRequeridos.Keys)
            {
                int requeridos = _rolesRequeridos[rol];
                int retenidos = asignacionesFinales.Count(a => a.RolAsignado == rol);
                int faltan = requeridos - retenidos;

                if (faltan > 0)
                {
                    rolesFaltantes[rol] = faltan;
                }
            }

            // 3. POOL DE DISPONIBLES: Personas presentes que NO tienen un rol retenido
            var disponibles = presentes.Where(p => !asignadosIds.Contains(p.UserId)).ToList();

            // 4. ASIGNAR NUEVOS: Llenar los huecos calculados en el paso 2
            foreach (var rol in rolesFaltantes.Keys.OrderBy(r => r))
            {
                int requeridos = rolesFaltantes[rol];
                int asignados = 0;

                // Prioridad 1: Buscar personas con el rol exacto
                var candidatosExactos = disponibles
                    .Where(p => p.RolReal == rol)
                    .OrderBy(p => _rand.Next())
                    .ToList();

                foreach (var candidato in candidatosExactos)
                {
                    if (asignados >= requeridos) break;
                    asignacionesFinales.Add(CrearAsignacion(candidato, rol));
                    asignadosIds.Add(candidato.UserId);
                    disponibles.Remove(candidato);
                    asignados++;
                }

                // Prioridad 2: Fallback con cualquier persona disponible
                if (asignados < requeridos)
                {
                    var candidatosFallback = disponibles
                        .OrderBy(p => _rand.Next())
                        .ToList();

                    foreach (var candidato in candidatosFallback)
                    {
                        if (asignados >= requeridos) break;
                        asignacionesFinales.Add(CrearAsignacion(candidato, rol));
                        asignadosIds.Add(candidato.UserId);
                        disponibles.Remove(candidato);
                        asignados++;
                    }
                }
            }

            return asignacionesFinales;
        }

        private Asignacion CrearAsignacion(Persona persona, int rolAsignado)
        {
            return new Asignacion
            {
                UserId = persona.UserId,
                NombreCompleto = $"{persona.FirstName} {persona.LastName}",
                Puesto = persona.Puesto,
                RolAsignado = rolAsignado,
                ProfileImageUrl = persona.ProfileImageUrl
            };
        }
    }
}