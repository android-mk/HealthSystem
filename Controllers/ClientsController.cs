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
                    .ThenInclude(c => c.EnrolledPrograms)
                        .ThenInclude(e => e.HealthProgram)
                .Select(chp => chp.Client)
                .ToListAsync();

            ViewBag.ProgramName = program.Name;
            ViewBag.ProgramId = programId;
            return View(clientsInProgram);
        }
        //For Client List PDF
        [HttpGet("DownloadClientsListPdf")]
        public async Task<IActionResult> DownloadClientsListPdf(string searchString)
        {
            var clientsQuery = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                clientsQuery = clientsQuery.Where(c =>
                    c.LastName.Contains(searchString) ||
                    c.FirstName.Contains(searchString));
            }

            var clients = await clientsQuery.ToListAsync();

            try
            {
                using var ms = new MemoryStream();
                var pdfDoc = new PdfDocument(new PdfWriter(ms));
                using var document = new Document(pdfDoc);

                // Title
                document.Add(new Paragraph("Client List")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20));

                document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10));

                if (!string.IsNullOrEmpty(searchString))
                {
                    document.Add(new Paragraph($"Filter: {searchString}")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE))
                        .SetFontColor(ColorConstants.GRAY));
                }

                // Create table with 5 columns
                var table = new Table(5)
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                // Header row
                table.AddHeaderCell("First Name");
                table.AddHeaderCell("Last Name");
                table.AddHeaderCell("Date of Birth");
                table.AddHeaderCell("Phone");
                table.AddHeaderCell("Program Count");

                // Data rows
                foreach (var client in clients)
                {
                    table.AddCell(client.FirstName);
                    table.AddCell(client.LastName);
                    table.AddCell(client.DateOfBirth.ToShortDateString());
                    table.AddCell(client.PhoneNumber ?? "N/A");
                    table.AddCell((await _context.ClientHealthPrograms
                        .CountAsync(chp => chp.ClientId == client.Id))
                        .ToString());
                }

                document.Add(table);
                document.Close();

                return File(ms.ToArray(), "application/pdf",
                    $"ClientsList_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client list PDF generation failed");
                return StatusCode(500, "PDF generation error");
            }
        }
        //For Client Details PDF
        [HttpGet("DownloadClientDetailsPdf/{id}")]
        public async Task<IActionResult> DownloadClientDetailsPdf(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .Include(c => c.EnrolledPrograms)
                    .ThenInclude(ep => ep.HealthProgram)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            try
            {
                using var ms = new MemoryStream();
                var pdfDoc = new PdfDocument(new PdfWriter(ms));
                using var document = new Document(pdfDoc);

                // Title
                document.Add(new Paragraph($"Client: {client.FirstName} {client.LastName}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20));

                document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10));

                // Client Info Table
                var infoTable = new Table(2)
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                AddPdfTableRow(infoTable, "First Name", client.FirstName);
                AddPdfTableRow(infoTable, "Last Name", client.LastName);
                AddPdfTableRow(infoTable, "Date of Birth", client.DateOfBirth.ToShortDateString());
                AddPdfTableRow(infoTable, "Phone", client.PhoneNumber ?? "N/A");
                AddPdfTableRow(infoTable, "Address", client.Address ?? "N/A");

                document.Add(infoTable);

                // Programs Table
                if (client.EnrolledPrograms?.Any() == true)
                {
                    document.Add(new Paragraph("Enrolled Programs")
                        .SetMarginTop(20)
                        .SetFontSize(16));

                    var programTable = new Table(2)
                        .UseAllAvailableWidth();

                    programTable.AddHeaderCell("Program Name");
                    programTable.AddHeaderCell("Enrollment Date");

                    foreach (var program in client.EnrolledPrograms.OrderBy(p => p.HealthProgram.Name))
                    {
                        programTable.AddCell(program.HealthProgram?.Name ?? "Unknown");
                        programTable.AddCell(program.EnrollmentDate.ToShortDateString());
                    }

                    document.Add(programTable);
                }

                document.Close();
                return File(ms.ToArray(), "application/pdf",
                    $"Client_{client.LastName}_{client.Id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed for client {ClientId}", id);
                return StatusCode(500, "PDF generation error");
            }
        }

        private void AddPdfTableRow(Table table, string label, string value)
        {
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(boldFont))
                .SetBackgroundColor(new DeviceRgb(240, 240, 240)));

            table.AddCell(new Cell().Add(new Paragraph(value ?? "N/A")));
        }
        //For Enrolled Clients PDF
        [HttpGet("DownloadEnrolledClientsPdf")]
        public async Task<IActionResult> DownloadEnrolledClientsPdf(int programId)
        {
            var program = await _context.HealthPrograms.FindAsync(programId);
            if (program == null) return NotFound();

            var clients = await _context.ClientHealthPrograms
                .Where(chp => chp.HealthProgramId == programId)
                .Include(chp => chp.Client)
                .OrderBy(chp => chp.Client.LastName)
                .ToListAsync();

            try
            {
                using var ms = new MemoryStream();
                var pdfDoc = new PdfDocument(new PdfWriter(ms));
                using var document = new Document(pdfDoc);

                // Title
                document.Add(new Paragraph($"Clients Enrolled in {program.Name}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20));

                document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10));

                if (clients.Any())
                {
                    var table = new Table(4)
                        .UseAllAvailableWidth()
                        .SetMarginTop(20);

                    // Header
                    table.AddHeaderCell("Client Name");
                    table.AddHeaderCell("Date of Birth");
                    table.AddHeaderCell("Phone");
                    table.AddHeaderCell("Enrollment Date");

                    // Rows
                    foreach (var enrollment in clients)
                    {
                        table.AddCell($"{enrollment.Client.LastName}, {enrollment.Client.FirstName}");
                        table.AddCell(enrollment.Client.DateOfBirth.ToShortDateString());
                        table.AddCell(enrollment.Client.PhoneNumber ?? "N/A");
                        table.AddCell(enrollment.EnrollmentDate.ToShortDateString());
                    }

                    document.Add(table);
                }
                else
                {
                    document.Add(new Paragraph("No clients enrolled in this program")
                        .SetMarginTop(20)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE))); // Italic
                }

                document.Close();
                return File(ms.ToArray(), "application/pdf",
                    $"{program.Name.Replace(" ", "_")}_Enrollments.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed for program {ProgramId}", programId);
                return StatusCode(500, "PDF generation error");
            }
        }
    }
}