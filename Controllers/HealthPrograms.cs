using Microsoft.AspNetCore.Mvc;
using HealthSystem.Data;
using HealthSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HealthSystem.Controllers
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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var healthProgram = await _context.HealthPrograms
                .FirstOrDefaultAsync(m => m.Id == id);

            if (healthProgram == null)
            {
                return NotFound();
            }

            return View(healthProgram);
        }

        // GET: HealthPrograms/Edit/1
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var healthProgram = await _context.HealthPrograms.FindAsync(id);
            if (healthProgram == null)
            {
                return NotFound();
            }
            return View(healthProgram);
        }

        // POST: HealthPrograms/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] HealthProgram program)
        {
            if (id != program.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(program);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HealthProgramExists(program.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(program);
        }

        private bool HealthProgramExists(int id)
        {
            return _context.HealthPrograms.Any(e => e.Id == id);
        }
    }
}