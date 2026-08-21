using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsignacionRolesApp.Models
{
    public class Persona
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public int RolReal { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
    }
}