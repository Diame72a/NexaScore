using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Projet.Controllers
{
    public class PostesController : Controller
    {
        private readonly ProjetContext _context;

        public PostesController(ProjetContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index(string searchString)
        {
            
            var query = _context.Postes.AsQueryable();

            
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Intitule.Contains(searchString));
            }

            
            var postes = await query.OrderBy(p => p.Intitule).ToListAsync();

            
            var topPostes = await _context.Offres
                .Include(o => o.Poste)
                .GroupBy(o => o.Poste.Intitule)
                .Select(g => new { Nom = g.Key, Nombre = g.Count() })
                .OrderByDescending(x => x.Nombre)
                .Take(5)
                .ToListAsync();

            // Si pas de données (ex: au début), on évite le bug du graph vide
            if (!topPostes.Any())
            {
                ViewBag.Labels = new List<string>();
                ViewBag.Data = new List<int>();
            }
            else
            {
                ViewBag.Labels = topPostes.Select(x => x.Nom).ToList();
                ViewBag.Data = topPostes.Select(x => x.Nombre).ToList();
            }

            ViewData["CurrentFilter"] = searchString;

            return View(postes);
        }

        // GET: Postes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var poste = await _context.Postes
                .FirstOrDefaultAsync(m => m.Id == id);

            // On récupère aussi le nombre d'offres liées pour l'afficher
            ViewBag.NbOffresLiees = await _context.Offres.CountAsync(o => o.PosteId == id);

            if (poste == null) return NotFound();

            return View(poste);
        }

        // GET: Postes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Postes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Intitule")] Poste poste)
        {
            if (ModelState.IsValid)
            {
                _context.Add(poste);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(poste);
        }

        // GET: Postes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var poste = await _context.Postes.FindAsync(id);
            if (poste == null) return NotFound();
            return View(poste);
        }

        // POST: Postes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Intitule")] Poste poste)
        {
            if (id != poste.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(poste);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PosteExists(poste.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(poste);
        }

        // GET: Postes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var poste = await _context.Postes.FirstOrDefaultAsync(m => m.Id == id);
            if (poste == null) return NotFound();
            return View(poste);
        }

        // POST: Postes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var poste = await _context.Postes.FindAsync(id);
            if (poste != null) _context.Postes.Remove(poste);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PosteExists(int id)
        {
            return _context.Postes.Any(e => e.Id == id);
        }
    }
}