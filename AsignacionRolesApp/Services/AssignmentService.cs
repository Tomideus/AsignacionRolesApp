using AsignacionRolesApp.Models;
using System.Collections.Generic;
using System.Linq;
using System;

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

        public List<Asignacion> AsignarRoles(List<Persona> personas)
        {
            var asignaciones = new List<Asignacion>();
            var disponibles = new List<Persona>(personas);
            var asignadosIds = new HashSet<int>();

            foreach (var rol in _rolesRequeridos.Keys.OrderBy(r => r))
            {
                int asignados = 0;
                int requeridos = _rolesRequeridos[rol];

                // Asignar con rol exacto
                var candidatos = disponibles
                    .Where(p => p.RolReal == rol && !asignadosIds.Contains(p.UserId))
                    .ToList();

                while (asignados < requeridos && candidatos.Count > 0)
                {
                    var seleccionado = candidatos[_rand.Next(candidatos.Count)];
                    asignaciones.Add(CrearAsignacion(seleccionado, rol));
                    disponibles.Remove(seleccionado);
                    candidatos.Remove(seleccionado);
                    asignadosIds.Add(seleccionado.UserId);
                    asignados++;
                }

                // Buscar en niveles inferiores
                int nivelBusqueda = rol + 1;
                while (asignados < requeridos && nivelBusqueda <= 5)
                {
                    candidatos = disponibles
                        .Where(p => p.RolReal == nivelBusqueda && !asignadosIds.Contains(p.UserId))
                        .ToList();

                    while (asignados < requeridos && candidatos.Count > 0)
                    {
                        var seleccionado = candidatos[_rand.Next(candidatos.Count)];
                        asignaciones.Add(CrearAsignacion(seleccionado, rol));
                        disponibles.Remove(seleccionado);
                        candidatos.Remove(seleccionado);
                        asignadosIds.Add(seleccionado.UserId);
                        asignados++;
                    }

                    nivelBusqueda++;
                }
            }
            return asignaciones;
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