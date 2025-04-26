using Microsoft.EntityFrameworkCore;
using HealthSystem.Models;

namespace HealthSystem.Data 
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<HealthProgram> HealthPrograms => Set<HealthProgram>();
        public DbSet<ClientHealthProgram> ClientHealthPrograms => Set<ClientHealthProgram>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientHealthProgram>()
                .HasKey(chp => new { chp.ClientId, chp.HealthProgramId });
        }
    }
}