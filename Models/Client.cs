using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthSystem.Models
{
    public class Client
    {
        public int Id { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        
        public ICollection<ClientHealthProgram> EnrolledPrograms { get; set; } = new List<ClientHealthProgram>();
    }
}