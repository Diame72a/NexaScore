using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services; // IMPORTANT
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Projet.Controllers
{
    [Authorize]
    public class CompetencesController : Controller
    {
        private readonly ProjetContext _context;
        private readonly INotificationService _notifService; // 1. Déclaration

        // 2. Injection
        public CompetencesController(ProjetContext context, INotificationService notifService)
        {
            _context = context;
            _notifService = notifService;
        }

        // ============================================================
        // 1. INDEX
        // ============================================================
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var query = _context.Competences.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Nom.Contains(searchString));
            }

            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            switch (sortOrder)
            {
                case "name_desc":
                    query = query.OrderByDescending(c => c.Nom);
                    break;
                default:
                    query = query.OrderBy(c => c.Nom);
                    break;
            }

            var competences = await query.ToListAsync();

            // Stats pour le graphique (Top 5 Compétences demandées)
            var topCompetences = await _context.CompetenceSouhaitees
                .Include(cs => cs.Competence)
                .GroupBy(cs => cs.Competence.Nom)
                .Select(g => new { Nom = g.Key, Nombre = g.Count() })
                .OrderByDescending(x => x.Nombre)
                .Take(5)
                .ToListAsync();

            if (!topCompetences.Any())
            {
                ViewBag.TopLabels = new List<string>();
                ViewBag.TopData = new List<int>();
            }
            else
            {
                ViewBag.TopLabels = topCompetences.Select(x => x.Nom).ToList();
                ViewBag.TopData = topCompetences.Select(x => x.Nombre).ToList();
            }

            ViewData["CurrentFilter"] = searchString;

            return View(competences);
        }

        // ============================================================
        // 2. CREATE
        // ============================================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom")] Competence competence)
        {
            if (ModelState.IsValid)
            {
                _context.Add(competence);
                await _context.SaveChangesAsync();

                // --- NOTIF ---
                await _notifService.Ajouter(
                    "Catalogue mis à jour",
                    $"La compétence '{competence.Nom}' a été ajoutée.",
                    "fas fa-tag",
                    "text-warning"
                );

                return RedirectToAction(nameof(Index));
            }
            return View(competence);
        }

        // ============================================================
        // 3. EDIT
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var competence = await _context.Competences.FindAsync(id);
            if (competence == null) return NotFound();
            return View(competence);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom")] Competence competence)
        {
            if (id != competence.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(competence);
                    await _context.SaveChangesAsync();

                    // --- NOTIF (Optionnel) ---
                    // await _notifService.Ajouter("Modification", $"Compétence '{competence.Nom}' mise à jour.", "fas fa-pen", "text-secondary");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Competences.Any(e => e.Id == competence.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(competence);
        }

        // ============================================================
        // 4. DETAILS & DELETE
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var competence = await _context.Competences.FirstOrDefaultAsync(m => m.Id == id);
            if (competence == null) return NotFound();
            return View(competence);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var competence = await _context.Competences.FirstOrDefaultAsync(m => m.Id == id);
            if (competence == null) return NotFound();
            return View(competence);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competence = await _context.Competences.FindAsync(id);
            if (competence != null)
            {
                string nom = competence.Nom;
                _context.Competences.Remove(competence);
                await _context.SaveChangesAsync();

                // --- NOTIF ---
                await _notifService.Ajouter(
                    "Catalogue Nettoyé",
                    $"La compétence '{nom}' a été supprimée.",
                    "fas fa-trash",
                    "text-danger"
                );
            }
            return RedirectToAction(nameof(Index));
        }
    }
}