using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionRolesApp.Models
{
    public class Asignacion
    {
        public int UserId { get; set; }
        public string NombreCompleto { get; set; }
        public string Puesto { get; set; }
        public int RolAsignado { get; set; }
        public string ProfileImageUrl { get; set; }
    }
}