using Microsoft.AspNetCore.Mvc;
using HealthSystem.Data; // Add this
using HealthSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthSystem.Controllers // Add namespace
{
    public class HealthProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HealthProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            Console.WriteLine("Rendering Create view");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(HealthProgram program)
        {
            Console.WriteLine($"Received POST with data: {program?.Name}");
            if (ModelState.IsValid)
            {
                _context.Add(program);
                await _context.SaveChangesAsync();
                Console.WriteLine("Redirecting to Index");
                return RedirectToAction(nameof(Index));
            }
            Console.WriteLine("Validation failed, returning to Create view");
            return View(program);
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.HealthPrograms.ToListAsync());
        }
    }
}