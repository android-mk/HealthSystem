namespace HealthSystem.Models
{
    public class ClientHealthProgram
    {
        public int ClientId { get; set; }
        public Client Client { get; set; }

        public int HealthProgramId { get; set; }
        public HealthProgram HealthProgram { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    }
}