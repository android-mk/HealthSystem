using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace HealthSystem.Models.DTOs
{
    public class ClientCreateDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [StringLength(100)]
        public string Address { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public List<int> ProgramIds { get; set; } = new List<int>();
    }
}