namespace HealthSystem.Models.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public List<EnrollmentDto> EnrolledPrograms { get; set; }
    }
}