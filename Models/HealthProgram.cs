using System.ComponentModel.DataAnnotations;
namespace HealthSystem.Models
{
    public class HealthProgram
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
    }
}