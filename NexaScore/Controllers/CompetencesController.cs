using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projet.Models;
using Projet.Services; 
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
        private readonly INotificationService _notifService; 


        public CompetencesController(ProjetContext context, INotificationService notifService)
        {
            _context = context;
            _notifService = notifService;
        }


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