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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Puesto { get; set; }
        public int RolReal { get; set; }
        public string ProfileImageUrl { get; set; }
    }
}