namespace HealthSystem.Models.DTOs
{
    public class EnrollmentDto
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}