using HealthSystem.Data;
using HealthSystem.Models;
using HealthSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace HealthSystem.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(ApplicationDbContext context, ILogger<ClientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.LastName.Contains(searchString) ||
                                       c.FirstName.Contains(searchString));
            }

            var clients = await query.Include(c => c.EnrolledPrograms)
                                   .ThenInclude(chp => chp.HealthProgram)
                                   .ToListAsync();
            return View(clients);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new ClientCreateViewModel
            {
                AvailablePrograms = await _context.HealthPrograms
                    .Select(p => new HealthProgramCheckboxItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        IsSelected = false
                    })
                    .ToListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.AvailablePrograms = await GetAvailablePrograms(viewModel.SelectedProgramIds);
                return View(viewModel);
            }

            try
            {
                var client = new Client
                {
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    DateOfBirth = viewModel.DateOfBirth,
                    Address = viewModel.Address,
                    PhoneNumber = viewModel.PhoneNumber
                };

                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                // Handle program enrollment
                foreach (var programId in viewModel.SelectedProgramIds ?? Enumerable.Empty<int>())
                {
                    _context.ClientHealthPrograms.Add(new ClientHealthProgram
                    {
                        ClientId = client.Id,
                        HealthProgramId = programId,
                        EnrollmentDate = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client");
                ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                viewModel.AvailablePrograms = await GetAvailablePrograms(viewModel.SelectedProgramIds);
                return View(viewModel);
            }
        }

        private async Task<List<HealthProgramCheckboxItem>> GetAvailablePrograms(ICollection<int> selectedIds)
        {
            return await _context.HealthPrograms
                .Select(p => new HealthProgramCheckboxItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsSelected = selectedIds != null && selectedIds.Contains(p.Id)
                })
                .ToListAsync();
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = await _context.Clients
                .Include(c => c.EnrolledPrograms)
                .ThenInclude(chp => chp.HealthProgram)
                .FirstOrDefaultAsync(c => c.Id == id);

            return client == null ? NotFound() : View(client);
        }
        // GET: Clients/Enroll/5
        public async Task<IActionResult> Enroll(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            ViewBag.Programs = await _context.HealthPrograms.ToListAsync();
            return View(client);
        }

        // POST: Clients/Enroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int id, int[] programIds)
        {
            var client = await _context.Clients
                .Include(c => c.EnrolledPrograms)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Remove existing enrollments for this client (optional, depending on your requirements)
            var existingEnrollments = client.EnrolledPrograms.ToList();
            foreach (var enrollmentToRemove in existingEnrollments)
            {
                _context.ClientHealthPrograms.Remove(enrollmentToRemove);
            }
            await _context.SaveChangesAsync();

            // Add the new enrollments
            foreach (var programId in programIds)
            {
                var enrollment = new ClientHealthProgram
                {
                    ClientId = id,
                    HealthProgramId = programId,
                    EnrollmentDate = DateTime.Now
                };
                _context.ClientHealthPrograms.Add(enrollment);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
        // GET: Clients/EnrolledInProgram/{programId}
        public async Task<IActionResult> EnrolledInProgram(int? programId)
        {
            if (programId == null)
            {
                return NotFound();
            }

            var program = await _context.HealthPrograms.FindAsync(programId);
            if (program == null)
            {
                return NotFound();
            }

            var clientsInProgram = await _context.ClientHealthPrograms
                .Where(chp => chp.HealthProgramId == programId)
                .Include(chp => chp.Client)
                    .ThenInclude(c => c.EnrolledPrograms) // Include for potential further details
                        .ThenInclude(e => e.HealthProgram)
                .Select(chp => chp.Client)
                .ToListAsync();

            ViewBag.ProgramName = program.Name;
            return View(clientsInProgram);
        }
        // GET: Clients/DownloadDetailsPdf/{id}
        public async Task<IActionResult> DownloadDetailsPdf(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.EnrolledPrograms)
                    .ThenInclude(ep => ep.HealthProgram)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            try
            {
                byte[] pdfBytes = GenerateClientDetailsPdf(client);
                return File(pdfBytes, "application/pdf", $"ClientDetails_{client.FirstName}_{client.LastName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for client ID {ClientId}", id);
                return StatusCode(500, "Error generating PDF document");
            }
        }

        private byte[] GenerateClientDetailsPdf(Client client)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                PdfDocument pdfDoc = new PdfDocument(new PdfWriter(ms));
                Document document = new Document(pdfDoc);

                // Add title with bold style
                Paragraph title = new Paragraph($"Client Details")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
                document.Add(title);

                document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10)
                    .SetFontColor(new DeviceRgb(128, 128, 128)));

                document.Add(new Paragraph("\n"));

                Table table = new Table(2, false);

                AddTableRow(table, "First Name", client.FirstName);
                AddTableRow(table, "Last Name", client.LastName);
                AddTableRow(table, "Date of Birth", client.DateOfBirth.ToString("yyyy-MM-dd"));
                AddTableRow(table, "Phone", client.PhoneNumber);

                if (!string.IsNullOrEmpty(client.Address))
                {
                    AddTableRow(table, "Address", client.Address);
                }

                table.AddCell(new Cell(1, 2).Add(new Paragraph("")).SetBorder(Border.NO_BORDER));

                if (client.EnrolledPrograms != null && client.EnrolledPrograms.Any())
                {
                    AddTableRow(table, "Enrolled Programs", "");
                    foreach (var program in client.EnrolledPrograms)
                    {
                        AddTableRow(table, "",
                            $"{program.HealthProgram?.Name} (since {program.EnrollmentDate:yyyy-MM-dd})");
                    }
                }

                document.Add(table);
                document.Close();

                return ms.ToArray();
            }
        }

        private void AddTableRow(Table table, string label, string value)
        {
            Cell labelCell = new Cell()
                .Add(new Paragraph(label).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                .SetPadding(5);
            table.AddCell(labelCell);

            table.AddCell(new Cell()
                .Add(new Paragraph(value ?? "N/A"))
                .SetPadding(5));
        }

    }
}