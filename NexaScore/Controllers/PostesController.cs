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
    public class PostesController : Controller
    {
        private readonly ProjetContext _context;
        private readonly INotificationService _notifService; 


        public PostesController(ProjetContext context, INotificationService notifService)
        {
            _context = context;
            _notifService = notifService;
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


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Intitule")] Poste poste)
        {
            if (ModelState.IsValid)
            {
                _context.Add(poste);
                await _context.SaveChangesAsync();


                await _notifService.Ajouter(
                    "Référentiel Mis à jour",
                    $"Le métier '{poste.Intitule}' a été ajouté à la base.",
                    "fas fa-book",
                    "text-info"
                );

                return RedirectToAction(nameof(Index));
            }
            return View(poste);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var poste = await _context.Postes.FindAsync(id);
            if (poste == null) return NotFound();
            return View(poste);
        }

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
                    if (!_context.Postes.Any(e => e.Id == poste.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(poste);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var poste = await _context.Postes
                .FirstOrDefaultAsync(m => m.Id == id);

            if (poste == null) return NotFound();

            ViewBag.NbOffresLiees = await _context.Offres.CountAsync(o => o.PosteId == id);

            return View(poste);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var poste = await _context.Postes.FirstOrDefaultAsync(m => m.Id == id);
            if (poste == null) return NotFound();
            return View(poste);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var poste = await _context.Postes.FindAsync(id);
            if (poste != null)
            {
                string nom = poste.Intitule;
                _context.Postes.Remove(poste);
                await _context.SaveChangesAsync();


                await _notifService.Ajouter(
                    "Référentiel Nettoyé",
                    $"Le métier '{nom}' a été supprimé.",
                    "fas fa-trash-alt",
                    "text-secondary"
                );
            }
            return RedirectToAction(nameof(Index));
        }
    }
}