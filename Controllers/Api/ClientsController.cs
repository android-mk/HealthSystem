using HealthSystem.Data;
using HealthSystem.Models;
using HealthSystem.Models.DTOs; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HealthSystem.Controllers.Api
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientsApiController> _logger;

        public ClientsApiController(
            ApplicationDbContext context,
            ILogger<ClientsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients()
        {
            try
            {
                var clients = await _context.Clients
                    .Include(c => c.EnrolledPrograms)
                        .ThenInclude(ep => ep.HealthProgram)
                    .AsNoTracking()
                    .Select(c => new ClientDto
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        DateOfBirth = c.DateOfBirth,
                        Address = c.Address,
                        PhoneNumber = c.PhoneNumber,
                        EnrolledPrograms = c.EnrolledPrograms.Select(ep => new HealthSystem.Models.DTOs.EnrollmentDto
                        {
                            ProgramId = ep.HealthProgramId,
                            ProgramName = ep.HealthProgram.Name,
                            EnrollmentDate = ep.EnrollmentDate
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all clients");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetClient(int id)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.EnrolledPrograms)
                        .ThenInclude(ep => ep.HealthProgram)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    return NotFound();
                }

                var clientDto = new ClientDto
                {
                    Id = client.Id,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    DateOfBirth = client.DateOfBirth,
                    Address = client.Address,
                    PhoneNumber = client.PhoneNumber,
                    EnrolledPrograms = client.EnrolledPrograms.Select(ep => new HealthSystem.Models.DTOs.EnrollmentDto
                    {
                        ProgramId = ep.HealthProgramId,
                        ProgramName = ep.HealthProgram.Name,
                        EnrollmentDate = ep.EnrollmentDate
                    }).ToList()
                };

                return Ok(clientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client with ID {ClientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> CreateClient([FromBody] ClientCreateDto clientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var client = new Client
                {
                    FirstName = clientDto.FirstName,
                    LastName = clientDto.LastName,
                    DateOfBirth = clientDto.DateOfBirth,
                    Address = clientDto.Address,
                    PhoneNumber = clientDto.PhoneNumber
                };

                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                if (clientDto.ProgramIds != null && clientDto.ProgramIds.Any())
                {
                    foreach (var programId in clientDto.ProgramIds)
                    {
                        _context.ClientHealthPrograms.Add(new ClientHealthProgram
                        {
                            ClientId = client.Id,
                            HealthProgramId = programId,
                            EnrollmentDate = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                var createdClient = await _context.Clients
                    .Include(c => c.EnrolledPrograms)
                        .ThenInclude(ep => ep.HealthProgram)
                    .FirstAsync(c => c.Id == client.Id);

                var resultDto = new ClientDto
                {
                    Id = createdClient.Id,
                    FirstName = createdClient.FirstName,
                    LastName = createdClient.LastName,
                    DateOfBirth = createdClient.DateOfBirth,
                    Address = createdClient.Address,
                    PhoneNumber = createdClient.PhoneNumber,
                    EnrolledPrograms = createdClient.EnrolledPrograms.Select(ep => new HealthSystem.Models.DTOs.EnrollmentDto
                    {
                        ProgramId = ep.HealthProgramId,
                        ProgramName = ep.HealthProgram.Name,
                        EnrollmentDate = ep.EnrollmentDate
                    }).ToList()
                };

                return CreatedAtAction(
                    nameof(GetClient), 
                    new { id = resultDto.Id }, 
                    resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}